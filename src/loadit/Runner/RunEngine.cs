using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Tool.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Loadit.Tool.Runner
{
    public interface IRunEngine
    {
        Task<bool> Execute(FileInfo file, List<string> additionalArgs, CancellationToken token);
    }

    public class RunEngine : IRunEngine
    {
        private readonly ILogger<RunEngine> _log;
        private readonly IServiceProvider _serviceProvider;

        public RunEngine(ILogger<RunEngine> log, IServiceProvider serviceProvider)
        {
            _log = log;
            _serviceProvider = serviceProvider;
        }

        public async Task<bool> Execute(FileInfo file, List<string> additionalArgs, CancellationToken token)
        {
            _log.LogDebug("Starting");
            var success = true;
            await AnsiConsole.Status()
                .StartAsync("Compiling...", async ctx =>
                {
                    try
                    {
                        var csprojDirectory = GetCsprojDirectory(file);
                        var fileToRun = Path.GetRelativePath(csprojDirectory.FullName, file.FullName);
                        var response = await RunDotnetProcess(file, token, new List<string>()
                        {
                            "build",
                            $"/p:ActiveDebugProfile={fileToRun}"
                        }, false);
                        if (response != 0)
                        {
                            _log.LogError("Could not compile your project. Terminating.");
                            success = false;
                            return;
                        }
                        success = await RunTestProject(file, additionalArgs, token, ctx);
                    }
                    catch (TaskCanceledException h)
                    {
                        _log.LogDebug(h, "Task canceled during run");
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Error during run");
                        success = false;
                    }
                });
            return success;
        }

        private async Task<bool> RunTestProject(FileInfo file, List<string> additionalArgs, CancellationToken token, StatusContext ctx)
        {
            var pipeEngine = _serviceProvider.GetService<PipeServer>()!;
            try
            {
                var pipeName = "loadit-" + Guid.NewGuid();
                var pipeTask = pipeEngine.Start(pipeName, token);
                ctx.Status = "Running...";
                var arguments = new List<string>()
                {
                    "run",
                    "--no-build",
                    "--",
                    "--pipe",
                    pipeName
                };
                arguments.AddRange(additionalArgs);
                var dotNetProcess = RunDotnetProcess(file, token, arguments, true);
                await pipeTask;
                await pipeEngine.StartTesting();
                await dotNetProcess;
                _log.LogDebug($"Process result {dotNetProcess.Result}");
                ctx.Status = "Closing...";
                if (dotNetProcess.Result != 0)
                {
                    _log.LogError("Could not run your test file. Terminating.");
                    return false;
                }
            }
            finally
            {
                await pipeEngine.Stop();
            }
            return true;
        }

        private async Task<int> RunDotnetProcess(FileInfo file, CancellationToken token, List<string> arguments, bool writeToConsole)
        {
            var dir = GetCsprojDirectory(file);
            var info = new ProcessStartInfo
            {
                FileName = "dotnet",
                CreateNoWindow = true,
                WorkingDirectory = dir?.FullName!,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            foreach (var arg in arguments)
            {
                info.ArgumentList.Add(arg);
            }

            info.Environment.Add("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

            var process = new Process
            {
                StartInfo = info
            };
            var dataLog = new StringBuilder();
            var errorLog = new StringBuilder();
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    dataLog.AppendLine(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    errorLog.AppendLine(args.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (TaskCanceledException)
            {
                //Make sure we terminate the process
                process.Kill(true);
                throw;
            }

            if (dataLog.Length > 0 && writeToConsole)
            {
                Console.WriteLine(dataLog.ToString());
            }

            if (errorLog.Length > 0)
            {
                throw new RunException(errorLog.ToString());
            }

            if (process.ExitCode != 0)
            {
                throw new RunException(dataLog.ToString());
            }

            return process.ExitCode;
        }

        private static DirectoryInfo GetCsprojDirectory(FileInfo file)
        {
            var dir = file.Directory;
            if (dir == null)
            {
                throw new InvalidOperationException("No directory for file: " + file.FullName);
            }

            if (!dir.GetFiles("*.csproj").Any())
            {
                //Search up until we find the parent with the csproj file in it
                dir = dir.Parent;
                while (dir != null && !dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Any())
                {
                    dir = dir.Parent;
                }
            }

            if (dir == null)
            {
                throw new InvalidOperationException("Could not find directory with csproj file. Make sure you have a valid project structure. File: " + file.FullName);
            }

            return dir;
        }
    }
}