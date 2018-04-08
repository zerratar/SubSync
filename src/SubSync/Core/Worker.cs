using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SubSync.Processors;

namespace SubSync
{
    internal class Worker : IWorker
    {
        private const int RetryLimit = 5;
        private readonly string filePath;
        private readonly ILogger logger;
        private readonly IWorkerQueue workerQueue;
        private readonly ISubtitleProvider subtitleProvider;
        private readonly HashSet<string> subtitleExtensions;
        private static readonly HashSet<string> FileCompressionExtensions = new HashSet<string>
        {
            ".zip", ".rar", ".gzip", ".gz", ".7z", ".tar", ".tar.gz"
        };

        private readonly TaskCompletionSource<object> taskCompletionSource = new TaskCompletionSource<object>();

        private int syncCount;

        public Worker(
            string filePath,
            ILogger logger,
            IWorkerQueue workerQueue,
            ISubtitleProvider subtitleProvider,
            HashSet<string> subtitleExtensions)
        {
            this.filePath = filePath;
            this.logger = logger;
            this.workerQueue = workerQueue;
            this.subtitleProvider = subtitleProvider;
            this.subtitleExtensions = subtitleExtensions;
        }

        public Task SyncAsync()
        {
            try
            {
                return taskCompletionSource.Task;
            }
            finally
            {
                Task.Factory.StartNew(async () =>
                {
                    var counter = Interlocked.Increment(ref this.syncCount);
                    var file = new FileInfo(filePath);
                    this.logger.WriteLine($"Synchronizing {file.Name}");
                    try
                    {
                        var directory = file.Directory?.FullName ?? "./";
                        var outputName = await subtitleProvider.GetAsync(file.Name, directory);
                        var extension = Path.GetExtension(outputName);
                        if (IsCompressed(extension))
                        {
                            outputName = await DecompressAsync(outputName);
                        }

                        var finalName = Rename(outputName, Path.GetFileNameWithoutExtension(file.Name));
                        this.logger.WriteLine($"@gray@Subtitle @white@{Path.GetFileName(finalName)} @green@downloaded!");
                    }
                    catch (Exception exc)
                    {
                        this.logger.Error($"Synchronization of {file.Name} failed with: ${exc.Message}");

                        if (counter <= RetryLimit)
                        {
                            this.workerQueue.Enqueue(this);
                        }
                    }

                    this.taskCompletionSource.SetResult(true);
                }, TaskCreationOptions.LongRunning);
            }
        }

        public void Dispose() { }

        private string Rename(string fileToRename, string newFilaNameWithoutExtension)
        {
            var inFile = new FileInfo(fileToRename);
            var directory = inFile.Directory?.FullName ?? "./";
            var destFileName = Path.Combine(directory, newFilaNameWithoutExtension + Path.GetExtension(fileToRename));
            inFile.MoveTo(destFileName);
            //File.Move(fileToRename, destFileName);
            return destFileName;
        }

        private Task<string> DecompressAsync(string filename)
        {
            switch (Path.GetExtension(filename)?.ToLower())
            {
                case ".rar": return DecompressRarAsync(filename);
                case ".zip": return DecompressZipAsync(filename);
                case ".7z": return Decompress7ZipAsync(filename);
                case ".tar": return DecompressTarAsync(filename);
                case ".gz":
                case ".gzip":
                    return DecompressGZipAsync(filename);

                default:
                    throw new NotImplementedException($"The archive extension: {Path.GetExtension(filename)?.ToLower()} has not yet been implemented.");
            }
        }

        private async Task<string> DecompressArchive(string filename, Func<Stream, IArchive> archiveOpener)
        {
            var file = new FileInfo(filename);
            var targetFile = string.Empty;
            try
            {
                var fileDirectory = file.Directory;
                var directory = fileDirectory?.FullName ?? "./";
                using (var fileReader = file.OpenRead())
                using (var reader = archiveOpener(fileReader))
                {
                    foreach (var entry in reader.Entries)
                    {
                        var ext = Path.GetExtension(entry.Key);
                        if (entry.Key.ToLower().EndsWith(".srt.txt"))
                        {
                            ext = ".srt";
                        }

                        if (ext == null || !this.subtitleExtensions.Contains(ext.ToLower()))
                        {
                            continue;
                        }

                        targetFile = Path.Combine(directory, Path.ChangeExtension(entry.Key.Replace("?", ""), ext));
                        var dir = new FileInfo(targetFile).Directory;
                        if (dir != null && !dir.Exists)
                        {
                            dir.Create();
                        }

                        var targetFileInfo = new FileInfo(targetFile);
                        using (var entryStream = entry.OpenEntryStream())
                        using (var sw = targetFileInfo.Create())//new FileStream(targetFile, FileMode.Create))
                        {
                            var read = 0;
                            var buffer = new byte[4096];
                            while ((read = await entryStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await sw.WriteAsync(buffer, 0, read);
                            }

                            return targetFile;
                        }
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(targetFile))
                {
                    file.Delete();
                }
            }

            throw new FileNotFoundException($"No suitable subtitle found in the downloaded archive, {filename}. Archive kept just in case.");
        }

        private Task<string> DecompressRarAsync(string filename) => DecompressArchive(filename, x => SharpCompress.Archives.Rar.RarArchive.Open(x));
        private Task<string> DecompressZipAsync(string filename) => DecompressArchive(filename, x => SharpCompress.Archives.Zip.ZipArchive.Open(x));
        private Task<string> DecompressGZipAsync(string filename) => DecompressArchive(filename, x => SharpCompress.Archives.GZip.GZipArchive.Open(x));
        private Task<string> Decompress7ZipAsync(string filename) => DecompressArchive(filename, x => SharpCompress.Archives.SevenZip.SevenZipArchive.Open(x));
        private Task<string> DecompressTarAsync(string filename) => DecompressArchive(filename, x => SharpCompress.Archives.Tar.TarArchive.Open(x));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCompressed(string extension) => FileCompressionExtensions.Contains(extension.ToLower());
    }
}