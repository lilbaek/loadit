using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Loadit.Interprocess;
using loadit.shared.Ipc;
using loadit.shared.Result;
using Loadit.Stats;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Lib.Test
{
    public class StatsTest : TestBaseHttp
    {
        public StatsTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact]
        public async Task Should_Record_Server_Calls()
        {
            var pipeServer = new PipeServer<IClientCommunication, IServerCommunication>(new NullLoggerFactory());
            var pipeName = Guid.NewGuid().ToString();
            var mocked = new Mock<IServerCommunication>();
            mocked.Setup(x => x.WriteTestResult(It.IsAny<SummarizedResult>())).Callback((SummarizedResult s) =>
            {
                Assert.Equal(1, s.Vus);
                Assert.Equal(10, s.Iterations);
                Assert.Equal(10, s.HttpRequests);
            });
            var snapshots = new List<InterprocessSnapshot>();
            mocked.Setup(x => x.CollectStats(It.IsAny<InterprocessSnapshot[]>())).Callback((InterprocessSnapshot[] s) =>
            {
                snapshots.AddRange(s);
            });
            var task = Task.Run(async () => { await pipeServer.WaitForConnectionAsync(pipeName, () => mocked.Object, CancellationToken.None); });
            var taskRun = Execute<StatsLoadTest>("--pipe", pipeName, "--out", "pipe");
            await Task.WhenAll(task, taskRun);
            
            Assert.Contains(snapshots, x => x.Name == "http-socket-connection-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-headers-duration");
            Assert.Contains(snapshots, x => x.Name == "http-response-headers-duration");
            Assert.Contains(snapshots, x => x.Name == "http-response-content-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-waiting-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-duration");
            Assert.Contains(snapshots, x => x.Name == "iteration-duration");
            Assert.Contains(snapshots, x => x.Name == "http-requests-count");
            Assert.Contains(snapshots, x => x.Name == "bytes-sent");
            Assert.Contains(snapshots, x => x.Name == "bytes-received");
        }
        
                [Fact]
        public async Task Should_Record_Server_Calls_Post()
        {
            var pipeServer = new PipeServer<IClientCommunication, IServerCommunication>(new NullLoggerFactory());
            var pipeName = Guid.NewGuid().ToString();
            var mocked = new Mock<IServerCommunication>();
            mocked.Setup(x => x.WriteTestResult(It.IsAny<SummarizedResult>())).Callback((SummarizedResult s) =>
            {
                Assert.Equal(1, s.Vus);
                Assert.Equal(10, s.Iterations);
                Assert.Equal(10, s.HttpRequests);
            });
            var snapshots = new List<InterprocessSnapshot>();
            mocked.Setup(x => x.CollectStats(It.IsAny<InterprocessSnapshot[]>())).Callback((InterprocessSnapshot[] s) =>
            {
                snapshots.AddRange(s);
            });
            var task = Task.Run(async () => { await pipeServer.WaitForConnectionAsync(pipeName, () => mocked.Object, CancellationToken.None); });
            var taskRun = Execute<StatsLoadTestPost>("--pipe", pipeName, "--out", "pipe");
            await Task.WhenAll(task, taskRun);
            
            Assert.Contains(snapshots, x => x.Name == "http-socket-connection-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-headers-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-content-duration");
            Assert.Contains(snapshots, x => x.Name == "http-response-headers-duration");
            Assert.Contains(snapshots, x => x.Name == "http-response-content-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-waiting-duration");
            Assert.Contains(snapshots, x => x.Name == "http-request-duration");
            Assert.Contains(snapshots, x => x.Name == "iteration-duration");
            Assert.Contains(snapshots, x => x.Name == "http-requests-count");
            Assert.Contains(snapshots, x => x.Name == "bytes-sent");
            Assert.Contains(snapshots, x => x.Name == "bytes-received");
        }
        
        private class StatsLoadTest : LoadTest
        {
            private readonly HttpClient _httpClient;

            public StatsLoadTest(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public override LoadOptions Options()
            {
                return new()
                {
                    VUs = 1,
                    Iterations = 10
                };
            }

            public override async Task Run(CancellationToken token)
            {
                using var res = await _httpClient.GetAsync("http://localhost:44335/simple", token);
            }
        }
        
        private class StatsLoadTestPost : LoadTest
        {
            private readonly HttpClient _httpClient;

            public StatsLoadTestPost(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public override LoadOptions Options()
            {
                return new()
                {
                    VUs = 1,
                    Iterations = 10
                };
            }

            public override async Task Run(CancellationToken token)
            {
                using var res = await _httpClient.PostAsync("http://localhost:44335/simple", new StringContent(""), token);
            }
        }
    }
}