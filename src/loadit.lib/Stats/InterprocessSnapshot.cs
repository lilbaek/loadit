using System;
using MessagePack;

namespace Loadit.Stats
{
    [MessagePackObject()]
    public class InterprocessSnapshot
    {
        [Key(0)]
        public string Name { get; set; } = null!;

        [Key(1)]
        public DateTimeOffset Time { get; set; }

        [Key(2)]
        public double Value { get; set; }
    }
}