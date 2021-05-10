using System.Collections.Generic;
using System.Reflection;

namespace Loadit.Stats
{
    /// <summary>
    /// Order has impact on what gets shown in the output :).
    ///
    /// The http duration/socket metrics are based on telemetry from .NET.
    /// It uses the events:
    /// System.Net.NameResolution.ResolutionStart
    /// System.Net.Sockets.ConnectStart
    /// System.Net.Security.HandshakeStart
    /// System.Net.Http.RequestHeadersStart
    /// System.Net.Http.RequestContentStart
    /// System.Net.Http.ResponseHeadersStart
    /// System.Net.Http.ResponseContentStart
    /// System.Net.Http.RequestStop
    ///
    /// To generate the durations.
    /// </summary>
    public class Metrics
    {
        /// <summary>
        /// DNS resolution time - NameResolution.ResolutionStart
        /// </summary>
        public static readonly Metric HttpNameResolution = new("http-name-resolution-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Time spent on doing socket connection - Sockets.ConnectStart
        /// </summary>
        public static readonly Metric HttpConnectingDuration = new("http-socket-connection-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// TLS handshake duration - Security.HandshakeStart
        /// </summary>
        public static readonly Metric HttpTlsHandshakeDuration = new("http-tls-handshake-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Time to send headers - Http.RequestHeadersStart
        /// </summary>
        public static readonly Metric HttpRequestHeadersDuration = new("http-request-headers-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Time to send content - Http.RequestContentStart
        /// </summary>
        public static readonly Metric HttpRequestContentDuration = new("http-request-content-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Time to receive headers - ResponseHeadersStart
        /// </summary>
        public static readonly Metric HttpResponseHeadersDuration = new("http-response-headers-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Time to receive content - ResponseContentStart
        /// </summary>
        public static readonly Metric HttpResponseContentDuration = new("http-response-content-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Time waiting for the remote host to return the first content (Headers).
        /// Difference between RequestHeadersStop/RequestContentStop and ResponseHeadersStart
        /// </summary>
        public static readonly Metric HttpRequestWaitingDuration = new("http-request-waiting-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Total duration of a request. This includes:
        /// System.Net.NameResolution.ResolutionStart
        /// System.Net.Sockets.ConnectStart
        /// System.Net.Security.HandshakeStart
        /// System.Net.Http.RequestHeadersStart
        /// System.Net.Http.RequestContentStart
        /// System.Net.Http.ResponseHeadersStart
        /// System.Net.Http.ResponseContentStart
        /// System.Net.Http.RequestStop
        /// </summary>
        public static readonly Metric HttpRequestDuration = new("http-request-duration", MetricType.Summary, ValueType.Time);

        /// <summary>
        /// Time of each "run" iteration
        /// </summary>
        public static readonly Metric IterationDuration = new("iteration-duration", MetricType.Summary, ValueType.Time);
        
        /// <summary>
        /// Number of outgoing requests 
        /// </summary>
        public static readonly Metric HttpRequests = new("http-requests-count", MetricType.Counter);
        
        /// <summary>
        /// Total data send during lifetime of process 
        /// </summary>
        public static readonly Metric DataSent = new("bytes-sent", MetricType.Gauge, ValueType.Data);
        
        /// <summary>
        /// Total data received during lifetime of process 
        /// </summary>
        public static readonly Metric DataReceived = new("bytes-received", MetricType.Gauge, ValueType.Data);
        
        /// <summary>
        /// Total number of http errors as reported by the event: RequestFailed
        /// </summary>
        public static readonly Metric HttpErrors = new("http-errors", MetricType.Counter);
        
        /// <summary>
        /// Number of Vus 
        /// </summary>
        public static readonly Metric Vus = new("vus", MetricType.Gauge);
        
        /// <summary>
        /// Total number of run iterations
        /// </summary>
        public static readonly Metric Iterations = new("iterations-count", MetricType.Counter);
        
        /// <summary>
        /// Returns all metrics in the same order they are defined in the class
        /// </summary>
        /// <returns></returns>
        public static List<Metric> AllMetrics()
        {
            var fields = typeof(Metrics).GetFields(BindingFlags.Public | BindingFlags.Static);
            var allMetrics = new List<Metric>();
            foreach (var field in fields)
            {
                var value = field.GetValue(null) as Metric;
                if (value != null)
                {
                    allMetrics.Add(value);
                }
            }
            return allMetrics;
        }
    }
}