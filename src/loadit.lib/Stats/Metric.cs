using System;

namespace Loadit.Stats
{
    public record Metric
    {
        public string Name { get; }
        public MetricType Type { get; }
        public ValueType ValueType { get; }
        public ICollectorChild Collector { get; }
        
        public Metric(string name, MetricType type, ValueType valueType = ValueType.Default)
        {
            Name = name;
            Type = type;
            ValueType = valueType;
            switch (type)
            {
                case MetricType.Counter:
                    Collector = new Counter();
                    break;
                case MetricType.Gauge:
                    Collector = new Gauge();
                    break;
                case MetricType.Summary:
                    Collector = new Summary();
                    break;
                case MetricType.Rate:
                    Collector = new Rate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
    
    public enum MetricType
    {
        Counter = 1, // Counters only increase in value and reset to zero when the process restarts.
        Summary = 2, // A summary, min/max/avg/med/epsilon are interesting 
        Gauge = 3, // Gauges can have any numeric value and change arbitrarily.
        Rate = 4 // A rate, displays % of values that aren't 0
    }

    public enum ValueType
    {
        Default = 1,
        Time = 2, 
        Data = 3, 
    }
}