using System.Threading;
using System.Threading.Tasks;
using Loadit.Engine;
using loadit.shared.Result;

namespace Loadit.Workers
{
    /// <summary>
    /// Interface defining a worker that will run the load test
    /// </summary>
    public interface IWorker
    {
        Task Run(ExecutionState state, CancellationToken cancellationToken);
    }
}