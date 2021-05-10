using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;

return await Execute.Run<Test4>();

public class Test4 : LoadTest
{
    private readonly HttpClient _http;

    public Test4(HttpClient http)
    {
        _http = http;
    }

    public override async Task Run(CancellationToken token)
    {
        using var res = await _http.GetAsync("http://localhost:5000/WeatherForecast", token);
    }
}