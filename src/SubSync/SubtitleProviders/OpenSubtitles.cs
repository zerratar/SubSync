using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SubSync
{
    internal class OpenSubtitles : SubtitleProviderBase
    {
        // https://www.opensubtitles.org
        public OpenSubtitles(HashSet<string> languages) : base(languages)
        {
        }

        protected override int RequestRetryLimit { get; } = 1;

        protected override int RequestTimeout { get; } = 3000;

        public override Task<string> GetAsync(string name, string outputDirectory)
        {
            throw new NotImplementedException();
        }
    }
}