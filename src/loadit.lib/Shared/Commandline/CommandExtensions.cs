using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace loadit.shared.Commandline
{
    public static class CommandExtensions
    {
        public static Command HandledBy<T>(this Command command) where T : ICommandHandler
        {
            command.Handler = CommandHandler.Create<IHost, InvocationContext>((host, invocation) =>
            {
                if (invocation.HasErrors())
                {
                    invocation.PrintErrors();
                    return Task.FromResult(ExitCode.Error);
                }

                return host.Services.GetRequiredService<T>().InvokeAsync(invocation);
            });
            return command;
        }

        public static Command AddSubCommand(this Command command, Command subCommand)
        {
            command.AddCommand(subCommand);
            return command;
        }

        public static Command Configure(this Command command, Action<Command> configure)
        {
            configure(command);
            return command;
        }

        private static void PrintErrors(this InvocationContext context)
        {
            if (!context.HasErrors())
            {
                return;
            }

            foreach (ParseError error in context.ParseResult.Errors)
            {
                AnsiConsole.WriteLine(error.Message);
            }
        }

        private static bool HasErrors(this InvocationContext context)
        {
            return context.ParseResult.Errors.Any();
        }
    }
}