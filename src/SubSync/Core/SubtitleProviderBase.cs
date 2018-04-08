using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SubSync
{
    internal abstract class SubtitleProviderBase : ISubtitleProvider
    {
        protected readonly HashSet<string> Languages;
        
        protected SubtitleProviderBase(HashSet<string> languages)
        {
            this.Languages = languages;
        }

        protected abstract int RequestRetryLimit { get; }

        protected abstract int RequestTimeout { get; }

        public abstract Task<string> GetAsync(string name, string outputDirectory);

        protected async Task<string> DownloadFileAsync(string url, string outputDirectory, int retryCount = 0)
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
                    {
                        var file = new FileInfo(outputFile);
                        using (var output = file.Create())
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

        protected async Task<string> DownloadStringAsync(string url, int retryCount = 0)
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
        protected static string GetUrlFriendlyName(string name)
        {
            return WebUtility.UrlEncode(Path.GetFileNameWithoutExtension(name));
        }
    }
}