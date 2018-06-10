using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using SubSyncLib.Logic.Extensions;

namespace SubSyncLib.Logic
{
    public class SubtitleSynchronizer : IFileSystemWatcher, IDisposable
    {
        private readonly ILogger logger;
        private readonly IVideoSyncList syncList;
        private readonly IWorkerQueue workerQueue;
        private readonly IStatusResultReporter<QueueProcessResult> statusReporter;
        private readonly IVideoIgnoreFilter videoIgnore;
        private readonly SubSyncSettings settings;
        private FileSystemWatcher fsWatcher;
        private bool disposed;
        private int skipped = 0;

        public SubtitleSynchronizer(
            ILogger logger,
            IVideoSyncList syncList,
            IWorkerQueue workerQueue,
            IStatusResultReporter<QueueProcessResult> statusReporter,
            IVideoIgnoreFilter videoIgnore,
            SubSyncSettings settings)
        {
            this.logger = logger;
            this.syncList = syncList;
            this.workerQueue = workerQueue;
            this.statusReporter = statusReporter;
            this.videoIgnore = videoIgnore;
            this.settings = settings;
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            fsWatcher?.Dispose();
            workerQueue.Dispose();
            disposed = true;
        }

        public void Start()
        {
            if (fsWatcher != null) return;
            fsWatcher = new FileSystemWatcher(settings.Input, "*.*");
            fsWatcher.Error += FsWatcherOnError;
            fsWatcher.IncludeSubdirectories = true;
            fsWatcher.EnableRaisingEvents = true;
            fsWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName |
                                          NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                                          NotifyFilters.Size | NotifyFilters.Security;

            fsWatcher.Created += FileCreated;
            fsWatcher.Renamed += FileCreated;
            statusReporter.OnReportFinished += ResultReport;
            workerQueue.Start();
            SyncAll();
        }

        private void StopAndExit()
        {
            Stop();
            Environment.Exit(0);
        }

        public void Stop()
        {
            if (fsWatcher == null) return;
            statusReporter.OnReportFinished -= ResultReport;
            workerQueue.Stop();
            fsWatcher.Error -= FsWatcherOnError;
            fsWatcher.Created -= FileCreated;
            fsWatcher.Renamed -= FileCreated;
            fsWatcher.Dispose();
            fsWatcher = null;
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

            logger.WriteLine($"");
            logger.WriteLine($" ═════════════════════════════════════════════════════");
            logger.WriteLine($"");
            logger.WriteLine($" @whi@Synchronization completed with a total of @yel@{total} @whi@video(s) processed.");

            logger.WriteLine($"    {skipcount} video(s) was skipped.");
            if (success > 0)
            {
                logger.WriteLine($"    @green@{success} @whi@video(s) was successefully was synchronized.");
            }
            if (failed.Length > 0)
            {
                logger.WriteLine($"    @red@{failed.Length} @whi@video(s) failed to synchronize.");
                foreach (var failedItem in failed)
                {
                    logger.WriteLine($"    @red@* {failedItem.Name}");
                }
            }

            syncList.Save();

            if (settings.ExitAfterSync)
            {
                StopAndExit();
            }
        }

        public void SyncAll()
        {
            var directoryInfo = new DirectoryInfo(settings.Input);
            workerQueue.Reset();
            settings.VideoExt
                .SelectMany(y => directoryInfo.GetFiles($"*{y}", SearchOption.AllDirectories)).Select(x => x.FullName)
                .ForEach(Sync);

            if (settings.ExitAfterSync
                && workerQueue.Count == 0
                && workerQueue.Active == 0)
            {
                StopAndExit();
            }
        }

        private void Sync(string fullFilePath)
        {
            try
            {
                var video = new VideoFile(fullFilePath);

                if (IsSynchronized(video))
                {
                    Interlocked.Increment(ref skipped);
                    return;
                }

                workerQueue.Enqueue(video);
            }
            catch (Exception exc)
            {
                logger.Error($"Unable to sync subtitles for @yellow@{fullFilePath} @red@, reason: {exc.Message}.");
            }
        }

        private void FsWatcherOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            logger.Error($"PANIC!! Fatal Media Watcher Error !! {errorEventArgs.GetException().Message}");
        }

        private void FileCreated(object sender, FileSystemEventArgs e)
        {
            if (!settings.VideoExt.Contains(Path.GetExtension(e.FullPath)))
            {
                return;
            }

            Sync(e.FullPath);
        }

        private bool IsSynchronized(VideoFile videoFile)
        {
            if (settings.ResyncAll)
            {
                // regardless, redownload this subtitle
                return false;
            }


            if (BlacklistedFile(videoFile))
            {
                return true;
            }

            if (videoIgnore.Match(videoFile))
            {
                return true;
            }

            if (videoFile.Directory == null)
            {
                throw new NullReferenceException(nameof(videoFile.Directory));
            }

            var hasSubtitleFile = HasSubtitleFile(settings, videoFile);

            // check if in sync list
            if (syncList.Contains(videoFile) && hasSubtitleFile)
            {
                return true;
            }

            if (settings.Resync)
            {
                // normal --resync flag just means we want to redownload any subtitles subsync has not synced.
                return false;
            }

            return hasSubtitleFile;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool BlacklistedFile(VideoFile video)
        {
            // todo: make this configurable. but for now, ignore all sample.<vid ext> files.
            return Path.GetFileNameWithoutExtension(video.Name).Equals("sample", StringComparison.OrdinalIgnoreCase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasSubtitleFile(SubSyncSettings settings, VideoFile videoFile)
        {
            return settings.SubtitleExt.SelectMany(x =>
                    videoFile.Directory.GetFiles($"{Path.GetFileNameWithoutExtension(videoFile.Name)}{x}", SearchOption.AllDirectories))
                .Any();
        }
    }

    public class VideoFile
    {
        private readonly FileInfo fileInfo;

        public VideoFile(string fullFilePath)
        {
            FilePath = fullFilePath;
            fileInfo = new FileInfo(fullFilePath);
            //Hash = Utilities.ComputeMovieHash(fullFilePath);
            //HashString = Utilities.ToHexadecimal(Hash);
        }

        public string FilePath { get; }
        //public byte[] Hash { get; }
        //public string HashString { get; }
        public string Name => fileInfo.Name;
        public DirectoryInfo Directory => fileInfo.Directory;
    }
}