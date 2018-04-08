using System.Collections.Generic;
using SubSync.Processors;

namespace SubSync.Proivders
{
    internal class SubSyncWorkerProvider : ISubSyncWorkerProvider
    {
        private readonly ILogger logger;
        private readonly ISubSyncSubtitleProvider subtitleProvider;
        private readonly HashSet<string> subtitleExtensions;

        public SubSyncWorkerProvider(
            ILogger logger,
            ISubSyncSubtitleProvider subtitleProvider,            
            HashSet<string> subtitleExtensions)
        {
            this.logger = logger;
            this.subtitleProvider = subtitleProvider;
            this.subtitleExtensions = subtitleExtensions;
        }

        public ISubSyncWorker GetWorker(ISubSyncWorkerQueue queue, string file)
        {
            return new SubSyncWorker(
                file,
                this.logger,
                queue,
                this.subtitleProvider,                
                subtitleExtensions);
        }
    }
}