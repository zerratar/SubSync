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
                list = new HashSet<string>(System.IO.File.ReadAllLines(SyncListFile).Where(x => !string.IsNullOrEmpty(x)));
            }
        }

        public bool Contains(VideoFile video)
        {
            lock (mutex)
            {
                return list.Contains(video.Name);
            }
        }

        public void Add(VideoFile video)
        {
            lock (mutex)
            {
                list.Add(video.Name);
            }
        }

        public void Save()
        {
            lock (mutex)
            {
                var sb = new StringBuilder();
                foreach (var item in list) sb.AppendLine(item);
                System.IO.File.WriteAllText(SyncListFile, sb.ToString());
            }
        }
    }
}