using System;
using Microsoft.Extensions.Logging;

namespace Loadit.Progress
{
    public class ProgressLogger : ILogger
    {
        private readonly IProgressReporter _progressProvider;

        public ProgressLogger(IProgressReporter progressProvider)
        {
            _progressProvider = progressProvider;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var reporter = _progressProvider;
            switch (logLevel)
            {
                case LogLevel.Trace:
                    break;
                case LogLevel.Debug:
                    reporter.LogDebug(formatter(state, exception));
                    break;
                case LogLevel.Information:
                    reporter.LogInformation(formatter(state, exception));
                    break;
                case LogLevel.Warning:
                    reporter.LogWarning(formatter(state, exception));
                    break;
                case LogLevel.Error:
                    reporter.LogError(formatter(state, exception));
                    break;
                case LogLevel.Critical:
                    reporter.LogCritical(formatter(state, exception));
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;
    }
}