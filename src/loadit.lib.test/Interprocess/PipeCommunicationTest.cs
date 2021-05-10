using System;
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

namespace Loadit.Lib.Test.Interprocess
{
    public class PipeCommunicationTest : TestBase
    {
        public PipeCommunicationTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Should_Connect_To_PipeServer()
        {
            var pipeServer = new PipeServer<IClientCommunication, IServerCommunication>(new NullLoggerFactory());
            var pipeName = Guid.NewGuid().ToString();
            var mockedServer = new Mock<IServerCommunication>();
            mockedServer.Setup(x => x.WriteTestResult(It.IsAny<SummarizedResult>())).Callback((SummarizedResult s) =>
            {
                Assert.Equal(1, s.Vus);
                Assert.Equal(10, s.Iterations);
            });
            var task = Task.Run(async () => { await pipeServer.WaitForConnectionAsync(pipeName, () => mockedServer.Object, CancellationToken.None); });
            var taskRun = Execute<PipeTest>("--pipe", pipeName);
            await Task.WhenAll(task, taskRun);
            
            mockedServer.Verify(x => x.LogDebug(It.IsAny<string>()), Times.AtLeast(1));
            mockedServer.Verify(x => x.LogInformation(It.IsAny<string>()), Times.AtLeast(1));
            mockedServer.Verify(x => x.LogError(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Should_ReportSnapshots_To_PipeServer()
        {
            var pipeServer = new PipeServer<IClientCommunication, IServerCommunication>(new NullLoggerFactory());
            var pipeName = Guid.NewGuid().ToString();
            var mocked = new Mock<IServerCommunication>();
            mocked.Setup(x => x.WriteTestResult(It.IsAny<SummarizedResult>())).Callback((SummarizedResult s) =>
            {
                Assert.Equal(1, s.Vus);
                Assert.Equal(10, s.Iterations);
            });
            var task = Task.Run(async () => { await pipeServer.WaitForConnectionAsync(pipeName, () => mocked.Object, CancellationToken.None); });
            var taskRun = Execute<PipeTest>("--pipe", pipeName, "--out", "pipe");
            await Task.WhenAll(task, taskRun);
            mocked.Verify(x => x.CollectStats(It.IsAny<InterprocessSnapshot[]>()), Times.AtLeast(1));
        }
        
        [Fact]
        public async Task Should_Return_Configuration_Settings()
        {
            var pipeServer = new PipeServer<IClientCommunication, IServerCommunication>(new NullLoggerFactory());
            var pipeName = Guid.NewGuid().ToString();
            var mocked = new Mock<IServerCommunication>();
            var task = Task.Run(async () =>
            {
                await pipeServer.WaitForConnectionAsync(pipeName, () => mocked.Object, CancellationToken.None);
                Assert.Equal("TestValue", await pipeServer.InvokeAsync(x => x.GetConfiguration("TestKey")));
                Assert.Equal("loadit", await pipeServer.InvokeAsync(x => x.GetConfigurationSection("InfluxDB", "Database")));
            });
            var taskRun = Execute<PipeTest>("--pipe", pipeName, "--out", "pipe");
            await Task.WhenAll(task, taskRun);
        }
        
        private class PipeTest : LoadTest
        {
            private readonly HttpClient _httpClient;

            public PipeTest(HttpClient httpClient)
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
    }
}