namespace SubSyncLib.Logic
{
    public interface IVideoSyncList
    {
        bool Contains(VideoFile video);
        void Add(VideoFile video);
        void Save();
    }
}