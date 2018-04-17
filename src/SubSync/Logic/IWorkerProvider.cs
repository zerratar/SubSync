namespace SubSync
{
    public interface IWorkerProvider
    {
        IWorker GetWorker(IWorkerQueue queue, string file, int tryCount = 0);
    }
}
