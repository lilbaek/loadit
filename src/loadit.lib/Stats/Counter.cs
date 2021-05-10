using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Loadit.Stats
{
    public class Counter : ICollectorChild
    {
        public double Value { get; set; }
        public DateTimeOffset? First { get; set; }

        public void Add(Snapshot snapshot)
        {
            //TODO: Thread safety? 
            Value += snapshot.Value;
            First ??= snapshot.Time;
        }

        public IReadOnlyDictionary<string, double> Format(TimeSpan time)
        {
            return new ReadOnlyDictionary<string, double>(new Dictionary<string, double>
            {
                {"count", Value},
                {"rate", Value / (time.TotalMilliseconds / 1000)}
            });
        }
    }
}