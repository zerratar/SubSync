using System;

namespace SubSync
{
    internal interface IWorkerQueue : IDisposable
    {
        event EventHandler<QueueCompletedEventArgs> QueueCompleted;
        void Enqueue(string fullFilePath);
        void Enqueue(IWorker worker);
        void Start();
        void Stop();
    }
}