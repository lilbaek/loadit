using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Loadit.Engine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Loadit.Lib.Test
{
    public class TestBase : LoaditStartup
    {
        protected readonly ITestOutputHelper _testOutputHelper;
        protected Dictionary<string, string> Configuration { get; set; } = new();
        protected TestConsole Console { get; } = new();
        protected string ConsoleStdOut => Console.Out.ToString();
        protected string ConsoleErrorOut => Console.Error.ToString();
        
        protected TestBase(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        public override CommandLineBuilder CreateBuilder(string[] args, Action<IServiceCollection> register = null)
        {
            var cliBuilder = base.CreateBuilder(args, register);
            ConfigureServices += services =>
            {
                services.AddSingleton<IConsole>(Console);
            };
            ConfigureAppConfiguration += builder =>
            {
                if (Configuration != null && Configuration.Any())
                {
                    builder.AddInMemoryCollection(Configuration);
                }
            };
            return cliBuilder.ConfigureConsole(_ => Console);
        }
        
        protected async Task<int> Execute<T>(params string[] args) where T : LoadTest, ILoadTest
        {
            using (Instances.TelemetryListener)
            {
                int returnValue = await CreateBuilder(args, services => { services.AddSingleton<ILoadTest, T>(); }).Build().InvokeAsync(args);
                if (!string.IsNullOrEmpty(ConsoleStdOut))
                {
                    _testOutputHelper.WriteLine("-----CLI STD OUT-----");
                    _testOutputHelper.WriteLine($"{ConsoleStdOut}");
                }

                if (!string.IsNullOrEmpty(ConsoleErrorOut))
                {
                    _testOutputHelper.WriteLine("-----CLI ERROR-----");
                    _testOutputHelper.WriteLine($"{ConsoleErrorOut}");
                }

                return returnValue;
            }
        }
    }
}