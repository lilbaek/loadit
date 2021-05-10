using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Stats;
using Loadit.Tool.Collectors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Loadit.Tool.Runner
{
    public class StatsHandler
    {
        private readonly IServiceProvider _provider;
        private readonly ILogger<StatsHandler> _logger;
        private readonly ParseResult _parser;
        private IStatsCollector[] _statsCollectors = null!;

        public StatsHandler(IServiceProvider provider, ILogger<StatsHandler> logger, ParseResult parser)
        {
            _provider = provider;
            _logger = logger;
            _parser = parser;
        }

        /// <summary>
        /// Initialize stats collectors
        /// </summary>
        public async Task Initialize(CancellationToken token)
        {
            _statsCollectors = GetStatsCollectors();
            foreach (var collector in _statsCollectors)
            {
                await collector.Initialize(token);
            }
        }

        /// <summary>
        /// Setup background offload of stats pr. stats collector 
        /// </summary>
        /// <returns></returns>
        public Task[] SetupStatsProcessing(CancellationToken token)
        {
            var statsCollectors = _statsCollectors.ToList();
            var collectorTasks = new Task[statsCollectors.Count];
            for (var index = 0; index < statsCollectors.Count; index++)
            {
                collectorTasks[index] = statsCollectors[index].Run(token);
            }
            return collectorTasks;
        }

        public void ProcessStats(InterprocessSnapshot[] snapshots)
        {
            foreach (var statsCollector in _statsCollectors)
            {
                statsCollector.Collect(snapshots);
            }
        }

        private IStatsCollector[] GetStatsCollectors()
        {
            //Filter them by what we want to use:
            var outOption = _parser.ValueForOption<string>("--out");
            if (string.IsNullOrEmpty(outOption))
            {
                return new IStatsCollector[0];
            }

            var possible = _provider.GetServices<IStatsCollector>().ToList();
            var wantedOutput = outOption.Split(";", StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLowerInvariant()).ToList();

            foreach (var output in wantedOutput)
            {
                if (possible.All(x => x.Name.ToLowerInvariant() != output))
                {
                    _logger.LogWarning($"Output with name: {output} was not found. Check the spelling of the wanted output.");
                }
            }

            return possible.Where(x => wantedOutput.Contains(x.Name.ToLowerInvariant())).ToArray();
        }
    }
}