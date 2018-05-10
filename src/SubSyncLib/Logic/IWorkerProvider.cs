namespace SubSyncLib.Logic
{
    public interface IWorkerProvider
    {
        IWorker GetWorker(IWorkerQueue queue, string file, int tryCount = 0);
    }
}
