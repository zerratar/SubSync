using System;
using System.Threading.Tasks;

namespace SubSyncLib.Logic
{
    public interface IWorker : IDisposable
    {
        Task SyncAsync();
    }
}
