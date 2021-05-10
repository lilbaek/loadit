using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Lib.Test
{
    public class HttpTest : TestBaseHttp
    {
        private string _counterReset = "counter/reset";
        private string _counterResult = "counter/result";
        private string _counterIncrement = "counter";

        public HttpTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Should_Call_Server_100_Times()
        {
            uint optionsIterations = 100;

            await TestServerClient.GetAsync(_counterReset);
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 1;
                options.Iterations = optionsIterations;
            }, async (http, token) =>
            {
                using var res = await http.GetAsync(Endpoint + _counterIncrement, token);
            });
            var result = await TestServerClient.GetAsync(_counterResult);
            Assert.True(result.IsSuccessStatusCode);
            var resultCount = await result.Content.ReadAsStringAsync();
            Assert.Equal(optionsIterations.ToString(), resultCount);
        }

        [Fact]
        public async Task Should_Call_Server_100_Times_Multiple_Vus()
        {
            uint optionsIterations = 100;

            await TestServerClient.GetAsync(_counterReset);
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 5;
                options.Iterations = optionsIterations;
            }, async (http, token) =>
            {
                using var res = await http.GetAsync(Endpoint + _counterIncrement, token);
            });
            await Task.Delay(500);
            var result = await TestServerClient.GetAsync(_counterResult);
            Assert.True(result.IsSuccessStatusCode);
            var resultCount = await result.Content.ReadAsStringAsync();
            Assert.Equal(optionsIterations.ToString(), resultCount);
        }
    }
}