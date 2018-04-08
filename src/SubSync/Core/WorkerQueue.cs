using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SubSync
{
    internal class WorkerQueue : IWorkerQueue
    {
        public event EventHandler<QueueCompletedEventArgs> QueueCompleted;
        private const int ConcurrentWorkers = 7;
        private readonly IWorkerProvider workerProvider;
        private readonly ConcurrentQueue<IWorker> queue = new ConcurrentQueue<IWorker>();
        private readonly Thread workerThread;
        private bool enabled;
        private bool disposed;
        private int queueSize = 0;

        public WorkerQueue(IWorkerProvider workerProvider)
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

        public void Enqueue(IWorker worker)
        {
            Interlocked.Increment(ref queueSize);
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
            var resultReported = false;
            var failed = 0;
            var succeeded = 0;
            do
            {
                while (activeJobs.Count < ConcurrentWorkers && this.queue.TryDequeue(out var worker))
                {
                    activeJobs.Add(worker.SyncAsync());
                }

                if (activeJobs.Count > 0)
                {
                    resultReported = false;
                    await Task.WhenAny(activeJobs);
                    failed += activeJobs.Count(x => x.IsFaulted && x.IsCompleted);
                    succeeded += activeJobs.Count(x => !x.IsFaulted && x.IsCompleted);
                    activeJobs = activeJobs.Where(x => !x.IsCompleted).ToList();
                }
                else
                {
                    if (!resultReported && QueueCompleted != null)
                    {
                        QueueCompleted?.Invoke(this, new QueueCompletedEventArgs(Volatile.Read(ref queueSize), succeeded, failed));
                        resultReported = true;
                    }

                    succeeded = 0;
                    failed = 0;
                    Volatile.Write(ref queueSize, 0);
                    System.Threading.Thread.Sleep(100);
                }

            } while (this.enabled);
        }
    }
}