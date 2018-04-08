using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SubSync
{
    internal class FallbackSubtitleProvider : ISubtitleProvider
    {
        private readonly ConcurrentDictionary<string, int> providerCache = new ConcurrentDictionary<string, int>();
        private readonly ISubtitleProvider[] _providers;
        private readonly int MaxRetryCount = 3;

        public FallbackSubtitleProvider(params ISubtitleProvider[] providers)
        {
            _providers = providers;
            MaxRetryCount = Math.Max(MaxRetryCount, _providers.Length);
        }

        public async Task<string> GetAsync(string name, string outputDirectory)
        {
            providerCache.TryGetValue(name, out var index);
            try
            {
                var result = await _providers[index].GetAsync(name, outputDirectory);
                providerCache.TryRemove(name, out _);
                return result;
            }
            catch (Exception exc)
            {
                providerCache[name] = index + 1;
                if (index + 1 > MaxRetryCount || index + 1 >= _providers.Length)
                {
                    providerCache.TryRemove(name, out _);
                    throw exc;
                }

                return await this.GetAsync(name, outputDirectory);
            }
        }
    }
}