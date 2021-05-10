using System.Threading.Tasks;

namespace Loadit.Interprocess.Models
{
    internal class PendingCall
    {
        public TaskCompletionSource<InterprocessResponse> TaskCompletionSource { get; } = new();
    }
}