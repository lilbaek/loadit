using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Collectors;
using Loadit.Interprocess;
using Loadit.Progress;
using loadit.shared.Ipc;
using loadit.shared.Result;
using Loadit.Stats;
using Microsoft.Extensions.Logging;

namespace Loadit.Ipc
{
    public class IpcProgressReporter : IProgressReporter, IStatsCollector
    {
        private readonly ParseResult _parseResult;

        private readonly IClientCommunication _clientCommunication;
        private readonly PipeClient<IServerCommunication, IClientCommunication> _pipeClient;
        private readonly ConcurrentQueue<LogEntry> _entries = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ConcurrentQueue<Snapshot> _stats = new();
        public string Name => "pipe";

        public IpcProgressReporter(ParseResult parseResult, PipeClient<IServerCommunication, IClientCommunication> pipeClient, IClientCommunication clientCommunication)
        {
            _parseResult = parseResult;
            _pipeClient = pipeClient;
            _clientCommunication = clientCommunication;
        }

        async ValueTask IProgressReporter.Report(SummarizedResult result)
        {
            LogDebug("Writing report");
            await _pipeClient.InvokeAsync(x => x.WriteTestResult(result));
        }

        async ValueTask IProgressReporter.Prepare(CancellationToken token)
        {
            var pipeName = _parseResult.ValueForOption<string>("--pipe");
            if (string.IsNullOrEmpty(pipeName))
            {
                throw new InvalidOperationException("IpcProgressReporter cannot be used without the option --pipe being specified");
            }
            await _pipeClient.ConnectAsync(pipeName, () => _clientCommunication, token);
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    await WriteLogEntries();
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
            }, CancellationToken.None);
        }

        private async Task WriteLogEntries()
        {
            while (_entries.TryDequeue(out var message))
            {
                if (message == null)
                {
                    continue;
                }

                switch (message.Level)
                {
                    case LogLevel.Trace:
                        await _pipeClient.InvokeAsync(x => x.LogDebug(message.Message));
                        break;
                    case LogLevel.Debug:
                        await _pipeClient.InvokeAsync(x => x.LogDebug(message.Message));
                        break;
                    case LogLevel.Information:
                        await _pipeClient.InvokeAsync(x => x.LogInformation(message.Message));
                        break;
                    case LogLevel.Warning:
                        await _pipeClient.InvokeAsync(x => x.LogWarning(message.Message));
                        break;
                    case LogLevel.Error:
                        await _pipeClient.InvokeAsync(x => x.LogError(message.Message));
                        break;
                    case LogLevel.Critical:
                        await _pipeClient.InvokeAsync(x => x.LogCritical(message.Message));
                        break;
                }
            }
        }

        public void LogInformation(string message)
        {
            _entries.Enqueue(new LogEntry(LogLevel.Information, message));
        }

        public void LogDebug(string message)
        {
            _entries.Enqueue(new LogEntry(LogLevel.Debug, message));
        }

        public void LogWarning(string message)
        {
            _entries.Enqueue(new LogEntry(LogLevel.Warning, message));
        }

        public void LogCritical(string message)
        {
            _entries.Enqueue(new LogEntry(LogLevel.Critical, message));
        }

        public void LogError(string message)
        {
            _entries.Enqueue(new LogEntry(LogLevel.Error, message));
        }

        async ValueTask IProgressReporter.Shutdown()
        {
            await WriteLogEntries();
            _pipeClient?.Dispose();
        }

        public async Task WaitForOkToStart(CancellationToken token)
        {
            for (int i = 0; i < 10; i++)
            {
                if (_clientCommunication.CanStart())
                {
                    break;
                }
                await Task.Delay(100, token);
            }
            if (_clientCommunication.CanStart())
            {
                LogDebug("Got start message from server");
            }
            else
            {
                LogWarning("Timeout waiting for start message from server. Continue now.");
            }
        }

        private record LogEntry(LogLevel Level, string Message);

        public ValueTask Initialize(CancellationToken token)
        {
            return ValueTask.CompletedTask;
        }

        public Task Run(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await ForwardEvents();
                        try
                        {
                            await Task.Delay(100, token);
                        }
                        catch
                        {
                            //Ignore
                        }
                    }
                }
                finally
                {
                    await ForwardEvents();
                }
            }, CancellationToken.None);
        }

        private async Task ForwardEvents()
        {
            //TODO: optimize me
            var send = new List<InterprocessSnapshot>();
            while (_stats.TryDequeue(out var message))
            {
                send.Add(new InterprocessSnapshot()
                {
                    Name = message.Metric.Name,
                    Time = message.Time,
                    Value = message.Value
                });
            }

            try
            {
                await _pipeClient.InvokeAsync(x => x.CollectStats(send.ToArray()), CancellationToken.None);
            }
            catch (Exception e)
            {
                LogInformation("Error sending IPC data: " + e);
            }
        }

        public void Collect(Snapshot[] samples)
        {
            foreach (var sample in samples)
            {
                _stats.Enqueue(sample);
            }
        }
    }
}