using System;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Diagnostics;
using Loadit.Engine;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

[assembly: InternalsVisibleTo("Loadit.Lib.Test")]
namespace Loadit
{
    public static class Instances
    {
        internal static bool Called;
        internal static bool CalledDisabled; //Testing
        public static readonly TelemetryListener TelemetryListener = new();
    }

    public static class Execute
    {
        /// <summary>
        /// Run a simple load test.
        /// </summary>
        /// <typeparam name="T">The load generator to use. Http is the only one for now.</typeparam>
        /// <exception cref="InvalidOperationException">Is thrown if you try to invoke it more than once</exception>
        public static Task<int> Run<T>(Func<T, CancellationToken, Task> executeAsync)
        {
            return Run(_ => { }, executeAsync);
        }

        /// <summary>
        /// Run a simple load test with additional options.
        /// </summary>
        /// <typeparam name="T">The load generator to use. Http is the only one for now.</typeparam>
        /// <exception cref="InvalidOperationException">Is thrown if you try to invoke it more than once</exception>
        public static Task<int> Run<T>(Action<LoadOptions> configure, Func<T, CancellationToken, Task> executeAsync)
        {
            return ExecuteFunc.Run(configure, executeAsync);
        }

        /// <summary>
        /// Run an advanced load test with more options.
        /// </summary>
        /// <typeparam name="T">The load test class to invoke.</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Is thrown if you try to invoke it more than once</exception>
        public static async Task<int> Run<T>() where T : LoadTest, ILoadTest
        {
            
            if (Instances.Called && !Instances.CalledDisabled)
            {
                throw new InvalidOperationException("Run can only be called once pr test class. Split your test into multiple test classes");
            }

            using (Instances.TelemetryListener)
            {
                Instances.Called = true;
                var startup = new LoaditStartup();
                var hostBuilder = startup.CreateBuilder(Environment.GetCommandLineArgs(), services => { services.AddSingleton<ILoadTest, T>(); });
                return await RunEngine(hostBuilder);
            }
        }

        internal static async Task<int> RunEngine(CommandLineBuilder hostBuilder)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 0)
            {
                AnsiConsole.MarkupLine("[bold red]Loadit.dev[/]");
            }
            return await hostBuilder.Build().InvokeAsync(args);
        }
    }
}