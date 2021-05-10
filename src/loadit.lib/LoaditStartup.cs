using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Loadit.Collectors;
using Loadit.Diagnostics;
using Loadit.Engine;
using Loadit.Interprocess;
using Loadit.Ipc;
using Loadit.Progress;
using loadit.shared.Ipc;
using loadit.shared.Logger;
using Loadit.Stats;
using Loadit.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loadit
{
    public class LoaditStartup
    {
        /// <summary>
        ///     Extensibility point 
        /// </summary>
        protected Action<IServiceCollection> ConfigureServices { get; set; } = _ => { };

        /// <summary>
        ///     Extensibility point 
        /// </summary>
        protected Action<IConfigurationBuilder> ConfigureAppConfiguration { get; set; } = _ => { };

        /// <summary>
        /// Register commandline commands
        /// </summary>
        /// <returns></returns>
        private Command RegisterCommandLineCommands()
        {
            var root = new RootCommand
            {
                new Option<int>("--vu", "Virtual user count"),
                new Option<string>("--out", "Output(s)"),
                new Option<string>("--pipe", "Named pipe identifier for IPC"),
                new Option<bool>("--debug", "Enable debug mode")
            };
            root.Handler = CommandHandler.Create<IHost, InvocationContext>((host, context) =>
            {
                var loadEngine = host.Services.GetService<ILoadEngine>()!;
                return loadEngine.RunAsync(context.GetCancellationToken());
            });
            root.Description = "Loadit.dev";
            return root;
        }

        /// <summary>
        /// Build and register services
        /// </summary>
        /// <param name="commandLineArgs"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        public virtual CommandLineBuilder CreateBuilder(string[] commandLineArgs, Action<IServiceCollection>? register = null)
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            return new CommandLineBuilder(
                    RegisterCommandLineCommands()
                )
                .UseHost(host =>
                {
                    host.UseConsoleLifetime()
                        .UseContentRoot(assemblyFolder)
                        .ConfigureAppConfiguration((_, config) =>
                        {
                            config.AddJsonFile(Path.Combine(assemblyFolder, "appsettings.json"), true);
                            config.AddEnvironmentVariables();
                            ConfigureAppConfiguration(config);
                        })
                        .ConfigureServices((_, services) =>
                        {
                            services.AddOptions();
                            services.AddSingleton<ILoadEngine, LoadEngine>();
                            if (commandLineArgs.Any(x => x == "--pipe")) //If we run with pipe we send data to pipe
                            {
                                services.AddSingleton(_ => new PipeClient<IServerCommunication, IClientCommunication>());
                                services.AddSingleton<IClientCommunication, ClientCommunication>();
                                services.AddSingleton<IProgressReporter, IpcProgressReporter>(provider => provider.GetService<IpcProgressReporter>()!);
                            }
                            else
                            {
                                services.AddSingleton<IProgressReporter, ConsoleProgressReporter>();
                            }

                            services.AddSingleton<SimpleWorker>();

                            services.AddSingleton<IWorker>(provider => provider.GetService<SimpleWorker>()!);
                            services.AddSingleton<IWorkerScheduler, WorkerScheduler>();

                            //Stats collectors
                            services.AddSingleton<IStatsCollector, ConsoleStatsCollector>(provider => provider.GetService<ConsoleStatsCollector>()!);
                            services.AddSingleton<IStatsCollector, IpcProgressReporter>(provider => provider.GetService<IpcProgressReporter>()!);
                            services.AddSingleton<ConsoleStatsCollector, ConsoleStatsCollector>();
                            services.AddSingleton<IpcProgressReporter, IpcProgressReporter>();

                            //Stats providers
                            services.AddSingleton<ISnapshotProvider>(provider => provider.GetService<SimpleWorker>()!);
                            services.AddSingleton<ISnapshotProvider>(provider => provider.GetService<TelemetryListener>()!);

                            services.AddSingleton<ConsoleProgressReporter>();
                            services.AddSingleton<TelemetryListener>();
                            services.AddSingleton<LoadEngine>();
                            services.AddSingleton(_ =>
                            {
                                var httpClient = new HttpClient(new SocketsHttpHandler()
                                {
                                    // Enable multiple HTTP/2 connections. - https://github.com/dotnet/runtime/issues/35088
                                    EnableMultipleHttp2Connections = true,
                                })
                                {
                                    Timeout = TimeSpan.FromSeconds(60),
                                };
                                return httpClient;
                            });
                            register?.Invoke(services);
                            ConfigureServices(services);
                            CallUserDefinedStartup(services);
                            services.AddLogging(x => { x.ProgressLogger(); });
                        })
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            logging.AddFilter("System", LogLevel.Warning);
                            logging.AddFilter("Microsoft", LogLevel.Warning);
                            var configurationSection = hostingContext.Configuration.GetSection("Logging");
                            logging.AddConfiguration(configurationSection);
                            if (commandLineArgs.Any(x => x == "--pipe")) //If we run with pipe we let the runner decide the log level + do all the logging
                            {
                                logging.SetMinimumLevel(LogLevel.Debug);
                            }
                            else
                            {
                                logging.SetMinimumLevel(LogLevel.Information);
                                logging.AddSpectreFormatter();
                                logging.AddConsole();
                            }
                        });
                });
        }

        /// <summary>
        /// Responsible for calling any user-defined startup declared in the running lib
        /// </summary>
        /// <param name="services"></param>
        private static void CallUserDefinedStartup(IServiceCollection services)
        {
            try
            {
                //Call user defined startup:
                var type = typeof(IStartup);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
                foreach (var t in types)
                {
                    IStartup instance = (IStartup) Activator.CreateInstance(t)!;
                    instance.ConfigureServices(services);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not call user-defined IStartup: {e}");
            }
        }
    }
}