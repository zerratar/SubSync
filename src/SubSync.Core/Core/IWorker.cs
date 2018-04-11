using System;
using System.Threading.Tasks;

namespace SubSync
{
    internal interface IWorker : IDisposable
    {
        Task SyncAsync();
    }
}
