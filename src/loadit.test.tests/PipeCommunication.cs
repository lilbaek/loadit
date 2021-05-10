using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;

return await Execute.Run<LoadTestService>();

public class LoadTestService : LoadTest
{
    private readonly HttpClient _http;

    public LoadTestService(HttpClient http)
    {
        _http = http;
    }
    
    public override LoadOptions Options()
    {
        return new()
        {
            VUs = 2,
            Iterations = 2
        };
    }

    public override async Task Run(CancellationToken token)
    {
        using var res = await _http.GetAsync("https://google.com", token);
    }
}