using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using loadit.shared.Commandline;
using Loadit.Tool.Runner;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace Loadit.Tool.Commands
{
    public class RunCommand : ICommandHandler
    {
        private readonly IRunEngine _runEngine;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public RunCommand(IRunEngine runEngine, IHostApplicationLifetime hostApplicationLifetime)
        {
            _runEngine = runEngine;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var result = context.BindingContext.ParseResult;
            var fileInfo = result.ValueForOption<FileInfo>("--file");
            if (fileInfo == null)
            {
                AnsiConsole.Markup("[red]Could not find the specified file[/]");
                return ExitCode.Error;
            }

            var debugEnabled = result.ValueForOption<bool>("--debug");
            try
            {
                var additionalArgs = new List<string>();
                if (debugEnabled)
                {
                    additionalArgs.Add("--debug");
                }

                //Always report stats back to main process
                additionalArgs.Add("--out");
                additionalArgs.Add("pipe");
             
                var resultOfRun = await _runEngine.Execute(fileInfo, additionalArgs, _hostApplicationLifetime.ApplicationStopping);
                if (resultOfRun)
                {
                    return ExitCode.Ok;
                }

                return ExitCode.Error;
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e);
                return ExitCode.Error;
            }
        }
    }
}