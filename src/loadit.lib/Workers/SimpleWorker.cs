using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Engine;
using Loadit.Exceptions;
using Loadit.Stats;
using Microsoft.Extensions.Logging;

namespace Loadit.Workers
{
    /// <summary>
    /// The simplest worker that can either run durations or iterations with a number of VUs
    /// </summary>
    internal class SimpleWorker : IWorker, ISnapshotProvider
    {
        private readonly ILoadTest _loadTest;
        private readonly ILogger<SimpleWorker> _logger;
        private readonly ConcurrentStack<Snapshot> _samples = new();
        private int _currentVus;

        public SimpleWorker(ILoadTest loadTest, ILogger<SimpleWorker> logger)
        {
            _loadTest = loadTest;
            _logger = logger;
        }

        public Task Run(ExecutionState state, CancellationToken cancellationToken)
        {
            var options = state.Options;
            return Run(options.VUs, options.Duration, options.Iterations, cancellationToken);
        }

        private Task Run(uint vus, TimeSpan duration, uint? iterations, CancellationToken cancellationToken)
        {
            return QueueVus(vus, duration, iterations, cancellationToken);
        }

        private async Task QueueVus(uint vus, TimeSpan duration, uint? iterations, CancellationToken cancellationToken)
        {
            uint? iterationsPrVu = null;
            if (iterations.HasValue)
            {
                //Adjust Vus to match iterations
                if (vus > iterations)
                {
                    vus = iterations.Value;
                }

                var value = iterations.Value / vus;
                if (value <= 0)
                {
                    value = 1;
                }

                iterationsPrVu = Convert.ToUInt32(value);
            }

            _currentVus = (int) vus;
            var tasks = new Task[vus];
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < vus; i++)
            {
                var index = i;
                // Warning: Task.Run() with a lambda closure adds mem/cpu overhead
                // (+10% CPU, +20% memory on a short 10 second test). We do it without the lambda.
                if (iterationsPrVu.HasValue)
                {
                    tasks[i] = RunIterations(iterationsPrVu.Value, cancellationToken, index);
                }
                else
                {
                    tasks[i] = RunDuration(duration, sw, cancellationToken, index);
                }
            }

            await Task.WhenAll(tasks.ToArray());
        }

        private async Task RunDuration(TimeSpan duration, Stopwatch sw, CancellationToken cancellationToken, int workerIndex)
        {
            // Start using the threadpool immediately
            await Task.Yield();
            var stopwatch = new Stopwatch();
            while (!cancellationToken.IsCancellationRequested && duration.TotalMilliseconds > sw.Elapsed.TotalMilliseconds)
            {
                try
                {
                    stopwatch.Restart();
                    await _loadTest.Run(cancellationToken);
                }
                catch (GeneratorCallException)
                {
                    //Ignore. Already recorded on lower levels
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
                finally
                {
                    var iterationTime = (float) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                    _samples.Push(new Snapshot(Metrics.Iterations, DateTimeOffset.UtcNow, 1));
                    _samples.Push(new Snapshot(Metrics.IterationDuration, DateTimeOffset.UtcNow, iterationTime));
                }
            }
        }

        private async Task RunIterations(uint iterations, CancellationToken cancellationToken, int workerIndex)
        {
            // Start using the threadpool immediately
            await Task.Yield();
            var stopwatch = new Stopwatch();
            for (var i = 0; i < iterations && !cancellationToken.IsCancellationRequested; i++)
            {
                try
                {
                    stopwatch.Restart();
                    await _loadTest.Run(cancellationToken);
                }
                catch (GeneratorCallException)
                {
                    //Ignore. Already recorded on lower levels
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
                finally
                {
                    var iterationTime = (float) stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000;
                    _samples.Push(new Snapshot(Metrics.Iterations, DateTimeOffset.UtcNow, 1));
                    _samples.Push(new Snapshot(Metrics.IterationDuration, DateTimeOffset.UtcNow, iterationTime));
                }
            }
        }

        Snapshot[] ISnapshotProvider.DeltaSnapshot()
        {
            //Always provide updates vu count
            _samples.Push(new Snapshot(Metrics.Vus, DateTimeOffset.UtcNow, _currentVus));
            var samples = new Snapshot[_samples.Count];
            _samples.TryPopRange(samples);
            return samples;
        }
    }
}