using System;
using System.Threading;
using System.Threading.Tasks;

namespace Loadit.Workers
{
    /// <summary>
    /// Interface defining a worker scheduler.
    /// The worker scheduler is responsible for scheduling the workers and run the load test
    /// </summary>
    public interface IWorkerScheduler
    {
        ExecutionState State();
        ValueTask Setup(LoadOptions options, CancellationToken token);
        Task Run(CancellationToken token);
    }

    public class ExecutionState
    {
        /// <summary>
        /// Options to use for this run
        /// </summary>
        public LoadOptions Options { get; init; } = new();
        
        /// <summary>
        /// Current status
        /// </summary>
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Created;

        /// <summary>
        /// Unix timestamp representing the time when the test was started in milliseconds
        /// </summary>
        public long StartTime { get; private set; }

        /// <summary>
        /// Unix timestamp representing when the test ends in milliseconds
        /// </summary>
        public long EndTime { get; private set; }

        private readonly string _locker = "THREAD_LOCKER";

        /// <summary>
        /// Sets the start time.
        /// </summary>
        /// <exception cref="InvalidOperationException">Can only be called once</exception>
        public void StartTest()
        {
            Monitor.Enter(_locker);
            try
            {
                if (StartTime != 0)
                {
                    throw new InvalidOperationException("It can only be called once");
                }

                StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            finally
            {
                Monitor.Exit(_locker);
            }
        }

        /// <summary>
        /// Returns the current run duration.
        /// Returns TimeSpan.Zero if it has not started yet.
        /// If the test is done it will return the duration of the test.
        /// </summary>
        /// <returns></returns>
        public TimeSpan CurrentRunDuration()
        {
            var startTime = StartTime;
            if (startTime == 0)
            {
                return TimeSpan.Zero;
            }
            var endTime = EndTime;
            if (endTime == 0)
            {
                endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            return DateTimeOffset.FromUnixTimeMilliseconds(endTime) - DateTimeOffset.FromUnixTimeMilliseconds(startTime);
        }
        
        /// <summary>
        /// Sets the end time.
        /// </summary>
        /// <exception cref="InvalidOperationException">Can only be called once</exception>
        public void EndTest()
        {
            Monitor.Enter(_locker);
            try
            {
                if (EndTime != 0)
                {
                    throw new InvalidOperationException("It can only be called once");
                }

                EndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            finally
            {
                Monitor.Exit(_locker);
            }
        }
    }

    public enum ExecutionStatus
    {
        Created = 0,
        Initializing = 1,
        Started = 2,
        Setup = 3,
        Running = 4,
        Teardown = 5,
        Done = 6
    }
}