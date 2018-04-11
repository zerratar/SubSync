namespace SubSync
{
    internal interface IWorkerProvider
    {
        IWorker GetWorker(IWorkerQueue queue, string file, int tryCount = 0);
    }
}
