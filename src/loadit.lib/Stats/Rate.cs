using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Loadit.Stats
{
    public class Rate : ICollectorChild
    {
        public int Trues;
        public int Total;

        public void Add(Snapshot snapshot)
        {
            Interlocked.Increment(ref Total);
            if (snapshot.Value != 0)
            {
                Interlocked.Increment(ref Trues);
            }
        }

        public IReadOnlyDictionary<string, double> Format(TimeSpan time)
        {
            return new ReadOnlyDictionary<string, double>(new Dictionary<string, double>
            {
                {"rate", (double)Trues / Total },
            });
        }
    }
}