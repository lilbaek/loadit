using MessagePack;

namespace Loadit.Interprocess.Models
{
    [MessagePackObject]
    public class InterprocessResponse
    {
        [Key(0)]
        public long CallId { get; set; }

        [Key(1)]
        public bool Succeeded { get; set; }

        [Key(2)]
        public object Data { get; set; } = null!;

        [Key(3)]
        public string Error { get; set; } = null!;
    }
}