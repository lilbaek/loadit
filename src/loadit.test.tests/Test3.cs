using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;


return await Execute.Run<Test3>();

public class Test3 : LoadTest
{
    private readonly HttpClient _http;

    public Test3(HttpClient http)
    {
        _http = http;
    }

    public override async Task Run(CancellationToken token)
    {
        using var res = await _http.GetAsync("http://localhost:5000/database", token);
    }
}