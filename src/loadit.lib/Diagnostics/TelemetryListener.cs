using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using Loadit.Progress;
using Loadit.Stats;
using Microsoft.Extensions.Logging;

namespace Loadit.Diagnostics
{
    public class TelemetryListener : EventListener, ISnapshotProvider
    {
        private readonly ConcurrentStack<Snapshot> _samples = new();

        // Constant necessary for attaching ActivityId to the events.
        private const EventKeywords TasksFlowActivityIds = (EventKeywords) 0x80;

        private const string SystemNetHttpEventSourceName = "System.Net.Http";
        private readonly HashSet<string> _telemetry = new()
        {
            SystemNetHttpEventSourceName,
            "System.Net.Sockets",
            "System.Net.Security",
            "System.Net.NameResolution"
        };

        /// <summary>
        /// Used to track general start events like: NameResolution.ResolutionStart, Sockets.ConnectStart etc.
        /// Used for the tracking af duration between start and stop.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, EventTracking> _eventsInProgress = new();

        /// <summary>
        /// Used to track the time between us sending our last bits of content and us getting a first response (Header) from server.
        /// Uses: RequestHeadersStart, RequestContentStart to find start time.
        /// Users: ResponseHeadersStart to find end time.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, EventTracking> _timeBetweenRequestSendAndAnswer = new();
        
        /// <summary>
        /// Used to track url's and associate them with a specific activity id. This way we can attach additional information to the tracked requests
        /// </summary>
        private readonly ConcurrentDictionary<Guid, UrlTracking> _urlsInProgress = new();
        
        protected override void OnEventSourceCreated(EventSource? eventSource)
        {
            if (eventSource == null)
            {
                return;
            }

            // List of event source names provided by networking in .NET 5.
            if (_telemetry.Contains(eventSource.Name))
            {
                var interval = TimeSpan.FromSeconds(0.1).TotalSeconds.ToString(CultureInfo.InvariantCulture);
#pragma warning disable 8620
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All, new Dictionary<string, string>()
                {
                    // These additional arguments will turn on counters monitoring with a reporting interval set to a half of a second. 
                    ["EventCounterIntervalSec"] = interval
                });
#pragma warning restore 8620
            }
            // Turn on ActivityId.
            else if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
            {
                // Attach ActivityId to the events.
                EnableEvents(eventSource, EventLevel.LogAlways, TasksFlowActivityIds);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventName is null)
            {
                return;
            }

            if (_telemetry.All(x => x != eventData.EventSource.Name))
            {
                return;
            }
            
            if (eventData.EventId != -1)
            {
                ProcessEvents(eventData);
            }
            else
            {
                ProcessCounters(eventData);
            }
        }

        private void ProcessCounters(EventWrittenEventArgs eventData)
        {
            // It's a counter, parse the data properly.
            if (eventData.EventId == -1 && eventData.Payload != null)
            {
                foreach (var payload in eventData.Payload)
                {
                    if (payload is IDictionary<string, object> eventPayload)
                    {
                        if (!eventPayload.TryGetValue("Name", out var counterName))
                        {
                            return;
                        }

                        switch (counterName)
                        {
                            case "bytes-received":
                                if (GetMeanValue(eventPayload, out var bytesRe))
                                {
                                    AddSnapshot(bytesRe, eventData.TimeStamp, Metrics.DataReceived);
                                }

                                break;
                            case "bytes-sent":
                                if (GetMeanValue(eventPayload, out var bytesSe))
                                {
                                    AddSnapshot(bytesSe, eventData.TimeStamp, Metrics.DataSent);
                                }

                                break;
                        }
                    }
                }
            }
        }

