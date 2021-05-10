using System.Threading;
using System.Threading.Tasks;
using loadit.shared.Result;

namespace Loadit.Progress
{
    /// <summary>
    /// Interface for reporting progress to the user/system.
    /// We can only have one progress reporter at a time.
    /// If you need to report stats use the IStatsCollector interface instead.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Prepare the progress reporter (Called before anything is done)
        /// </summary>
        internal ValueTask Prepare(CancellationToken token);
        void LogInformation(string message);
        void LogDebug(string message);
        void LogWarning(string message);
        void LogCritical(string message);
        void LogError(string message);
        /// <summary>
        /// Report a summarized result to the user/system
        /// </summary>
        internal ValueTask Report(SummarizedResult result);
        
        /// <summary>
        /// Called just before the process terminates.
        /// Should perform any shutdown steps required
        /// </summary>
        internal  ValueTask Shutdown();

        /// <summary>
        /// Ask if we can start testing. If we are run by the CLI we need to wait with the test run until the CLI is ready
        /// </summary>
        Task WaitForOkToStart(CancellationToken token);
    }
}