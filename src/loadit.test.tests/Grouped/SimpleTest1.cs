using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;

return await Execute.Run<SimpleTest1>();

public class SimpleTest1 : LoadTest
{
    public override LoadOptions Options()
    {
        return new()
        {
            VUs = 1,
            Iterations = 1
        };
    }
    
    public override async Task Run(CancellationToken token)
    {

    }
}