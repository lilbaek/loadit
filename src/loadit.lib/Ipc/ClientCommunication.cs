using loadit.shared.Ipc;
using Microsoft.Extensions.Configuration;

namespace Loadit.Ipc
{
    public class ClientCommunication : IClientCommunication
    {
        private readonly IConfiguration _configuration;
        private bool _canStart;

        public ClientCommunication(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetConfiguration(string key)
        {
            return _configuration.GetValue<string>(key);
        }
        
        public string GetConfigurationSection(string section, string key)
        {
            return _configuration.GetSection(section).GetValue<string>(key);
        }

        public void StartTesting()
        {
            _canStart = true;
        }

        public bool CanStart()
        {
            return _canStart;
        }
    }
}