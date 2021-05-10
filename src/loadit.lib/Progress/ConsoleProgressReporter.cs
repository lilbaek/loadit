using System.Threading;
using System.Threading.Tasks;
using loadit.shared.Formatting;
using loadit.shared.Result;

namespace Loadit.Progress
{
    public class ConsoleProgressReporter : IProgressReporter
    {
        
        ValueTask IProgressReporter.Prepare(CancellationToken token)
        {
            return ValueTask.CompletedTask;
        }

        public void LogInformation(string message)
        {
            //Ignore - we have another logger doing work
        }

        public void LogDebug(string message)
        {
            //Ignore - we have another logger doing work
        }

        public void LogWarning(string message)
        {
            //Ignore - we have another logger doing work
        }

        public void LogCritical(string message)
        {
            //Ignore - we have another logger doing work
        }

        public void LogError(string message)
        {
            //Ignore - we have another logger doing work
        }


        ValueTask IProgressReporter.Report(SummarizedResult result)
        {
            LoadTestResultToConsole.Report(result);
            return ValueTask.CompletedTask;
        }

        ValueTask IProgressReporter.Shutdown()
        {
            return ValueTask.CompletedTask;
        }

        public Task WaitForOkToStart(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}