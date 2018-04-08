using System;

namespace SubSync
{
    internal class QueueCompletedEventArgs : EventArgs
    {
        public QueueCompletedEventArgs(int total, int succeeded, int failed)
        {
            Total = total;
            Succeeded = succeeded;
            Failed = failed;
        }

        public int Total { get; }
        public int Succeeded { get; }
        public int Failed { get; }
    }
}