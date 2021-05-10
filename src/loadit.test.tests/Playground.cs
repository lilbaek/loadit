using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;

return await Execute.Run<HttpClient>(options =>
{
    options.VUs = 1;
    options.Duration = TimeSpan.FromMinutes(5);
}, async (http, token) =>
{
    using var res = await http.GetAsync("https://test.loadit.dev", token);
    /*
    using var rese = await http.PostAsync("https://test.loadit.dev", new StringContent(""), token);
    using var res2 = await http.PatchAsync("https://test.loadit.dev", new StringContent(""), token);
    using var res3 = await http.DeleteAsync("https://test.loadit.dev", token);*/
});