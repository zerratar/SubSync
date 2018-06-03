using System;

namespace SubSyncLib.Logic
{
    public interface IWorkerQueue : IDisposable
    {
        bool Enqueue(VideoFile video);
        void Start();
        void Stop();
        void Reset();
        int Count { get; }
        int Active { get; }
    }
}