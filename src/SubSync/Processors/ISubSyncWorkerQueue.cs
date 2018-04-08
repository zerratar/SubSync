using System;

namespace SubSync.Processors
{
    internal interface ISubSyncWorkerQueue : IDisposable
    {
        void Enqueue(string fullFilePath);
        void Enqueue(ISubSyncWorker worker);
        void Start();
        void Stop();
    }
}