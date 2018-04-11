using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SubSync.Extensions;

namespace SubSync
{
    internal class SubtitleSynchronizer : IFileSystemWatcher, IDisposable
    {
        private readonly ILogger logger;
        private readonly IWorkerQueue workerQueue;
        private readonly IStatusResultReporter<QueueProcessResult> statusReporter;
        private readonly string input;
        private readonly HashSet<string> videoExtensions;
        private readonly HashSet<string> subtitleExtensions;
        private System.IO.FileSystemWatcher fsWatcher;
        private bool disposed;
        private int skipped = 0;

        public SubtitleSynchronizer(
            ILogger logger,
            IWorkerQueue workerQueue,
            IStatusResultReporter<QueueProcessResult> statusReporter,
            string input,
            HashSet<string> videoExtensions,
            HashSet<string> subtitleExtensions)
        {
            this.logger = logger;
            this.workerQueue = workerQueue;
            this.statusReporter = statusReporter;
            this.input = input;
            this.videoExtensions = videoExtensions;
            this.subtitleExtensions = subtitleExtensions;
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.Stop();
            this.fsWatcher?.Dispose();
            this.workerQueue.Dispose();
            disposed = true;
        }

        public void Start()
        {
            if (this.fsWatcher != null) return;
            this.fsWatcher = new System.IO.FileSystemWatcher(this.input, "*.*");
            this.fsWatcher.Error += FsWatcherOnError;
            this.fsWatcher.IncludeSubdirectories = true;
            this.fsWatcher.EnableRaisingEvents = true;
            this.fsWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName |
                                          NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                                          NotifyFilters.Size | NotifyFilters.Security;

            this.fsWatcher.Created += FileCreated;
            this.fsWatcher.Renamed += FileCreated;
            this.statusReporter.OnReportFinished += ResultReport;
            this.workerQueue.Start();
            this.SyncAll();
        }

        public void Stop()
        {
            if (this.fsWatcher == null) return;
            this.statusReporter.OnReportFinished -= ResultReport;
            this.workerQueue.Stop();
            this.fsWatcher.Error -= FsWatcherOnError;
            this.fsWatcher.Created -= FileCreated;
            this.fsWatcher.Renamed -= FileCreated;
            this.fsWatcher.Dispose();
            this.fsWatcher = null;
        }

        private void ResultReport(object sender, QueueProcessResult result)
        {
            var skipcount = Interlocked.Exchange(ref skipped, 0);
            var total = result.Total;
            var failed = result.Failed;
            var success = result.Succeeded;

            if (total == 0 && skipcount == 0)
            {
                return;
            }

            this.logger.WriteLine($"");
            this.logger.WriteLine($" ═════════════════════════════════════════════════════");
            this.logger.WriteLine($"");
            this.logger.WriteLine($" @whi@Synchronization completed with a total of @yel@{total} @whi@video(s) processed.");

            this.logger.WriteLine($"    {skipcount} video(s) was skipped.");
            if (success > 0)
            {
                this.logger.WriteLine($"    @green@{success} @whi@video(s) was successefully was synchronized.");
            }
            if (failed.Length > 0)
            {
                this.logger.WriteLine($"    @red@{failed.Length} @whi@video(s) failed to synchronize.");
                foreach (var failedItem in failed)
                {
                    this.logger.WriteLine($"    @red@* {System.IO.Path.GetFileName(failedItem)}");
                }
            }

        }

        public void SyncAll()
        {
            var directoryInfo = new DirectoryInfo(this.input);
            this.workerQueue.Reset();
            this.videoExtensions.SelectMany(y => directoryInfo.GetFiles($"*{y}", SearchOption.AllDirectories)).Select(x => x.FullName)
                .ForEach(Sync);
        }

        private void Sync(string fullFilePath)
        {
            try
            {
                if (IsSynchronized(fullFilePath))
                {
                    Interlocked.Increment(ref skipped);
                    return;
                }

                this.workerQueue.Enqueue(fullFilePath);
            }
            catch (Exception exc)
            {
                this.logger.Error($"Unable to sync subtitles for @yellow@{fullFilePath} @red@, reason: {exc.Message}.");
            }
        }

        private void FsWatcherOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            this.logger.Error($"PANIC!! Fatal Media Watcher Error !! {errorEventArgs.GetException().Message}");
        }

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            if (!videoExtensions.Contains(System.IO.Path.GetExtension(e.FullPath)))
            {
                return;
            }

            this.Sync(e.FullPath);
        }


        private bool IsSynchronized(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (BlacklistedFile(fileInfo.Name))
            {
                return true;
            }
            var directory = fileInfo.Directory;
            if (directory == null)
            {
                throw new NullReferenceException(nameof(directory));
            }

            return this.subtitleExtensions
                .SelectMany(x =>
                    directory.GetFiles($"{Path.GetFileNameWithoutExtension(fileInfo.Name)}{x}", SearchOption.AllDirectories))
                .Any();
        }

        private bool BlacklistedFile(string filename)
        {
            // todo: make this configurable. but for now, ignore all sample.<vid ext> files.
            return Path.GetFileNameWithoutExtension(filename).Equals("sample", StringComparison.OrdinalIgnoreCase);
        }
    }
}