using SubSync.Processors;

namespace SubSync
{
    internal interface IWorkerProvider
    {
        IWorker GetWorker(IWorkerQueue queue, string file);
    }
}
