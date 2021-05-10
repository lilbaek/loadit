using System.Threading.Tasks;

namespace Loadit.Tool.Server
{
    public interface ITestConfiguration
    {
        Task<string> GetConfiguration(string key);
        Task<string> GetConfigurationSection(string section, string key);
    }
}