using System;

namespace SubSync.Processors
{
    internal interface IFileSystemWatcher : IDisposable
    {
        void Start();
        void Stop();
    }
}