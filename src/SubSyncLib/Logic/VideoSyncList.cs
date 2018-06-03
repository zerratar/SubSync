using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubSyncLib.Logic
{
    public class VideoSyncList : IVideoSyncList
    {
        private const string SyncListFile = ".sync-cache";
        private readonly HashSet<string> list = new HashSet<string>();
        private readonly object mutex = new object();

        public VideoSyncList()
        {
            if (System.IO.File.Exists(SyncListFile))
            {
                this.list = new HashSet<string>(System.IO.File.ReadAllLines(SyncListFile).Where(x => !string.IsNullOrEmpty(x)));
            }
        }

        public bool Contains(VideoFile video)
        {
            lock (mutex)
            {
                return this.list.Contains(video.HashString);
            }
        }

        public void Add(VideoFile video)
        {
            lock (mutex)
            {
                this.list.Add(video.HashString);
            }
        }

        public void Save()
        {
            lock (mutex)
            {
                var sb = new StringBuilder();
                foreach (var item in this.list) sb.AppendLine(item);
                System.IO.File.WriteAllText(SyncListFile, sb.ToString());
            }
        }
    }
}