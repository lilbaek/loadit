using System;

namespace Loadit.Diagnostics
{
    public record EventTracking
    {
        public DateTimeOffset StartTime { get; init; }
    }
}