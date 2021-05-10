using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Progress;
using loadit.shared.Result;
using Loadit.Stats;
using Loadit.Workers;
using Microsoft.Extensions.Logging;
using ValueType = Loadit.Stats.ValueType;

namespace Loadit.Collectors
{
    internal class ConsoleStatsCollector : IStatsCollector
    {
        private readonly IProgressReporter _progressReporter;
        private readonly IWorkerScheduler _workerScheduler;
        private readonly ILogger<ConsoleStatsCollector> _logger;
        private readonly ConcurrentStack<Snapshot> _samples = new();

        public ConsoleStatsCollector(IProgressReporter progressReporter, IWorkerScheduler workerScheduler, ILogger<ConsoleStatsCollector> logger)
        {
            _progressReporter = progressReporter;
            _workerScheduler = workerScheduler;
            _logger = logger;
        }

        public string Name => "Console";

        public ValueTask Initialize(CancellationToken token)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// The console stats collector does not care about the running progress.
        /// It just wants to summarize the result and push it all at once.
        /// Other collectors will be pushing the data on a regular basis
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task Run(CancellationToken token)
        {
            token.Register(() =>
            {
                try
                {
                    _logger.LogDebug(nameof(ConsoleStatsCollector) + " collecting run summary");
                    var currentRunDuration = _workerScheduler.State().CurrentRunDuration();
                    var errors = Metrics.HttpErrors.Collector.Format(currentRunDuration);
                    var iterations = Metrics.Iterations.Collector.Format(currentRunDuration);
                    var vus = Metrics.Vus.Collector.Format(currentRunDuration);
                    var dataReceived = Metrics.DataReceived.Collector.Format(currentRunDuration);
                    var dataSent = Metrics.DataSent.Collector.Format(currentRunDuration);
                    var httpRequests = Metrics.HttpRequests.Collector.Format(currentRunDuration);
                    var bytesSentPrSecond = dataSent["value"] / (currentRunDuration.TotalMilliseconds / 1000);
                    var allMetrics = Metrics.AllMetrics();
                    var timingMetrics = allMetrics.Where(x => x.Type == MetricType.Summary && x.ValueType == ValueType.Time);
                    var timings = new List<ResponseTimeResult>();
                    foreach (var metric in timingMetrics)
                    {
                        var values = metric.Collector.Format(currentRunDuration);
                        timings.Add(new ResponseTimeResult(
                            metric.Name,
                            values["med"],
                            values["avg"],
                            values["min"],
                            values["max"],
                            values["p(95)"],
                            values["p(90)"],
                            (int) values["count"],
                            values["count"] / (currentRunDuration.TotalMilliseconds / 1000)
                        ));
                    }
                    _progressReporter.Report(new SummarizedResult()
                    {
                        Vus = vus["value"],
                        Elapsed = currentRunDuration,
                        BytesReceived = dataReceived["value"],
                        BytesReceivedPrSecond = dataReceived["value"] / (currentRunDuration.TotalMilliseconds / 1000),
                        BytesSent = dataSent["value"],
                        BytesSentPrSecond = bytesSentPrSecond,
                        Bandwidth = Math.Round(bytesSentPrSecond * 8 / 1024 / 1024, MidpointRounding.AwayFromZero),
                        HttpErrors = errors["count"],
                        HttpRequests = (int) httpRequests["count"],
                        HttpRequestsPrSecond = httpRequests["rate"],
                        Iterations = (int) iterations["count"],
                        IterationsPrSecond = (int) iterations["rate"],
                        Timings = timings,
                    });
                    _logger.LogDebug(nameof(ConsoleStatsCollector) + " reported summarized result");
                }
                catch (Exception e)
                {
                    _logger.LogError($"{nameof(ConsoleStatsCollector)} {e}");
                }
            });
            return Task.CompletedTask;
        }
        
        public void Collect(Snapshot[] samples)
        {
            foreach (var sample in samples)
            {
                _samples.Push(sample);
            }   
        }
    }
}