using System.Threading.Tasks;

namespace SubSync
{
    internal interface ISubtitleProvider
    {
        Task<string> GetAsync(string name, string outputDirectory);
    }
}