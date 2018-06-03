namespace SubSyncLib.Logic
{
    public struct WorkerStatus
    {
        public readonly bool Succeeded;
        public readonly VideoFile Target;

        public WorkerStatus(bool succeeded, VideoFile target)
        {
            Succeeded = succeeded;
            Target = target;
        }
    }
}