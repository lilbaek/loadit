using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;


return await Execute.Run<Test2>();

public class Test2 : LoadTest
{
    private readonly HttpClient _http;

    public Test2(HttpClient http)
    {
        _http = http;
    }

    public override async Task Run(CancellationToken token)
    {
        using var res = await _http.GetAsync("http://localhost:5000/limited", token);
    }
}