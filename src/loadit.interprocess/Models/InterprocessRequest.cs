using System;
using MessagePack;

namespace Loadit.Interprocess.Models
{
    [MessagePackObject]
    public class InterprocessRequest
    {
        [Key(0)]
        public long CallId { get; set; }

        [Key(1)]
        public string MethodName { get; set; } = null!;

        [Key(2)]
        public object[] Parameters { get; set; } = null!;

        [Key(3)]
        public Type[] GenericArguments { get; set; } = null!;
    }
}