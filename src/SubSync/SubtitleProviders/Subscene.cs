using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SubSync
{
    internal class Subscene : SubtitleProviderBase
    {
        private const string SearchApiUrlFormat = "https://subscene.com/subtitles/release?q={0}&r=true";
        private const string SubtitleApiUrlFormat = "https://subscene.com/{0}";

        public Subscene(HashSet<string> languages) : base(languages)
        {
        }

        protected override int RequestRetryLimit { get; } = 3;

        protected override int RequestTimeout { get; } = 3000;

        public override async Task<string> GetAsync(string name, string outputDirectory)
        {
            var url = await FindAsync(name);
            if (string.IsNullOrEmpty(url))
            {
                throw new Exception($"No subtitles for {name} could befound");
            }

            var subtitlePageContent = await DownloadStringAsync(url);
            foreach (var language in this.Languages)
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

            foreach (var language in this.Languages)
            {
                var match = Regex.Match(searchPageContent, $@"\/subtitles\/.*\/{language}\/[0-9]+");

                if (match.Success)
                {
                    return string.Format(SubtitleApiUrlFormat, match.Value);
                }
            }
            return null;
        }
    }
}