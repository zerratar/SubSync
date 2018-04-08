using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SubSync.Proivders;

namespace SubSync.Processors
{
    internal class SubSyncWorkerQueue : ISubSyncWorkerQueue
    {
        private const int ConcurrentWorkers = 3;
        private readonly ISubSyncWorkerProvider workerProvider;
        private readonly ConcurrentQueue<ISubSyncWorker> queue = new ConcurrentQueue<ISubSyncWorker>();
        private readonly Thread workerThread;
        private bool enabled;
        private bool disposed;

        public SubSyncWorkerQueue(ISubSyncWorkerProvider workerProvider)
        {
            this.workerProvider = workerProvider;
            this.workerThread = new Thread(ProcessQueue);
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.Stop();
            this.disposed = true;
        }

        public void Enqueue(string fullFilePath)
        {
            this.Enqueue(this.workerProvider.GetWorker(this, fullFilePath));
        }

        public void Enqueue(ISubSyncWorker worker)
        {
            queue.Enqueue(worker);
        }

        public void Start()
        {
            if (enabled) return;
            enabled = true;
            workerThread.Start();
        }

        public void Stop()
        {
            if (!enabled) return;
            enabled = false;
            workerThread.Join();
        }

        private async void ProcessQueue()
        {
            var activeJobs = new List<Task>();
            do
            {
                while (activeJobs.Count < ConcurrentWorkers && this.queue.TryDequeue(out var worker))
                {
                    activeJobs.Add(worker.SyncAsync());
                }

                if (activeJobs.Count > 0)
                {
                    await Task.WhenAny(activeJobs);

                    activeJobs = activeJobs.Where(x => !x.IsCompleted).ToList();
                }
                else
                {
                    System.Threading.Thread.Sleep(100);
                }

            } while (this.enabled);
        }
    }
}