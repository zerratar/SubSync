using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SubSyncLib.Logic
{
    public class FallbackSubtitleProvider : ISubtitleProvider, IDisposable
    {
        private readonly ConcurrentDictionary<string, int> providerCache = new ConcurrentDictionary<string, int>();
        private readonly IVideoSyncList syncList;
        private readonly ISubtitleProvider[] _providers;
        private readonly int MaxRetryCount = 3;
        private bool disposed;

        public FallbackSubtitleProvider(IVideoSyncList syncList, params ISubtitleProvider[] providers)
        {
            this.syncList = syncList;
            _providers = providers;
            MaxRetryCount = Math.Max(MaxRetryCount, _providers.Length);
        }

        public async Task<string> GetAsync(VideoFile video)
        {
            providerCache.TryGetValue(video.HashString, out var index);
            try
            {
                var result = await _providers[index].GetAsync(video);
                providerCache.TryRemove(video.HashString, out _);
                syncList.Add(video);
                return result;
            }
            catch (Exception exc)
            {
                providerCache[video.HashString] = index + 1;
                if (index + 1 > MaxRetryCount || index + 1 >= _providers.Length)
                {
                    providerCache.TryRemove(video.HashString, out _);
                    throw exc;
                }

                return await GetAsync(video);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (var provider in _providers)
            {
                try
                {
                    if (provider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}