using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SubSync.Net462
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = "./";
            var videoExtensions = ParseList("*.avi;*.mp4;*.mkv;*.mpeg;*.flv;*.webm");
            var subtitleExtensions = ParseList("*.srt;*.txt;*.sub;*.idx;*.ssa;*.ass");
            var languages = ParseList("english");

            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                input = args[0];
            }

            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                languages = ParseList(args[1]);
            }

            if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
            {
                videoExtensions = ParseList(args[2]);
            }

            var version = GetVersion();
            var logger = new ConsoleLogger();

            using (var fallbackSubtitleProvider = new FallbackSubtitleProvider(
                new OpenSubtitles(languages, new FileBasedCredentialsProvider("opensubtitle.auth", logger)),
                new Subscene(languages)))
            {
                var resultReporter = new QueueProcessReporter();
                var subSyncWorkerProvider = new WorkerProvider(logger, subtitleExtensions, fallbackSubtitleProvider, resultReporter);
                var subSyncWorkerQueue = new WorkerQueue(subSyncWorkerProvider, resultReporter);

                using (var mediaWatcher = new SubtitleSynchronizer(logger, subSyncWorkerQueue, resultReporter, input, videoExtensions, subtitleExtensions))
                {
                    logger.WriteLine("╔════════════════════════════════════════════╗");
                    logger.WriteLine("║   @whi@SubSync v" + version.PadRight(30 - version.Length) + "@gray@         ║");
                    logger.WriteLine("║   --------------------------------------   ║");
                    logger.WriteLine("║   Copyright (c) 2018 zerratar\\@gmail.com    ║");
                    logger.WriteLine("╚════════════════════════════════════════════╝");
                    logger.WriteLine("");
                    logger.WriteLine("  Following folder and its subfolders being watched");
                    logger.WriteLine($"    @whi@{input} @gray@");
                    logger.WriteLine("");
                    logger.WriteLine("  You may press @green@'q' @gray@at any time to quit.");
                    logger.WriteLine("");
                    logger.WriteLine(" ───────────────────────────────────────────────────── ");
                    logger.WriteLine("");

                    mediaWatcher.Start();
                    ConsoleKeyInfo ck;
                    while ((ck = Console.ReadKey(true)).Key != ConsoleKey.Q)
                    {
                        if (ck.Key == ConsoleKey.A)
                        {
                            mediaWatcher.SyncAll();
                        }
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HashSet<string> ParseList(string s)
        {
            return new HashSet<string>(s
                .Split(';')
                .Select(x => x.Trim())
                .Select(x => x.StartsWith("*") ? x.Substring(1) : x));
        }
    }
}
