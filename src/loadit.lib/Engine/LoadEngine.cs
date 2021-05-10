using System;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Helpers;
using Loadit.Progress;
using loadit.shared.Commandline;
using Loadit.Workers;
using Microsoft.Extensions.Logging;

namespace Loadit.Engine
{
    internal interface ILoadEngine
    {
        Task<int> RunAsync(CancellationToken token);
    }

    internal class LoadEngine : ILoadEngine
    {
        private readonly ILoadTest _test;
        private readonly IProgressReporter _progressProvider;
        private readonly ILogger<LoadEngine> _logger;
        private readonly IWorkerScheduler _workerScheduler;

        public LoadEngine(ILoadTest test,
            IProgressReporter progressProvider,
            ILogger<LoadEngine> logger,
            IWorkerScheduler workerScheduler)
        {
            _test = test;
            _progressProvider = progressProvider;
            _logger = logger;
            _workerScheduler = workerScheduler;
        }

        public async Task<int> RunAsync(CancellationToken token)
        {
            try
            {
                await _progressProvider.Prepare(token);
                await _progressProvider.WaitForOkToStart(token);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not get progressReporter instance: {e}");
                return ExitCode.Error;
            }
            _logger.LogDebug("Starting run.");
            try
            {
                //TODO: Combine options from the test with options from commandline etc!
                var options = _test.Options();
                if (options.VUs <= 0)
                {
                    options.VUs = 1;
                }

                if (!options.Iterations.HasValue && options.Duration == TimeSpan.Zero)
                {
                    options.Iterations = 1;
                }

                _logger.LogInformation(OptionsTranslator.TranslateOptions(options));
                _logger.LogDebug("Setup - Worker scheduler");
                await _workerScheduler.Setup(options, token);
                _logger.LogDebug("Run - Worker scheduler");
                await _workerScheduler.Run(token);
                _logger.LogDebug("Load engine done. Returning ok.");
                return ExitCode.Ok;
            }
            catch (TaskCanceledException e)
            {
                _logger.LogDebug($"Error during RunAsync - TaskCanceledException: {e}");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during RunAsync: {e}");
            }
            finally
            {
                await _progressProvider.Shutdown();
            }
            _logger.LogDebug("Load engine done. Returning error. See previous messages.");
            return ExitCode.Error;
        }
    }
}