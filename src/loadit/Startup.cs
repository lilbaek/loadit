using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Loadit.Interprocess;
using loadit.shared.Commandline;
using loadit.shared.Ipc;
using loadit.shared.Logger;
using Loadit.Tool.Collectors;
using Loadit.Tool.Commands;
using Loadit.Tool.Runner;
using Loadit.Tool.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loadit.Tool
{
    public class Startup
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
        /// Register all services in the DI container
        /// </summary>
        /// <param name="services"></param>
        private void RegisterInternalServices(IServiceCollection services)
        {
            services.AddTransient<RunCommand>();
            services.AddTransient<VersionCommand>();
            services.AddTransient<NewCommand>();
            services.AddTransient<PipeServer>();
            
            //Singletons
            services.AddSingleton<ITestConfiguration>(service => service.GetService<PipeServer>()!);
            services.AddSingleton<IServerCommunication, ServerCommunication>();
            services.AddSingleton<IRunEngine, RunEngine>();
            services.AddSingleton<IStatsCollector, InfluxdbStatsCollector>();
            services.AddSingleton<StatsHandler>();
            services.AddSingleton<PipeServer<IClientCommunication, IServerCommunication>, PipeServer<IClientCommunication, IServerCommunication>>();
        }

        /// <summary>
        /// Register commandline commands
        /// </summary>
        /// <returns></returns>
        private Command RegisterCommandLineCommands()
        {
            var root = new RootCommand();
            var run = new Command("--run", "Start a load test")
                .Configure(c =>
                {
                    c.AddAlias("-r");

                    var fileOption = new Option<FileInfo>("--file").ExistingOnly();
                    fileOption.AddAlias("-f");
                    c.AddOption(fileOption);

                    var @out = new Option<string>("--out");
                    @out.AddAlias("-o");
                    c.AddOption(@out);

                    var debugOption = new Option<bool>("--debug");
                    debugOption.AddAlias("-d");
                    c.AddOption(debugOption);
                })
                .HandledBy<RunCommand>();

            var version = new Command("--version", "Show version information")
                .Configure(c => { c.AddAlias("-v"); })
                .HandledBy<VersionCommand>();

            var newProject = new Command("--new", "Creates a new test project")
                .Configure(c =>
                {
                    var nameOption = new Option<string>("--name");
                    nameOption.AddAlias("-n");
                    nameOption.IsRequired = true;
                    c.AddOption(nameOption);
                })
                .HandledBy<NewCommand>();
            return root
                    .AddSubCommand(run)
                    .AddSubCommand(version)
                    .AddSubCommand(newProject)
                ;
        }

        /// <summary>
        /// Setup commandline system and integrate with DI
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual CommandLineBuilder CreateCommandLineBuilder(string[] args)
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
                            RegisterInternalServices(services);
                            ConfigureServices(services);
                        })
                        .ConfigureLogging((hostingContext, logging) =>
                        {
                            var configurationSection = hostingContext.Configuration.GetSection("Logging");
                            logging.AddFilter("System", LogLevel.Warning);
                            logging.AddFilter("Microsoft", LogLevel.Warning);
                            if (args.ToList().Any(x => x.Contains("--debug")))
                            {
                                logging.SetMinimumLevel(LogLevel.Trace);
                            }
                            else
                            {
                                logging.SetMinimumLevel(LogLevel.Information);
                                logging.AddConfiguration(configurationSection);
                            }

                            logging.AddSpectreFormatter();
                            logging.AddConsole();
                        });
                })
                .UseVersionOption()
                .UseHelp()
                .UseEnvironmentVariableDirective()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseExceptionHandler()
                .CancelOnProcessTermination();
        }
    }
}