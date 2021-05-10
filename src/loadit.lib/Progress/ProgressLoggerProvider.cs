using Microsoft.Extensions.Logging;

namespace Loadit.Progress
{
    public class ProgressLoggerProvider : ILoggerProvider
    {
        private IProgressReporter _progressLogger;

        public ProgressLoggerProvider(IProgressReporter progressLogger)
        {
            _progressLogger = progressLogger;
        }

        public void Dispose()
        {
            
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ProgressLogger(_progressLogger);
        }
    }
}