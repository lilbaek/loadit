using System.Threading;
using System.Threading.Tasks;

namespace Loadit.Engine
{
    public interface ILoadTest
    {
        public LoadOptions Options() =>
            new()
            {
                VUs = 1,
                Iterations = 1
            };
        
        Task Setup(CancellationToken token);
        Task Run(CancellationToken token);
        Task Teardown(CancellationToken token);
    }
}