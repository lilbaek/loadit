using System;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Loadit.Tool.Commands;
using Spectre.Console;

namespace Loadit.Tool
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            AnsiConsole.MarkupLine("Loadit.dev - Modern load testing as Code - v" + VersionCommand.AssemblyVersion.Value);
            Console.WriteLine();
            if (args.Length == 0)
            {
                Console.WriteLine("To begin working with loadit, run the `loadit --new` command:");
                Console.WriteLine();
                Console.WriteLine("    $ loadit --new --name TestProject");
                Console.WriteLine();
                Console.WriteLine("This will create a new load testing project.");
                Console.WriteLine();
                Console.WriteLine("The most common commands from there are:");
                Console.WriteLine();
                Console.WriteLine("    - loadit --run --file PathToFile   : Run a load test on a single file");
                Console.WriteLine();
                Console.WriteLine("For more information, please visit the project page: https://www.loadit.dev/docs/");
                Console.WriteLine();
                args = new[] {"-h"};
            }
            return new Startup().CreateCommandLineBuilder(args).Build().InvokeAsync(args);
        }
    }
}