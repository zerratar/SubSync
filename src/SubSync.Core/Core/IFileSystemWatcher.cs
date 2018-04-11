using System;

namespace SubSync
{
    internal interface IFileSystemWatcher
    {
        void Start();
        void Stop();
    }
}