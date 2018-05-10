using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SubSyncLib.Logic
{
    public class WorkerQueue : IWorkerQueue
    {
        private const int ConcurrentWorkers = 5;
        private readonly IWorkerProvider workerProvider;
        private readonly IStatusReporter statusReporter;
        private readonly ConcurrentQueue<IWorker> queue = new ConcurrentQueue<IWorker>();
        private readonly ConcurrentDictionary<string, int> queueTries = new ConcurrentDictionary<string, int>();
        private readonly Thread workerThread;
        private bool enabled;
        private bool disposed;

        // the max times the same item can be enqueued.
        private const int RetryLimit = 3;

        public WorkerQueue(IWorkerProvider workerProvider, IStatusReporter statusReporter)
        {
            this.workerProvider = workerProvider;
            this.statusReporter = statusReporter;
            this.workerThread = new Thread(ProcessQueue);
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.Stop();
            this.disposed = true;
        }

        public bool Enqueue(string fullFilePath)
        {
            queueTries.TryGetValue(fullFilePath, out var tries);
            if (tries < RetryLimit)
            {
                queueTries[fullFilePath] = tries + 1;
                queue.Enqueue(this.workerProvider.GetWorker(this, fullFilePath, tries));
                return true;
            }

            return false;
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

        public void Reset()
        {
            while (queue.TryDequeue(out _)) ;
            queueTries.Clear();
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
                    statusReporter.FinishReport();// should only report if it has been set dirty. This happens only if someone has reported data.                                        
                    Thread.Sleep(100);
                }

            } while (this.enabled);
        }
    }
}