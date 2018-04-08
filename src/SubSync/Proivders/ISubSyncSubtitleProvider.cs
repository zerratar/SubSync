using System.Threading.Tasks;

namespace SubSync.Proivders
{
    internal interface ISubSyncSubtitleProvider
    {
        Task<string> GetAsync(string name, string outputDirectory);
    }
}