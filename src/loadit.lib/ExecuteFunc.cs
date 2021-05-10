using System;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Loadit
{
    internal static class ExecuteFunc
    {
        public static async Task<int> Run<T>(Action<LoadOptions> configure, Func<T, CancellationToken, Task> executeAsync)
        {
            var options = new LoadOptions()
            {
                VUs = 1
            };
            configure.Invoke(options);

            if (Instances.Called && !Instances.CalledDisabled)
            {
                throw new InvalidOperationException("Run can only be called once pr test class. Split your test into multiple test classes");
            }

            using (Instances.TelemetryListener)
            {
                Instances.Called = true;
                var startup = new LoaditStartup();
                var hostBuilder = startup.CreateBuilder(Environment.GetCommandLineArgs(), services => { services.AddSingleton(provider => ImplementationFactory(options, provider, executeAsync)); });
                return await Execute.RunEngine(hostBuilder);
            }
        }

        private static ILoadTest ImplementationFactory<T>(LoadOptions options, IServiceProvider arg, Func<T, CancellationToken, Task> executeAsync)
        {
            var service = arg.GetService<T>();
            if (service == null)
            {
                throw new Exception("Could not locate wanted service: " + typeof(T));
            }

            return new SimpleLoadTest<T>(options, service, executeAsync);
        }
    }
}