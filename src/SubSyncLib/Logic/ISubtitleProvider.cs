using System.Threading.Tasks;

namespace SubSyncLib.Logic
{
    public interface ISubtitleProvider
    {
        Task<string> GetAsync(string name, string outputDirectory);
    }
}