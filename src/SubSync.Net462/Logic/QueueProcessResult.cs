namespace SubSync
{
    public struct QueueProcessResult
    {
        public readonly int Total;
        public readonly int Succeeded;
        public readonly string[] Failed;

        public QueueProcessResult(int total, int succeeded, string[] failed)
        {
            Total = total;
            Succeeded = succeeded;
            Failed = failed;
        }
    }
}