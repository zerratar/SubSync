using System;
using System.Collections.Generic;
using System.Text;

namespace SubSyncLib.Logic.VideoFormats
{
    public interface IVideoHeaderProvider
    {
        bool IsSupported(VideoFile file);
        IVideoHeader Get(VideoFile file);
    }

    public class VideoHeaderProvider : IVideoHeaderProvider
    {
        public bool IsSupported(VideoFile file)
        {
            // return Get(file) != null;
            switch (System.IO.Path.GetExtension(file.Name.ToLower()))
            {
                case ".mkv": return true;
                default: return false;
            }
        }

        public IVideoHeader Get(VideoFile file)
        {
            switch (System.IO.Path.GetExtension(file.Name.ToLower()))
            {
                case ".mkv": return new MatroskaVideoHeader(file);
                default: return null;
            }
        }
    }

    public class MatroskaVideoHeader : IVideoHeader
    {
        private readonly VideoFile file;

        public MatroskaVideoHeader(VideoFile file)
        {
            this.file = file;
            
        }

        public bool HasSubtitles { get; }

        public TimeSpan Length { get; }
    }
}
