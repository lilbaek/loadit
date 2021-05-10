using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Loadit.Stats
{
    public class Gauge : ICollectorChild
    {
        public double Value  { get; set; }
        public double? Min  { get; set; }
        public double Max  { get; set; }

        public void Add(Snapshot snapshot)
        {
            //TODO: Thread safety?
            Value = snapshot.Value;
            if (snapshot.Value > Max)
            {
                Max = snapshot.Value;
            }

            if (!Min.HasValue || snapshot.Value < Min)
            {
                Min = snapshot.Value;
            }
        }

        public IReadOnlyDictionary<string, double> Format(TimeSpan time)
        {
            return new ReadOnlyDictionary<string, double>(new Dictionary<string, double>
            {
                {"value", Value},
                {"min", Min.GetValueOrDefault()},
                {"max", Max}
            });
        }
    }
}