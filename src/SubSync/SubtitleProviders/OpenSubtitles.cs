using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SubSync
{
    internal class OpenSubtitles : SubtitleProviderBase, IDisposable
    {
        private const string VipApiUrl = "https://vip-api.opensubtitles.org/xml-rpc";
        private const string ApiUrl = "http://api.opensubtitles.org/xml-rpc";



        private const int MaxDownloadsPerDay = 200;
        private const int VipMaxDownloadsPerDay = 1000;

        private const int MaxRequestsEvery10Seconds = 40;
        private const int VipMaxRequestsEvery10Seconds = 40;

        private readonly IAuthCredentialProvider credentialProvider;
        private readonly HashSet<SubtitleLanguage> supportedLanguages;
        private readonly Thread keepAliveThread;

        private AutoResetEvent loginMutex = new AutoResetEvent(true);

        private DateTime startTime;
        private DateTime requestBlockTimeLimit;

        private int keepAliveInterval = 60 * 14; // every 14 minutes, 15 according to api. but just to be safe.

        private int totalRequests;
        private int totalRequestsToday;
        private int totalRequestsInTimeBlock;
        private int downloadQuota = 200; // should be updated from the response header: "Download-Quota" if available. Otherwise manually track.

        private string authenticationToken;
        private bool isAuthenticated;
        private bool isVip;

        private bool disposed;


        // https://www.opensubtitles.org
        public OpenSubtitles(HashSet<string> languages, IAuthCredentialProvider credentialProvider) : base(languages)
        {
            this.credentialProvider = credentialProvider;
            this.supportedLanguages = GetSupportedLanguages(languages);
            // until we get our user agent registered for OpenSubtitles.org
            // we can use a temporary one.
            // See: http://trac.opensubtitles.org/projects/opensubtitles/wiki/DevReadFirst
            UserAgent = "TemporaryUserAgent";
            RequestRetryLimit = 3; // max 3 retries, and with some seconds delay is necessary for opensubtitles
            startTime = DateTime.Now.Date;

            // Max 40 requests per 10 seconds per IP
            // Max 200 subtitle downloads per 24 hour per IP/User
            // User has to register as VIP to download 1000 per 24 hours.
            //  We will have to keep track on requests and downloads for this provider to not exceed the limit and first rely on other providers such as subscene            
            keepAliveThread = new Thread(KeepAliveProcess);
            keepAliveThread.Start();
        }

        private HashSet<SubtitleLanguage> GetSupportedLanguages(HashSet<string> languages)
        {
            var result = new HashSet<SubtitleLanguage>();
            foreach (var language in languages)
            {
                result.Add(SubtitleLanguage.Find(language));
            }
            return result;
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (this.isAuthenticated)
            {
                LogoutAsync();
            }

            this.keepAliveThread.Join();
        }

        public override async Task<string> GetAsync(string name, string outputDirectory)
        {
            AssertWithinRequestLimits();

            await LoginIfRequiredAsync();

            var searchResults = await SearchSubtitleAsync(name);
            if (searchResults.Length == 0)
            {
                throw new SubtitleNotFoundException();
            }

            var bestMatchingResult = FindBestSearchResultMatch(name, searchResults);
            if (bestMatchingResult == null)
            {
                throw new SubtitleNotFoundException();
            }

            return await DownloadSubtitleAsync(name, bestMatchingResult);
        }

        // see http://trac.opensubtitles.org/projects/opensubtitles/wiki/XmlRpcSearchSubtitles
        private async Task<Subtitle[]> SearchSubtitleAsync(string name)
        {
            var languageList = string.Join(",", supportedLanguages.Select(x => x.LanguageId).ToArray());

            // preferred one
            // var movieHash = CalculateVideoHash(name);
            // var movieByteSize = GetVideoByteSize(name);

            var query = Path.GetFileName(name);

            var season = "";
            var episode = "";
            var seasonNumber = 0;
            var episodeNumber = 0;
            var isTvShowEpisode = false;

            var regex = new Regex(@"([s](?<season>\d+)[e](?<episode>\d+))|((?<episode>\d+)[s](?<season>\d+))
                                    |((?<season>\d+)[e](?<episode>\d+))|([s](?<season>\d+))|(ep(?<episode>\d+))
                                    |(season.(?<season>\d+))|(episode.(?<episode>\d+))|(e(?<episode>\d+))",
                RegexOptions.IgnoreCase);

            foreach (Match m in regex.Matches(query))
            {
                var episodeGroup = m.Groups["episode"];
                if (episodeGroup.Success && string.IsNullOrEmpty(episode))
                {
                    var item = episodeGroup.Captures.FirstOrDefault();
                    if (item != null && !string.IsNullOrEmpty(item.Value))
                    {
                        episode = item.Value;
                    }
                }

                var seasonGroup = m.Groups["season"];
                if (seasonGroup.Success && string.IsNullOrEmpty(season))
                {
                    var item = seasonGroup.Captures.FirstOrDefault();
                    if (item != null && !string.IsNullOrEmpty(item.Value))
                    {
                        season = item.Value;
                    }
                }

                if (!string.IsNullOrEmpty(episode) && !string.IsNullOrEmpty(season))
                {
                    isTvShowEpisode = true;
                    int.TryParse(episode, out episodeNumber);
                    int.TryParse(season, out seasonNumber);
                    break;
                }
            }

            XmlRpcObject requestResult = null;
            if (isTvShowEpisode)
            {
                query = regex.Replace(query, "");
                requestResult = await ApiRequest("SearchSubtitles",
                    Arg("query", query),
                    Arg("sublanguageid", languageList),
                    Arg("seriesepisode", episodeNumber),
                    Arg("serieseason", seasonNumber));
            }
            else
            {
                requestResult = await ApiRequest("SearchSubtitles",
                    Arg("query", query),
                    Arg("sublanguageid", languageList));
            }

            return requestResult.Deserialize<Subtitle[]>("data");
        }

        private Task<string> DownloadSubtitleAsync(string name, Subtitle target)
        {
            return null;
        }

        private Subtitle FindBestSearchResultMatch(string name, Subtitle[] searchResults)
        {
            // TODO(Zerratar): implement an appropriate matching algorithm
            return searchResults.FirstOrDefault(); // super dumb just now.
        }

        private async Task LoginIfRequiredAsync()
        {
            loginMutex.WaitOne();

            if (!isAuthenticated)
            {
                try
                {
                    var credentials = credentialProvider.Get();
                    var authResult = await LoginAsync(credentials);
                    authenticationToken = authResult.GetValue<string>("token");
                    if (!string.IsNullOrEmpty(authenticationToken))
                    {
                        isAuthenticated = true;
                        return;
                    }
                }
                finally
                {
                    loginMutex.Set();
                }
            }

            throw new UnauthorizedAccessException("Login to opensubtitle.org failed!");
        }

        private Task LogoutAsync()
        {
            return ApiRequest("LogOut");
        }

        private async Task<bool> NoOperationAsync()
        {
            var result = await ApiRequest("NoOperation");
            if (!result.GetValue<string>("status").StartsWith("200"))
            {
                isAuthenticated = false;
                isVip = false;
                return false;
            }

            return true;
        }

        private Task<XmlRpcRoot> LoginAsync(AuthCredentials credentials)
        {
            this.authenticationToken = null;
            this.isAuthenticated = false;
            this.isVip = false;
            return ApiRequest("LogIn", Arg(credentials.Username), Arg(credentials.Password), Arg("en"), Arg(UserAgent));
        }

        private async Task<XmlRpcRoot> ApiRequest(string method, params KeyValuePair<string, object>[] arguments)
        {
            Interlocked.Increment(ref totalRequests);
            Interlocked.Increment(ref totalRequestsToday);
            Interlocked.Increment(ref totalRequestsInTimeBlock);

            if (requestBlockTimeLimit == DateTime.MinValue)
            {
                requestBlockTimeLimit = DateTime.Now;
            }

            var url = isVip ? VipApiUrl : ApiUrl;
            var requestData = BuildRequestData(method, arguments);
            var request = CreatePostAsync(url, requestData);
            try
            {
                using (var response = await request.GetResponseAsync())
                {
                    if (response.Headers.HasKeys())
                    {
                        var headerKeys = response.Headers.AllKeys;
                        if (headerKeys.Contains("Content-Location"))
                        {
                            isVip = isVip || response.Headers.Get("Content-Location")?.ToLower() ==
                                    "https://vip-api.opensubtitles.org.local/xml-rpc";
                        }

                        if (headerKeys.Contains("Download-Quota"))
                        {
                            if (int.TryParse(response.Headers.Get("Download-Quota"), out var quota))
                            {
                                Volatile.Write(ref downloadQuota, quota);
                            }

                        }
                    }

                    return XmlRpcObject.Parse(await GetResponseStringAsync(response));
                }
            }
            catch (Exception exc)
            {
                // for now...
                return null;
            }
        }

        private void KeepAliveProcess()
        {
            var lastKeepAliveMessage = DateTime.Now;
            while (!disposed)
            {
                if (this.isAuthenticated)
                {
                    if (DateTime.Now - lastKeepAliveMessage > TimeSpan.FromSeconds(keepAliveInterval))
                    {
                        lastKeepAliveMessage = DateTime.Now;
                        // ignore the fact that its fire-and-forget, since we will only do this every 14 mins, which is more than enough time for this to return a result.                        
                        NoOperationAsync();
                    }
                }
                Thread.Sleep(250);
            }
        }

        private void AssertWithinRequestLimits()
        {
            var elapsedTime = DateTime.Now.Date - startTime;
            if (elapsedTime >= TimeSpan.FromDays(1))
            {
                // its been one day. Reset the counters                
                Volatile.Write(ref totalRequestsToday, 0);
                Volatile.Write(ref downloadQuota, isVip ? VipMaxDownloadsPerDay : MaxDownloadsPerDay);
                startTime = DateTime.Now.Date;
            }
            else
            {
                var quota = Volatile.Read(ref downloadQuota);
                if (quota <= 0)
                {
                    throw new DownloadQuotaReachedException();
                }

                var timeBlock = DateTime.Now - requestBlockTimeLimit;
                if (timeBlock < TimeSpan.FromSeconds(10))
                {
                    var requests = Volatile.Read(ref totalRequestsInTimeBlock);
                    if (requests >= (isVip ? VipMaxRequestsEvery10Seconds : MaxRequestsEvery10Seconds))
                    {
                        throw new RequestQuotaReachedException();
                    }
                }
                else
                {
                    requestBlockTimeLimit = DateTime.Now;
                    Volatile.Write(ref totalRequestsInTimeBlock, 0);
                }
            }
        }

        private string BuildRequestData(string method, params KeyValuePair<string, object>[] arguments)
        {
            var sb = new StringBuilder();
            sb.Append($"<methodCall><methodName>{method}</methodName><params>");

            var tokenRequest = this.isAuthenticated && !string.IsNullOrEmpty(authenticationToken);
            if (tokenRequest)
            {
                sb.Append($"<param><value><string>{authenticationToken}</string></value></param>");
                if (arguments.Length > 0)
                {
                    sb.Append($"<param><value><array><data><value><struct>");
                    foreach (var item in arguments)
                    {
                        var argumentType = GetArgumentTypeName(item.Value);
                        sb.Append($"<member><name>{item.Key}</name><value><{argumentType}>{item.Value}</{argumentType}></value></member>");
                    }
                    sb.Append($"</struct></value></data></array></value></param>");
                }
            }
            else
            {
                foreach (var item in arguments)
                {
                    var argumentType = GetArgumentTypeName(item.Value);
                    sb.Append($"<param><value><{argumentType}>{item.Value}</{argumentType}></value></param>");
                }
            }
            sb.AppendLine("</params></methodCall>");
            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static KeyValuePair<string, object> Arg(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static KeyValuePair<string, object> Arg(object value)
        {
            return new KeyValuePair<string, object>("", value);
        }

        private static string GetArgumentTypeName(object item)
        {
            if (item is string) return "string";
            if (item is double) return "double";
            if (item is int) return "int";
            return "string";
        }

        private static byte[] ComputeMovieHash(string filename)
        {
            byte[] result;
            using (Stream input = File.OpenRead(filename))
            {
                result = ComputeMovieHash(input);
            }
            return result;
        }

        private static byte[] ComputeMovieHash(Stream input)
        {
            long lhash, streamsize;
            streamsize = input.Length;
            lhash = streamsize;

            long i = 0;
            byte[] buffer = new byte[sizeof(long)];
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }

            input.Position = Math.Max(0, streamsize - 65536);
            i = 0;
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }
            input.Close();
            byte[] result = BitConverter.GetBytes(lhash);
            Array.Reverse(result);
            return result;
        }

        private static string ToHexadecimal(byte[] bytes)
        {
            StringBuilder hexBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                hexBuilder.Append(bytes[i].ToString("x2"));
            }
            return hexBuilder.ToString();
        }

        internal class Subtitle
        {
            public string IdSubtitleFile { get; set; }
            public string SubFileName { get; set; }
            public string SubLanguageId { get; set; }
            public string LanguageName { get; set; }
            public string MovieReleaseName { get; set; }
            public string MovieName { get; set; }
            public string SubEncoding { get; set; }
            public string SubDownloadLink { get; set; }
            public string ZipDownloadLink { get; set; }
            public string SubtitleLink { get; set; }
            public double SubRating { get; set; }
        }
    }
}