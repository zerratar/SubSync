using System;
using System.Threading.Tasks;

namespace SubSync
{
    public interface IWorker : IDisposable
    {
        Task SyncAsync();
    }
}
