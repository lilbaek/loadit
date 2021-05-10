using System;
using System.Net.Http;
using Loadit.Lib.Test.Setup;
using Xunit.Abstractions;

namespace Loadit.Lib.Test
{
    public class TestBaseHttp : TestBase
    {
        private HttpClient _testServerClient;

        protected TestBaseHttp(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        protected string Endpoint => "http://localhost:" + ServerFactory.PortNumber + "/";

        protected HttpClient TestServerClient
        {
            get
            {
                if (_testServerClient != null)
                {
                    return _testServerClient;
                }
                _testServerClient = new HttpClient {BaseAddress = new Uri(Endpoint)};
                return _testServerClient;
            }
        }
    }
}