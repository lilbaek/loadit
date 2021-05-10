using System;

namespace Loadit
{
    /// <summary>
    /// Options to use while doing the load test
    /// </summary>
    public class LoadOptions
    {
        /// <summary>
        /// The VUs to run concurrently.
        /// </summary>
        public uint VUs { get; set; }
        
        /// <summary>
        ///  Fixed number of iterations to execute the load test.
        /// The number of iterations is split between all VUs defined.
        ///
        /// An alternative is to define the duration instead. 
        /// </summary>
        public uint? Iterations { get; set; }
        
        /// <summary>
        /// Total duration of the load test. During this time each VU will be executed in a loop.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}