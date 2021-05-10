using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Lib.Test
{
    public class IterationTest : TestBase
    {
        public IterationTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact]
        public async Task Should_Reach_10000_Iterations()
        {
            uint wantedIterations = 10000;
            IterationTestClass.Vus = 10;
            IterationTestClass.Iterations = wantedIterations;
            IterationTestClass.TotalCallCount = 0;
            await Execute<IterationTestClass>();
            Assert.Equal(wantedIterations, IterationTestClass.TotalCallCount);
        }

        [Fact]
        public async Task Should_Reach_10_Iterations()
        {
            uint wantedIterations = 10;
            IterationTestClass.Vus = 10;
            IterationTestClass.Iterations = wantedIterations;
            IterationTestClass.TotalCallCount = 0;
            await Execute<IterationTestClass>();
            Assert.Equal(wantedIterations, IterationTestClass.TotalCallCount);
        }
        
        [Fact]
        public async Task Should_Reach_1_Iterations()
        {
            uint wantedIterations = 1;
            IterationTestClass.Vus = 10;
            IterationTestClass.Iterations = wantedIterations;
            IterationTestClass.TotalCallCount = 0;
            await Execute<IterationTestClass>();
            Assert.Equal(wantedIterations, IterationTestClass.TotalCallCount);
        }
        
        [Fact]
        public async Task Should_Reach_1_Iterations_If_Vus_AreZero()
        {
            uint wantedIterations = 1;
            IterationTestClass.Vus = 0;
            IterationTestClass.Iterations = wantedIterations;
            IterationTestClass.TotalCallCount = 0;
            await Execute<IterationTestClass>();
            Assert.Equal(wantedIterations, IterationTestClass.TotalCallCount);
        }
        
        [Fact]
        public async Task Should_Reach_0_Iterations_If_Iterations_AreZero()
        {
            uint wantedIterations = 0;
            IterationTestClass.Vus = 10;
            IterationTestClass.Iterations = wantedIterations;
            IterationTestClass.TotalCallCount = 0;
            await Execute<IterationTestClass>();
            Assert.Equal(wantedIterations, IterationTestClass.TotalCallCount);
        }
        
        private class IterationTestClass : LoadTest
        {
            public static uint TotalCallCount = 0;
            public static uint Iterations = 0;
            public static uint Vus = 0;

            public override LoadOptions Options()
            {
                return new()
                {
                    VUs = Vus,
                    Iterations = Iterations
                };
            }

            public override Task Run(CancellationToken token)
            {
                Interlocked.Increment(ref TotalCallCount);
                return Task.CompletedTask;
            }
        }
    }
}