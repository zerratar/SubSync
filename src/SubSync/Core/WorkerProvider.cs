using System.Collections.Generic;

namespace SubSync
{
    internal class WorkerProvider : IWorkerProvider
    {
        private readonly ILogger logger;
        private readonly ISubtitleProvider subtitleProvider;
        private readonly IStatusReporter<WorkerStatus> statusReporter;
        private readonly HashSet<string> subtitleExtensions;

        public WorkerProvider(
            ILogger logger,
            HashSet<string> subtitleExtensions,
            ISubtitleProvider subtitleProvider,
            IStatusReporter<WorkerStatus> statusReporter)
        {
            this.logger = logger;
            this.subtitleProvider = subtitleProvider;
            this.statusReporter = statusReporter;
            this.subtitleExtensions = subtitleExtensions;
        }

        public IWorker GetWorker(IWorkerQueue queue, string file, int tryCount = 0)
        {
            return new Worker(
                file,
                logger,
                queue,
                subtitleProvider,
                statusReporter,
                subtitleExtensions,
                tryCount);
        }
    }
}