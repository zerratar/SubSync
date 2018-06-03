namespace SubSyncLib.Logic
{
    public struct QueueProcessResult
    {
        public readonly int Total;
        public readonly int Succeeded;
        public readonly VideoFile[] Failed;

        public QueueProcessResult(int total, int succeeded, VideoFile[] failed)
        {
            Total = total;
            Succeeded = succeeded;
            Failed = failed;
        }
    }
}