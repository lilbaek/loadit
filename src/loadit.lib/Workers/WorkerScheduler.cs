using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Collectors;
using Loadit.Engine;
using Loadit.Exceptions;
using Loadit.Stats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Loadit.Workers
{
    public class WorkerScheduler : IWorkerScheduler
    {
        private readonly IServiceProvider _provider;
        private readonly ILoadTest _test;
        private readonly ILogger<WorkerScheduler> _logger;
        private readonly ParseResult _parseResult;
        private ExecutionState _state = new();
        private IWorker[] _workers = new IWorker[0];
        private readonly CancellationTokenSource _statsProcessingTerminationToken = new();
        private readonly CancellationTokenSource _statsFeederTerminationToken = new();
        private IStatsCollector[] _statsCollectors = null!;
        private ISnapshotProvider[] _statsProviders = null!;

        public WorkerScheduler(IServiceProvider provider, ILoadTest test, ILogger<WorkerScheduler> logger, ParseResult parseResult)
        {
            _provider = provider;
            _test = test;
            _logger = logger;
            _parseResult = parseResult;
        }

        public ExecutionState State()
        {
            return _state;
        }

        public async ValueTask Setup(LoadOptions options, CancellationToken token)
        {
            _state.Status = ExecutionStatus.Initializing;
            _state = new ExecutionState
            {
                Options = options,
                Status = ExecutionStatus.Created
            };
            _workers = new[]
            {
                _provider.GetService<IWorker>()!
            };
            _statsCollectors = GetStatsCollectors();
            _statsProviders = _provider.GetServices<ISnapshotProvider>().ToArray();
            //Initialize stats collectors
            foreach (var collector in _statsCollectors)
            {
                await collector.Initialize(token);
            }
        }

        /// <summary>
        /// Returns the stats collectors that are enabled based on the parser result 
        /// </summary>
        /// <returns></returns>
        private IStatsCollector[] GetStatsCollectors()
        {
            //Filter them by what we want to use:
            var outOption = _parseResult.ValueForOption<string>("--out");
            if (string.IsNullOrEmpty(outOption))
            {
                //Only add console output
                _logger.LogDebug("Only using - ConsoleStatsCollector");
                return new IStatsCollector[] {_provider.GetService<ConsoleStatsCollector>()!};
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
            var statsCollectors = possible.Where(x => wantedOutput.Contains(x.Name.ToLowerInvariant())).ToList();
            statsCollectors.Add(_provider.GetService<ConsoleStatsCollector>()!);
            _logger.LogDebug("Using - " + string.Join(", ", statsCollectors.Select(x => x.Name)));
            return statsCollectors.ToArray();
        }

        /// <summary>
        /// Starts the testing. First setup, then telemetry and then the actual test
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Run(CancellationToken token)
        {
            _state.Status = ExecutionStatus.Started;
            try
            {
                _logger.LogDebug("Starting load test");
                _state.Status = ExecutionStatus.Setup;
                await HandleException(() => _test.Setup(token));
                await Task.Delay(500, token); //Wait for all telemetry to get reported
                _logger.LogDebug("Setup done");
                _state.Status = ExecutionStatus.Running;
                
                ClearTelemetry();
                var statsFeeder = SetupStatsFeeder();
                var collectorTasks = SetupStatsProcessing();
                
                _state.StartTest();
                await RunTests(token);
                _state.EndTest();
                
                await Task.Delay(500, token); //Wait for all telemetry to get reported
                _logger.LogDebug("Load test done");
                
                //Ask for the stats feeder to terminate and wait for it to be done.
                _statsFeederTerminationToken.Cancel();
                await statsFeeder;

                //Ask stats processing to terminate now that the feeder is done and wait for it to be done
                _statsProcessingTerminationToken.Cancel();
                await Task.WhenAll(collectorTasks);

                _state.Status = ExecutionStatus.Teardown;
                await HandleException(() => _test.Teardown(token));
                _logger.LogDebug("Teardown done");
            }
            catch (TaskCanceledException e)
            {
                _logger.LogDebug($"Error during RunAsync - TaskCanceledException: {e}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during RunAsync: {e}");
            }
            _state.Status = ExecutionStatus.Done;
            async Task HandleException(Func<Task> invoke)
            {
                try
                {
                    await invoke.Invoke();
                }
                catch (GeneratorCallException)
                {
                    //Ignore. Handled further down in the system.
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }
        }

        /// <summary>
        /// Discards any telemetry that has been collected prior to the test starting (Test setup).
        /// </summary>
        private void ClearTelemetry()
        {
            //Throw away any telemetry generated before starting the load test:
            foreach (var provider in _statsProviders)
            {
                provider.DeltaSnapshot();
            }
        }

        /// <summary>
        /// Setup background offload of stats pr. stats collector 
        /// </summary>
        /// <returns></returns>
        private Task[] SetupStatsProcessing()
        {
            var statsCollectors = _statsCollectors.ToList();
            var collectorTasks = new Task[statsCollectors.Count];
            for (var index = 0; index < statsCollectors.Count; index++)
            {
                collectorTasks[index] = statsCollectors[index].Run(_statsProcessingTerminationToken.Token);
            }

            return collectorTasks;
        }

        /*
         * The stats feeder task collects stats from the ILoadGenerators, processes it and sends it to the stats collectors
         */
        private Task SetupStatsFeeder()
        {
            return Task.Run(async () =>
            {
                while (!_statsFeederTerminationToken.IsCancellationRequested)
                {
                    ProcessSamples();
                    await Task.Delay(500);
                }
                //Process once more before terminating
                ProcessSamples();
            });
        }

        /// <summary>
        /// Gets samples from the stats providers and sends them to the stats collectors
        /// </summary>
        private void ProcessSamples()
        {
            foreach (var provider in _statsProviders)
            {
                var deltaSamples = provider.DeltaSnapshot();
                foreach (var deltaSample in deltaSamples)
                {
                    deltaSample.Metric.Collector.Add(deltaSample);
                }

                foreach (var statsCollector in _statsCollectors)
                {
                    statsCollector.Collect(deltaSamples);
                }
            }
        }
        
        private async Task RunTests(CancellationToken token)
        {
            //TODO: Make this more intelligent to handle different start times etc pr. worker
            var tasks = new Task[_workers.Length];
            for (var index = 0; index < _workers.Length; index++)
            {
                tasks[index] = _workers[index].Run(_state, token);
            }

            await Task.WhenAll(tasks);
        }
    }
}