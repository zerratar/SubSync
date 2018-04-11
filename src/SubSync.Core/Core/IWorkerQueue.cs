using System;

namespace SubSync
{
    internal interface IWorkerQueue : IDisposable
    {
        bool Enqueue(string fullFilePath);
        void Start();
        void Stop();
        void Reset();        
    }
}