using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using loadit.shared.Result;

namespace Loadit.Stats
{
    public class Summary : ICollectorChild
    {
        private List<double> Values { get; set; } = new();
        private bool _changed;
        private uint Count { get; set; }
        private double Min { get; set; }
        private double Max { get; set; }
        private double Sum { get; set; }
        private double Avg { get; set; }
        private double Median { get; set; }
        private double P95 { get; set; }
        private double P90 { get; set; }

        public void Add(Snapshot snapshot)
        {
            //TODO: Thread safety?
            Values.Add(snapshot.Value);
            _changed = true;
            Count += 1;
            Sum += snapshot.Value;
            Avg = Sum / Count;

            if (snapshot.Value > Max)
            {
                Max = snapshot.Value;
            }

            if (snapshot.Value < Min || Count == 1)
            {
                Min = snapshot.Value;
            }
        }

        private void Calc()
        {
            if (!_changed)
            {
                return;
            }

            Values.Sort();
            _changed = false;

            Median = Values.GetMedian();
            //StdDev = Values.GetStdDev();
            P95 = Values[(int) (Values.Count * 0.95)];
            P90 = Values[(int) (Values.Count * 0.90)];
            //P50 = Values[Values.Count / 2];
        }
        
        public IReadOnlyDictionary<string, double> Format(TimeSpan time)
        {
            Calc();
            return new ReadOnlyDictionary<string, double>(new Dictionary<string, double>
            {
                {"count", Count},
                {"min", Min},
                {"max", Max},
                {"avg", Avg},
                {"med", Median},
                {"p(90)", P90},
                {"p(95)", P95},
            });
        }
    }
}