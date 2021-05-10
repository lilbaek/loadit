using MessagePack;

namespace loadit.shared.Result
{
    [MessagePackObject()]
    public class ResponseTimeResult
    {
        [Key(0)]
        public string Name { get; set; } = null!;

        [Key(1)]
        public double Median { get; set; }

        [Key(2)]
        public double Avg { get; set; }

        [Key(3)]
        public double Min { get; set; }

        [Key(4)]
        public double Max { get; set; }

        [Key(5)]
        public double P95 { get; set; }

        [Key(6)]
        public double P90 { get; set; }

        [Key(7)]
        public int Count { get; set; }

        [Key(8)]
        public double PerSecond { get; set; }

        public ResponseTimeResult(string name, double median, double avg, double min, double max, double p95, double p90, int count, double perSecond)
        {
            Name = name;
            Median = median;
            Avg = avg;
            Min = min;
            Max = max;
            P95 = p95;
            P90 = p90;
            Count = count;
            PerSecond = perSecond;
        }

        // ReSharper disable once UnusedMember.Global
        public ResponseTimeResult()
        {
            //Serialization
        }
    }
}