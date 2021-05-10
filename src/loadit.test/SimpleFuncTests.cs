using System.Threading.Tasks;
using loadit.shared.Commandline;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Test
{
    public class SimpleFuncTests : TestBase
    {
        public SimpleFuncTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact]
        public async Task Should_Call_Test_In_RootDirectory()
        {
            var testFile = GetTestFile("SimpleFuncTest.cs");
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Ok, execute);
        }
    }
}