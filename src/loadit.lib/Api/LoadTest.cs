using System.Threading;
using System.Threading.Tasks;
using Loadit.Engine;

namespace Loadit
{
    public abstract class LoadTest : ILoadTest
    {
        public virtual LoadOptions Options()
        {
            return new()
            {
                VUs = 1,
                Iterations = 1
            };
        }

        public virtual Task Setup(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public abstract Task Run(CancellationToken token);

        public virtual Task Teardown(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}