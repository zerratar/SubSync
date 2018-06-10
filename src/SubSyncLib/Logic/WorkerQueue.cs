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
        private readonly SubSyncSettings settings;
        private readonly IWorkerProvider workerProvider;
        private readonly IStatusReporter statusReporter;
        private readonly ConcurrentQueue<IWorker> queue = new ConcurrentQueue<IWorker>();
        private readonly ConcurrentDictionary<string, int> queueTries = new ConcurrentDictionary<string, int>();
        private readonly Thread workerThread;
        private bool enabled;
        private bool disposed;

        private int activeJobCount = 0;


        // the max times the same item can be enqueued.
        private const int RetryLimit = 3;

        public WorkerQueue(SubSyncSettings settings, IWorkerProvider workerProvider, IStatusReporter statusReporter)
        {
            this.settings = settings;
            this.workerProvider = workerProvider;
            this.statusReporter = statusReporter;
            workerThread = new Thread(ProcessQueue);
        }

        public int Count => queue.Count;

        public int Active => activeJobCount;

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
        }

        public bool Enqueue(VideoFile video)
        {
            queueTries.TryGetValue(video.Name, out var tries);
            if (tries < RetryLimit)
            {
                queueTries[video.Name] = tries + 1;
                queue.Enqueue(workerProvider.GetWorker(this, video, tries));
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
                while (activeJobs.Count < ConcurrentWorkers && queue.TryDequeue(out var worker))
                {
                    Interlocked.Increment(ref activeJobCount);

                    if (settings.MinimummDelayBetweenRequests > 0)
                    {
                        await Task.Delay(settings.MinimummDelayBetweenRequests);
                    }

                    activeJobs.Add(worker.SyncAsync());
                }

                if (activeJobs.Count > 0)
                {
                    await Task.WhenAny(activeJobs);
                    activeJobs = activeJobs.Where(x => !x.IsCompleted).ToList();

                    Volatile.Write(ref activeJobCount, activeJobs.Count);
                }
                else
                {
                    statusReporter.FinishReport();// should only report if it has been set dirty. This happens only if someone has reported data.                                        
                    Thread.Sleep(100);
                }

            } while (enabled);
        }
    }
}