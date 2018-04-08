using System;

namespace SubSync.Processors
{
    internal interface ISubSyncMediaWatcher : IDisposable
    {
        void Start();
        void Stop();
    }
}