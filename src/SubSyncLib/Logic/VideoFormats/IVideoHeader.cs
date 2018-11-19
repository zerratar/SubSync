using System;

namespace SubSyncLib.Logic.VideoFormats
{
    public interface IVideoHeader
    {
        bool HasSubtitles { get; }
        TimeSpan Length { get; }
    }
}