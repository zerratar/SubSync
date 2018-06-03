namespace SubSyncLib.Logic
{
    public interface IWorkerProvider
    {
        IWorker GetWorker(IWorkerQueue queue, VideoFile video, int tryCount = 0);
    }
}
