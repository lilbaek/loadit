using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Lib.Test
{
    public class IterationTestFunc : TestBase
    {
        public IterationTestFunc(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Should_Reach_100_Iterations()
        {
            uint callCounter = 0;
            uint optionsIterations = 100;
            
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 5;
                options.Iterations = optionsIterations;
            }, (_, _) =>
            {
                Interlocked.Increment(ref callCounter);
                return Task.CompletedTask;
            });
            Assert.Equal(optionsIterations, callCounter);
        }
        
        [Fact]
        public async Task Should_Reach_10000_Iterations()
        {
            uint callCounter = 0;
            uint optionsIterations = 10000;
            
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 10;
                options.Iterations = optionsIterations;
            }, (_, _) =>
            {
                Interlocked.Increment(ref callCounter);
                return Task.CompletedTask;
            });
            Assert.Equal(optionsIterations, callCounter);
        }

        [Fact]
        public async Task Should_Reach_10_Iterations()
        {
            uint callCounter = 0;
            uint optionsIterations = 10;
            
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 10;
                options.Iterations = optionsIterations;
            }, (_, _) =>
            {
                Interlocked.Increment(ref callCounter);
                return Task.CompletedTask;
            });
            Assert.Equal(optionsIterations, callCounter);
        }
        
        [Fact]
        public async Task Should_Reach_1_Iterations()
        {
            uint callCounter = 0;
            uint optionsIterations = 1;
            
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 10;
                options.Iterations = optionsIterations;
            }, (_, _) =>
            {
                Interlocked.Increment(ref callCounter);
                return Task.CompletedTask;
            });
            Assert.Equal(optionsIterations, callCounter);
        }
        
        [Fact]
        public async Task Should_Reach_1_Iterations_If_Vus_AreZero()
        {
            uint callCounter = 0;
            uint optionsIterations = 1;
            
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 0;
                options.Iterations = optionsIterations;
            }, (_, _) =>
            {
                Interlocked.Increment(ref callCounter);
                return Task.CompletedTask;
            });
            Assert.Equal(optionsIterations, callCounter);
        }
        
        [Fact]
        public async Task Should_Reach_0_Iterations_If_Iterations_AreZero()
        {
            uint callCounter = 0;
            uint optionsIterations = 0;
            
            await Loadit.Execute.Run<HttpClient>(options =>
            {
                options.VUs = 0;
                options.Iterations = optionsIterations;
            }, (_, _) =>
            {
                Interlocked.Increment(ref callCounter);
                return Task.CompletedTask;
            });
            Assert.Equal(optionsIterations, callCounter);
        }
    }
}