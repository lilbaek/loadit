using System;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Interprocess;
using loadit.shared.Ipc;
using Loadit.Tool.Runner;
using Microsoft.Extensions.Logging;

namespace Loadit.Tool.Server
{
    public class PipeServer : ITestConfiguration
    {
        private readonly IServerCommunication _serverCommunication;
        private readonly ILogger<PipeServer> _logger;
        private readonly PipeServer<IClientCommunication, IServerCommunication> _pipeServer;
        private readonly StatsHandler _statsHandler;
        private CancellationTokenSource _statsCancellationToken = new();
        private Task[]? _collectorTasks;

        public PipeServer(IServerCommunication serverCommunication, ILogger<PipeServer> logger, PipeServer<IClientCommunication, IServerCommunication> pipeServer, StatsHandler statsHandler)
        {
            _serverCommunication = serverCommunication;
            _logger = logger;
            _pipeServer = pipeServer;
            _statsHandler = statsHandler;
        }

        public async Task Start(string name, CancellationToken token)
        {
            try
            {
                _logger.LogDebug($"Starting pipe server: {name}");
                await _pipeServer.WaitForConnectionAsync(name, () => _serverCommunication, token);
                _logger.LogDebug($"Pipe server connected");
                await _statsHandler.Initialize(token);
                _collectorTasks = _statsHandler.SetupStatsProcessing(_statsCancellationToken.Token);
            }
            catch (ThreadAbortException)
            {
                //Ignore
            }
        }

        public async Task Stop()
        {
            //Ask stats processing to terminate now that the test is done and wait for it to be done
            _statsCancellationToken.Cancel();
            if (_collectorTasks != null)
            {
                await Task.WhenAll(_collectorTasks);
            }
            _pipeServer?.Dispose();
        }

        public Task<string> GetConfiguration(string key)
        {
            return _pipeServer.InvokeAsync(x => x.GetConfiguration(key));
        }

        public Task<string> GetConfigurationSection(string section, string key)
        {
            return _pipeServer.InvokeAsync(x => x.GetConfigurationSection(section, key));
        }

        public Task StartTesting()
        {
            return _pipeServer.InvokeAsync(x => x.StartTesting());
        }
    }
}