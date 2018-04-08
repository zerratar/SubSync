using System;

namespace SubSync
{
    internal interface IFileSystemWatcher : IDisposable
    {
        void Start();
        void Stop();
    }
}