using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubSync.Proivders
{
    internal class SubsceneSubtitleProvider : ISubSyncSubtitleProvider
    {
        private readonly HashSet<string> languages;
        private const string SearchApiUrlFormat = "https://subscene.com/subtitles/release?q={0}&r=true";
        private const string SubtitleApiUrlFormat = "https://subscene.com/{0}";

        private const int RequestRetryLimit = 3;
        private const int RequestTimeout = 3000;

        public SubsceneSubtitleProvider(HashSet<string> languages)
        {
            this.languages = languages;
        }

        public async Task<string> GetAsync(string name, string outputDirectory)
        {
            var url = await FindAsync(name);
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception($"No subtitles for {name} could befound");
            }

            var subtitlePageContent = await DownloadStringAsync(url);
            foreach (var language in this.languages)
            {
                var match = Regex.Match(subtitlePageContent, $@"\/subtitles\/{language}-text\/[a-zA-Z0-9_-]*");
                if (match.Success)
                {
                    var downloadUrl = string.Format(SubtitleApiUrlFormat, match.Value);
                    return await DownloadFileAsync(downloadUrl, outputDirectory);
                }
            }

            return null;
        }

        private async Task<string> FindAsync(string name)
        {
            var searchName = GetUrlFriendlyName(name);
            var searchUrl = string.Format(SearchApiUrlFormat, searchName);
            var searchPageContent = await DownloadStringAsync(searchUrl);

            foreach (var language in this.languages)
            {
                var match = Regex.Match(searchPageContent, $@"\/subtitles\/.*\/{language}\/[0-9]+");

                if (match.Success)
                {
                    return string.Format(SubtitleApiUrlFormat, match.Value);
                }
            }
            return null;
        }


        private async Task<string> DownloadFileAsync(string url, string outputDirectory, int retryCount = 0)
        {
            try
            {
                var filename = "download.zip";
                var req = HttpWebRequest.CreateHttp(url);
                req.Timeout = req.ReadWriteTimeout = RequestTimeout;
                using (var response = (HttpWebResponse)await req.GetResponseAsync())
                {
                    var contentDisposition = response.Headers.Get("Content-Disposition");
                    if (!string.IsNullOrEmpty(contentDisposition))
                    {
                        var newFileName = contentDisposition.Split('=').LastOrDefault();
                        if (!string.IsNullOrEmpty(newFileName))
                            filename = newFileName;
                    }

                    var outputFile = System.IO.Path.Combine(outputDirectory, filename);
                    using (var stream = response.GetResponseStream())
                    using (var output = new FileStream(outputFile, FileMode.Create))
                    {
                        int read = 0;
                        var buffer = new byte[4096];

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await output.WriteAsync(buffer, 0, read);
                        }

                        return outputFile;
                    }
                }
            }
            catch (WebException webException)
            {
                if ((int)webException.Status != 409 && webException.Status != WebExceptionStatus.ProtocolError) // 409: conflict
                {
                    throw new WebException($"Downloading file from url: {url} failed.", webException);
                }

                if (retryCount >= RequestRetryLimit)
                {
                    throw new WebException($"Downloading file from url: {url} failed after {retryCount} retries.", webException);
                }

                await Task.Delay(1000 * (retryCount + 1));
                return await DownloadFileAsync(url, outputDirectory, ++retryCount);
            }
        }

        private async Task<string> DownloadStringAsync(string url, int retryCount = 0)
        {
            try
            {
                var req = HttpWebRequest.CreateHttp(url);
                req.Timeout = req.ReadWriteTimeout = RequestTimeout;
                using (var response = await req.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var sr = new StreamReader(stream))
                {
                    return await sr.ReadToEndAsync();
                }
            }
            catch (WebException webException)
            {
                if ((int)webException.Status != 409 && webException.Status != WebExceptionStatus.ProtocolError) // 409: conflict
                {
                    throw new WebException($"Request to url: {url} failed.", webException);
                }

                if (retryCount >= RequestRetryLimit)
                {
                    throw new WebException($"Request to url: {url} failed after {retryCount} retries.", webException);
                }

                await Task.Delay(1000 * (retryCount + 1));
                return await DownloadStringAsync(url, ++retryCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetUrlFriendlyName(string name)
        {
            return WebUtility.UrlEncode(Path.GetFileNameWithoutExtension(name));
        }
    }
}