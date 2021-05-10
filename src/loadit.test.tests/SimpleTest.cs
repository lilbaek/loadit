using System.Threading;
using System.Threading.Tasks;
using Loadit;

return await Execute.Run<SimpleTest>();

public class SimpleTest : LoadTest
{
    public override LoadOptions Options()
    {
        return new()
        {
            VUs = 1,
            Iterations = 1
        };
    }
    
    public override Task Run(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}