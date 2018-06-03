using System;

namespace SubSyncLib.Logic
{
    public interface IWorkerQueue : IDisposable
    {
        bool Enqueue(string fullFilePath);
        void Start();
        void Stop();
        void Reset();
        int Count { get; }
        int Active { get; }
    }
}