using System.Collections.Generic;

namespace SubSync
{
    internal class WorkerProvider : IWorkerProvider
    {
        private readonly ILogger logger;
        private readonly ISubtitleProvider subtitleProvider;
        private readonly HashSet<string> subtitleExtensions;

        public WorkerProvider(
            ILogger logger,
            HashSet<string> subtitleExtensions,
            ISubtitleProvider subtitleProvider)
        {
            this.logger = logger;
            this.subtitleProvider = subtitleProvider;
            this.subtitleExtensions = subtitleExtensions;
        }

        public IWorker GetWorker(IWorkerQueue queue, string file)
        {
            return new Worker(
                file,
                this.logger,
                queue,
                this.subtitleProvider,                
                subtitleExtensions);
        }
    }
}