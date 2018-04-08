using System;
using System.Threading.Tasks;

namespace SubSync.Processors
{
    internal interface ISubSyncWorker : IDisposable
    {
        Task SyncAsync();
    }
}
