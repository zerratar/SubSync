using System.Collections.Generic;

namespace SubSyncLib.Logic
{
    public class WorkerProvider : IWorkerProvider
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

        public IWorker GetWorker(IWorkerQueue queue, VideoFile video, int tryCount = 0)
        {
            return new Worker(
                video,
                logger,
                queue,
                subtitleProvider,
                statusReporter,
                subtitleExtensions,
                tryCount);
        }
    }
}