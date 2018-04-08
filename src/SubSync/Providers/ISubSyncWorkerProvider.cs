using SubSync.Processors;

namespace SubSync.Proivders
{
    internal interface ISubSyncWorkerProvider
    {
        ISubSyncWorker GetWorker(ISubSyncWorkerQueue queue, string file);
    }
}
