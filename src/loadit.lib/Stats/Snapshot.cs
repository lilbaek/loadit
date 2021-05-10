using System;

namespace Loadit.Stats
{
    public record Snapshot
    {
        public Metric Metric { get; } 
        public DateTimeOffset Time { get; }
        public double Value { get; }

        public Snapshot(Metric metric, DateTimeOffset time, double value)
        {
            Metric = metric;
            Time = time;
            Value = value;
        }
    }
}