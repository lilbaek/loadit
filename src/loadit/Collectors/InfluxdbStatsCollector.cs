using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using Loadit.Stats;
using Loadit.Tool.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Loadit.Tool.Collectors
{
    /// <summary>
    /// InfluxDB stats collector
    /// </summary>
    public class InfluxdbStatsCollector : IStatsCollector
    {
        private readonly ITestConfiguration _configuration;
        private readonly ILogger<InfluxdbStatsCollector> _logger;
        private readonly ConcurrentQueue<InterprocessSnapshot> _send = new();
        private int _interval;
        private LineProtocolClient? _influxDbClient;
        public string Name => "InfluxDB";

        public InfluxdbStatsCollector(ITestConfiguration configuration, ILogger<InfluxdbStatsCollector> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async ValueTask Initialize(CancellationToken token)
        {
            var url = await _configuration.GetConfigurationSection("InfluxDB", "Url");
            var username = await _configuration.GetConfigurationSection("InfluxDB", "Username");
            var password = await _configuration.GetConfigurationSection("InfluxDB", "Password");
            var database = await _configuration.GetConfigurationSection("InfluxDB", "Database");
            _interval = int.TryParse(await  _configuration.GetConfigurationSection("InfluxDB", "Interval"), out var interval) ? interval : 1000;
            try
            {
                _influxDbClient = new LineProtocolClient(new Uri(url), database, username, password);
                _logger.LogDebug("InfluxDB: Initialize done");
            }
            catch (Exception e)
            {
                _logger.LogWarning("Could not connect to InfluxDB: " + e);
            }
        }

        public Task Run(CancellationToken token)
        {
            return Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        await SaveSnapshots();
                        try
                        {
                            await Task.Delay(_interval, token);
                        }
                        catch
                        {
                            //Ignore
                        }
                    }
                }
                finally
                {
                    await SaveSnapshots();
                }
            }, CancellationToken.None);
        }

        private async Task SaveSnapshots()
        {
            if (_influxDbClient == null || _send.IsEmpty)
            {
                return;
            }
            try
            {
                var lineProtocolPayload = new LineProtocolPayload();
                while (_send.TryDequeue(out var message))
                {
                    var readOnlyDictionary = new Dictionary<string, object> {{"value", message.Value}};
                    lineProtocolPayload.Add(new LineProtocolPoint(message.Name, readOnlyDictionary, null, message.Time.UtcDateTime));
                }

                await _influxDbClient.WriteAsync(lineProtocolPayload, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogWarning("InfluxDB: Could not commit data: " + e.Message);
            }
        }

        public void Collect(InterprocessSnapshot[] samples)
        {
            foreach (var sample in samples)
            {
                _send.Enqueue(sample);
            }
        }
    }
}