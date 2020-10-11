using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using SubSyncLib.Logic;
using SubSyncLib.Providers;

[assembly: InternalsVisibleTo("SubSync.Tests")]
namespace SubSyncLib
{
    public class SubSyncSettings
    {
        [StartupArgument(0, "./")]
        public string Input { get; set; }

        [StartupArgument("lang", "english")]
        public HashSet<string> Languages { get; set; }

        [StartupArgument("vid", "*.avi;*.mp4;*.mkv;*.mpeg;*.flv;*.webm")]
        public HashSet<string> VideoExt { get; set; }

        [StartupArgument("sub", "*.srt;*.txt;*.sub;*.idx;*.ssa;*.ass")]
        public HashSet<string> SubtitleExt { get; set; }

        [StartupArgument("exit")]
        public bool ExitAfterSync { get; set; }

        // using this will force requests to be sequential rather than concurrent
        [StartupArgument("delay", "0")]
        public int MinimummDelayBetweenRequests { get; set; }

        [StartupArgument("resync")]
        public bool Resync { get; set; }

        [StartupArgument("resyncall")]
        public bool ResyncAll { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var settings = Arguments.Parse<SubSyncSettings>(args);

            //// ugly workaround for ending backslashes with single/double-quotes. Seem to be a bug in the dotnet!
            //if (!string.IsNullOrEmpty(settings.Input) && (settings.Input.EndsWith("\"") || settings.Input.EndsWith("'")))
            //{
            //    settings.Input = settings.Input.Substring(0, settings.Input.Length - 1);
            //}

            var subtitleExtensions = settings.SubtitleExt;
            var languages = settings.Languages;
            var input = settings.Input;

            var version = GetVersion();
            var logger = new ConsoleLogger();
            var videoIgnoreFilter = new VideoIgnoreFilter(ReadVideoIgnoreList());
            var videoSyncList = new VideoSyncList();
            using (var fallbackSubtitleProvider = new FallbackSubtitleProvider(
                videoSyncList,
                new OpenSubtitles(languages, new FileBasedCredentialsProvider("opensubtitles.auth", logger), logger)))
            //new Subscene(languages)))
            {
                var resultReporter = new QueueProcessReporter();
                var subSyncWorkerProvider = new WorkerProvider(logger, subtitleExtensions, fallbackSubtitleProvider, resultReporter);
                var subSyncWorkerQueue = new WorkerQueue(settings, subSyncWorkerProvider, resultReporter);

                using (var mediaWatcher = new SubtitleSynchronizer(
                    logger, videoSyncList, subSyncWorkerQueue, resultReporter, videoIgnoreFilter, settings))
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

                    if (settings.MinimummDelayBetweenRequests > 0)
                    {
                        logger.WriteLine($"  @yel@Request delay: @red@{settings.MinimummDelayBetweenRequests}ms @yel@activated.");
                        logger.WriteLine($"  @yel@All requests will be run in sequential order and may take a lot longer to sync.");
                        logger.WriteLine("");
                    }

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
        private static List<string> ReadVideoIgnoreList()
        {
            const string vidignore = ".vidignore";
            return System.IO.File.Exists(vidignore)
                ? ReadVidIgnoreList(System.IO.File.ReadAllLines(vidignore))
                : new List<string>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<string> ReadVidIgnoreList(string[] lines)
        {
            return lines.Where(x => !string.IsNullOrEmpty(x.Trim()) && !x.Trim().StartsWith("#")).ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
    }
}
