using System.Threading;
using System.Threading.Tasks;
using Loadit.Stats;

namespace Loadit.Tool.Collectors
{
    /// <summary>
    /// A stats collector is responsible for collecting metrics and either offloading or aggregating them.
    /// WARN: A stats collector is not allow to use HTTP or Sockets communication as it will impact the metrics provide by .NET!
    /// It can write to a file, use named pipes or display the output correctly. 
    /// </summary>
    public interface IStatsCollector
    {
        /// <summary>
        /// Name of the collector. Is used to determine collectors to use.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Used for setup of the stats collector. 
        /// </summary>
        public ValueTask Initialize(CancellationToken token);

        /// <summary>
        /// Called once during a test run. Should report stats on its own at regular intervals to the backend.
        /// When the CancellationToken is triggered it should report all remaining stats and terminate.
        /// </summary>
        public Task Run(CancellationToken token);

        /// <summary>
        /// Called repeatably during a test run. Collect the stats and report them in the run loop.
        /// The method only get delta stats. If you want to summarize all stats you will need to collect them in this method. 
        /// </summary>
        public void Collect(InterprocessSnapshot[] samples);
    }
}