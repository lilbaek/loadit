using System;
using System.Collections.Generic;
using System.Linq;

namespace loadit.shared.Result
{
    internal static class DoubleExtensions
    {
        public static double GetMedian(this List<double> source)
        {
            var count = source.Count;

            if (count == 0)
            {
                return 0;
            }

            if (count % 2 != 0)
            {
                return source[count / 2];
            }

            var a = source[count / 2 - 1];
            var b = source[count / 2];
            return (a + b) / 2;
        }

        public static double GetStdDev(this List<double> source)
        {
            if (source.Count <= 0)
            {
                return 0;
            }

            var avg = source.Average();
            var sum = source.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt(sum / (source.Count - 1));
        }
    }
}
