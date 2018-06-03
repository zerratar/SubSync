namespace SubSyncLib.Logic
{
    public interface IVideoIgnoreFilter
    {
        bool Match(string filepath);
        bool Match(VideoFile video);
    }
}