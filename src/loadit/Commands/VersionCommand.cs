using System;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading.Tasks;
using loadit.shared.Commandline;
using Spectre.Console;

namespace Loadit.Tool.Commands
{
    public class VersionCommand : ICommandHandler
    {
        internal static readonly Lazy<string> AssemblyVersion =
            new(() =>
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var assemblyVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                return assemblyVersionAttribute is null ? assembly.GetName().Version!.ToString() : assemblyVersionAttribute.InformationalVersion;
            });

        public Task<int> InvokeAsync(InvocationContext context)
        {
            Console.WriteLine(AssemblyVersion.Value);
            return Task.FromResult(ExitCode.Ok);
        }
    }
}