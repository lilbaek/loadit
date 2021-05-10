using System;
using System.Collections.Generic;
using MessagePack;

namespace loadit.shared.Result
{
    [MessagePackObject]
    public class SummarizedResult
    {
        [Key(0)]
        public double Vus { get; init; }
        
        [Key(1)]
        public TimeSpan Elapsed { get; init; }
        
        [Key(2)]
        public double HttpErrors { get; init; }
       
        [Key(3)]
        public int HttpRequests { get; init; }
        
        [Key(4)]
        public double HttpRequestsPrSecond { get; init; }
        
        [Key(5)]
        public int Iterations { get; init; }
        
        [Key(6)]
        public double IterationsPrSecond { get; init; }
        
        [Key(7)]
        public double BytesSentPrSecond { get; init; }
        
        [Key(8)]
        public double BytesReceivedPrSecond { get; init; }
       
        [Key(9)]
        public double BytesReceived { get; init; }
       
        [Key(10)]
        public double BytesSent { get; init; }
        
        [Key(11)]
        public double Bandwidth { get; init; }
       
        [Key(12)]
        public List<ResponseTimeResult> Timings { get; init; } = null!;
    }
}