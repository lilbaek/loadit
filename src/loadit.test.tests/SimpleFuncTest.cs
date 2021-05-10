using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit;

return await Execute.Run<HttpClient>(options =>
{
    options.VUs = 1;
    options.Iterations = 10;
}, async (http, token) =>
{
    using var res = await http.GetAsync("https://test.loadit.dev/", token);
});