        private void ProcessEvents(EventWrittenEventArgs eventData)
        {
            var eventName = eventData.EventName;
            Debug.Assert(eventName != null, "eventData.EventName != null");
            if (eventName.Contains("Start"))
            {
                TrackUrls(eventData, eventName);
                ProcessTimeBetweenTracking(eventData, eventName);
                _eventsInProgress.TryAdd(eventData.ActivityId, new EventTracking()
                {
                    StartTime = eventData.TimeStamp
                });
            }
            else if (eventName.Contains("Stop") || eventName.Contains("Failed"))
            {
                if (_eventsInProgress.TryGetValue(eventData.ActivityId, out var entry))
                {
                    var duration = (eventData.TimeStamp - entry.StartTime).TotalMilliseconds;
                    var endTime = eventData.TimeStamp;
                    //Add it to samples as it is now done.
                    switch (eventData.EventSource.Name)
                    {
                        case SystemNetHttpEventSourceName:
                            switch (eventName)
                            {
                                case "RequestStop":
                                case "RequestFailed":
                                    _samples.Push(new Snapshot(Metrics.HttpRequests, endTime, 1));
                                    _samples.Push(new Snapshot(Metrics.HttpRequestDuration, endTime, duration));
                                    if (eventName == "RequestFailed")
                                    {
                                        _samples.Push(new Snapshot(Metrics.HttpErrors, endTime, 1));
                                    }
                                    RemoveTrackedUrls(eventData.ActivityId);
                                    break;
                                case "RequestHeadersStop":
                                    _samples.Push(new Snapshot(Metrics.HttpRequestHeadersDuration, endTime, duration));
                                    break;
                                case "RequestContentStop":
                                    _samples.Push(new Snapshot(Metrics.HttpRequestContentDuration, endTime, duration));
                                    break;
                                case "ResponseHeadersStop":
                                    _samples.Push(new Snapshot(Metrics.HttpResponseHeadersDuration, endTime, duration));
                                    break;
                                case "ResponseContentStop":
                                    _samples.Push(new Snapshot(Metrics.HttpResponseContentDuration, endTime, duration));
                                    break;
                            }

                            break;
                        case "System.Net.Security":
                            switch (eventName)
                            {
                                case "HandshakeStop":
                                    _samples.Push(new Snapshot(Metrics.HttpTlsHandshakeDuration, endTime, duration));
                                    break;
                            }

                            break;
                        case "System.Net.Sockets":
                            switch (eventName)
                            {
                                case "ConnectStop":
                                    _samples.Push(new Snapshot(Metrics.HttpConnectingDuration, endTime, duration));
                                    break;
                            }

                            break;
                        case "System.Net.NameResolution":
                            switch (eventName)
                            {
                                case "ResolutionStop":
                                    _samples.Push(new Snapshot(Metrics.HttpNameResolution, endTime, duration));
                                    break;
                            }

                            break;
                    }

                    _eventsInProgress.TryRemove(eventData.ActivityId, out _);
                }
            }
        }

        private void TrackUrls(EventWrittenEventArgs eventData, string eventName)
        {
            //Top level request
            if (eventName == "RequestStart" && eventData.Payload != null)
            {
                var scheme = eventData.Payload[0];
                var host = eventData.Payload[1];
                var port = eventData.Payload[2];
                var pathAndQuery = eventData.Payload[3];
                var versionMajor = eventData.Payload[4];
                var versionMinor = eventData.Payload[5];
                var versionPolicy = eventData.Payload[6];
                var url = $"{scheme}//{host}:{port}{pathAndQuery}";
                var version = $"{versionMajor}.{versionMinor}.{versionPolicy}";
                _urlsInProgress.TryAdd(eventData.ActivityId, new UrlTracking(url, version));
            }
        }

        private void RemoveTrackedUrls(Guid eventDataActivityId)
        {
            _urlsInProgress.TryRemove(eventDataActivityId, out _);
        }

        private void ProcessTimeBetweenTracking(EventWrittenEventArgs eventData, string eventName)
        {
            switch (eventName)
            {
                case "ResponseContentStart":
                    //Check if we can pull out the time between
                    if (_timeBetweenRequestSendAndAnswer.TryGetValue(eventData.RelatedActivityId, out var entry))
                    {
                        var duration = (eventData.TimeStamp - entry.StartTime).TotalMilliseconds;
                        var endTime = eventData.TimeStamp;
                        _samples.Push(new Snapshot(Metrics.HttpRequestWaitingDuration, endTime, duration));
                        _timeBetweenRequestSendAndAnswer.TryRemove(eventData.RelatedActivityId, out _);
                    }
                    break;
                case "RequestHeadersStart":
                    //This is included as RequestContentStart only gets send if we actually send content.
                    //If we do a GET RequestContentStart is not sent so we use this timestamp instead.
                    _timeBetweenRequestSendAndAnswer.TryAdd(eventData.RelatedActivityId, new EventTracking()
                    {
                        StartTime = eventData.TimeStamp
                    });
                    break;
                case "RequestContentStart":
                    //If we get here we already added a header start. Remove it and add correct timestamp.
                    _timeBetweenRequestSendAndAnswer.TryRemove(eventData.RelatedActivityId, out _);
                    _timeBetweenRequestSendAndAnswer.TryAdd(eventData.RelatedActivityId, new EventTracking()
                    {
                        StartTime = eventData.TimeStamp
                    });
                    break;
            }
        }

        private void AddSnapshot(double value, DateTimeOffset time, Metric metric)
        {
            _samples.Push(new Snapshot(metric, time, value));
        }

        private bool GetMeanValue(IDictionary<string, object> dictionary, out double value)
        {
            value = 0;
            if (!(dictionary.TryGetValue("Mean", out var meanObj) && meanObj is double mean)
                || !(dictionary.TryGetValue("Count", out var countObj) && countObj is int count))
            {
                return false;
            }

            value = mean == 0 && count > 0 ? count : mean;
            value = double.IsNaN(value) ? 0 : value;
            return true;
        }

        Snapshot[] ISnapshotProvider.DeltaSnapshot()
        {
            if (_samples.IsEmpty)
            {
                return Array.Empty<Snapshot>();
            }

            var samples = new Snapshot[_samples.Count];
            _samples.TryPopRange(samples);
            return samples;
        }
    }

    public record UrlTracking(string Url, string HttpVersion);
}