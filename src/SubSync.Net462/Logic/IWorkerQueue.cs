using System;

namespace SubSync
{
    public interface IWorkerQueue : IDisposable
    {
        bool Enqueue(string fullFilePath);
        void Start();
        void Stop();
        void Reset();        
    }
}