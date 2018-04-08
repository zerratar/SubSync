using System;

namespace SubSync.Processors
{
    internal interface IWorkerQueue : IDisposable
    {
        void Enqueue(string fullFilePath);
        void Enqueue(IWorker worker);
        void Start();
        void Stop();
    }
}