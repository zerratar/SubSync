using System;

namespace SubSync
{
    public interface IFileSystemWatcher
    {
        void Start();
        void Stop();
    }
}