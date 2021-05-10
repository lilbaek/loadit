using System.Threading.Tasks;
using loadit.shared.Commandline;
using loadit.shared.Ipc;
using loadit.shared.Result;
using Loadit.Stats;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Test
{
    public class PipeCommunicationTest : TestBase
    {
        private readonly ServerCommunicationTestClass _testService = new();

        public PipeCommunicationTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            ConfigureServices += services => { services.AddSingleton<IServerCommunication>(_testService); };
        }

        [Fact]
        public async Task Should_Get_Test_Data()
        {
            var testFile = GetTestFile("PipeCommunication.cs");
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Ok, execute);
            Assert.Equal(2, _testService.Data.Vus);
            Assert.Equal(2, _testService.Data.Iterations);
            Assert.Equal(2, _testService.Data.HttpRequests);
        }

        class ServerCommunicationTestClass : IServerCommunication
        {
            public SummarizedResult Data = null!;
            
            public void CollectStats(InterprocessSnapshot[] samples)
            {
                
            }

            public void WriteTestResult(SummarizedResult data)
            {
                Data = data;
            }

            public void LogInformation(string message)
            {
            }

            public void LogDebug(string message)
            {
            }

            public void LogError(string message)
            {
            }

            public void LogWarning(string message)
            {
                
            }

            public void LogCritical(string message)
            {
                
            }
        }
    }
}