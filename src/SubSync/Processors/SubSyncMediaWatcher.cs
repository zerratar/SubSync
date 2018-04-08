using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SubSync.Extensions;

namespace SubSync.Processors
{
    internal class SubSyncMediaWatcher : ISubSyncMediaWatcher
    {
        private readonly ILogger logger;
        private readonly ISubSyncWorkerQueue workerQueue;
        private readonly string input;
        private readonly HashSet<string> videoExtensions;
        private readonly HashSet<string> subtitleExtensions;
        private FileSystemWatcher fsWatcher;
        private bool disposed;

        public SubSyncMediaWatcher(
            ILogger logger,
            ISubSyncWorkerQueue workerQueue,
            string input,
            HashSet<string> videoExtensions,
            HashSet<string> subtitleExtensions)
        {
            this.logger = logger;
            this.workerQueue = workerQueue;
            this.input = input;
            this.videoExtensions = videoExtensions;
            this.subtitleExtensions = subtitleExtensions;
        }

        public void Dispose()
        {
            if (this.disposed) return;
            this.fsWatcher?.Dispose();
            this.workerQueue.Dispose();
            disposed = true;
        }

        public void Start()
        {
            if (this.fsWatcher != null) return;
            this.fsWatcher = new FileSystemWatcher(this.input, "*.*");
            this.fsWatcher.Error += FsWatcherOnError;
            this.fsWatcher.IncludeSubdirectories = true;
            this.fsWatcher.EnableRaisingEvents = true;
            this.fsWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName |
                                          NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                                          NotifyFilters.Size | NotifyFilters.Security;

            this.fsWatcher.Created += FileCreated;
            this.workerQueue.Start();
            this.SyncAll();
        }

        public void Stop()
        {
            if (this.fsWatcher == null) return;
            this.workerQueue.Stop();
            this.fsWatcher.Error -= FsWatcherOnError;
            this.fsWatcher.Created -= FileCreated;
            this.fsWatcher.Dispose();
            this.fsWatcher = null;
        }

        public void SyncAll()
        {

            this.videoExtensions.SelectMany(y =>
                    new DirectoryInfo(this.input).GetFiles($"*{y}", SearchOption.AllDirectories))
                    .Select(x => x.FullName)
                //Directory.GetFiles(this.input, $"*{y}", SearchOption.AllDirectories))
                .ForEach(Sync);
        }

        private void Sync(string fullFilePath)
        {
            try
            {
                if (IsSynchronized(fullFilePath))
                {
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
    }
}