using System;
using System.Collections.Generic;
using System.Threading;

namespace SubSyncLib.Logic
{
    public class QueueProcessReporter : IStatusReporter<WorkerStatus>, IStatusResultReporter<QueueProcessResult>
    {
        private readonly List<string> failed = new List<string>();
        private readonly object reportMutex = new object();
        private int total = 0;
        private int succeeded = 0;
        private int changes = 0;

        public event EventHandler<QueueProcessResult> OnReportFinished;

        public void Report(WorkerStatus data)
        {
            lock (reportMutex)
            {
                ++total;
                if (data.Succeeded)
                {
                    ++succeeded;
                }
                else
                {
                    failed.Add(data.Target);
                }
            }
            Interlocked.Increment(ref changes);
        }

        public void FinishReport()
        {
            var changeCount = Interlocked.Exchange(ref changes, 0);
            if (changeCount == 0)
            {
                return;
            }

            lock (reportMutex)
            {
                OnReportFinished?.Invoke(this, new QueueProcessResult(total, succeeded, failed.ToArray()));
                total = 0;
                succeeded = 0;
                failed.Clear();
            }
        }
    }
}