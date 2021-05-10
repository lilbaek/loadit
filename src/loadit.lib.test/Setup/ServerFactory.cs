using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Lib.Test.Setup;
using Loadit.Test.Server;
using Loadit.Test.Shared;
using Microsoft.AspNetCore.Hosting;
using Xunit;

[assembly: TestFramework("Loadit.Test.Shared.XunitTestFrameworkWithAssemblyFixture", "loadit.test.shared")]
[assembly: AssemblyFixture(typeof(ServerFactory))]

namespace Loadit.Lib.Test.Setup
{
    public class ServerFactory : IDisposable
    {
        public const int PortNumber = 44335;
        private IWebHost _webHost;

        public ServerFactory()
        {
            //Setup enviroment so that we can get access to the appconfig + launch the server for testing
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            StartEndpoint();
            Instances.CalledDisabled = true;
        }

        public void Dispose()
        {
            _webHost?.Dispose();
        }

        private void StartEndpoint()
        {
            var builder = Program.CreateHostBuilder(new string[0]);
            builder.UseKestrel(options => { options.Listen(IPAddress.Any, PortNumber); });
            var webHost = builder.Build();
            _webHost = webHost;
            Task.Factory.StartNew(() => { _webHost.Run(); });
            var client = new HttpClient {BaseAddress = new Uri("http://localhost:" + PortNumber)};
            //wait for it!
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    var unused = client.GetAsync("/simple").Result;
                    break;
                }
                catch
                {
                    //Ignore
                    Thread.Sleep(1000);
                }
            }
        }
    }
}