namespace SubSync
{
    internal struct WorkerStatus
    {
        public readonly bool Succeeded;
        public readonly string Target;

        public WorkerStatus(bool succeeded, string target)
        {
            Succeeded = succeeded;
            Target = target;
        }
    }
}