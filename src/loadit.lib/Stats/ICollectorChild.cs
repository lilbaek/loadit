using System;
using System.Collections.Generic;

namespace Loadit.Stats
{
    public interface ICollectorChild
    {
        /// <summary>
        /// Adds a snapshot to the collector
        /// </summary>
        /// <param name="snapshot"></param>
        void Add(Snapshot snapshot); 

        /// <summary>
        /// Get data in a fixed format we can work with
        /// </summary>
        IReadOnlyDictionary<string, double> Format(TimeSpan time);
    }
}