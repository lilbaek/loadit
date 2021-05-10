using loadit.shared.Formatting;
using loadit.shared.Ipc;
using loadit.shared.Result;
using Loadit.Stats;
using Loadit.Tool.Runner;
using Microsoft.Extensions.Logging;

namespace Loadit.Tool.Server
{
    public class ServerCommunication : IServerCommunication
    {
        private readonly ILogger _logger;
        private readonly StatsHandler _statsHandler;

        public ServerCommunication(ILogger<ServerCommunication> logger, StatsHandler statsHandler)
        {
            _logger = logger;
            _statsHandler = statsHandler;
        }
        
        public void CollectStats(InterprocessSnapshot[] samples)
        {
            _statsHandler.ProcessStats(samples);
        }

        public void WriteTestResult(SummarizedResult data)
        {
            LoadTestResultToConsole.Report(data);
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation($"Test - {message}");
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug($"Test - {message}");
        }

        public void LogError(string message)
        {
            _logger.LogError($"Test - {message}");
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning($"Test - {message}");
        }

        public void LogCritical(string message)
        {
            _logger.LogCritical($"Test - {message}");
        }
    }
}