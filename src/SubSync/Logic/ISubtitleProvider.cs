using System;
using System.Threading.Tasks;

namespace SubSync
{
    public interface ISubtitleProvider
    {
        Task<string> GetAsync(string name, string outputDirectory);
    }
}