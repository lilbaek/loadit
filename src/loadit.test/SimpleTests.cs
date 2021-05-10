using System.IO;
using System.Threading.Tasks;
using loadit.shared.Commandline;
using Loadit.Tool;
using Xunit;
using Xunit.Abstractions;

namespace Loadit.Test
{
    public class SimpleTests : TestBase
    {
        public SimpleTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }
        
        [Fact]
        public async Task Should_Fail_If_File_DoesNotExist()
        {
            var testFile = GetTestFile("DoesNotExist.cs");
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Error, execute);
        }
                
        
        [Fact]
        public async Task Should_Fail_If_Csproj_DoesNotExist()
        {
            var directoryInfo = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent;
            var testFile = Path.Combine(directoryInfo?.FullName!, "dummy_for_tests.cs");
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Error, execute);
        }
        
        [Fact]
        public async Task Should_Call_Test_In_RootDirectory()
        {
            var testFile = GetTestFile("SimpleTest.cs");
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Ok, execute);
        }
        
        [Fact]
        public async Task Should_Call_Test_In_SubDirectory()
        {
            var testFile = GetTestFile(Path.Combine("Grouped", "SimpleTest1.cs"));
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Ok, execute);
        }
        
        [Fact]
        public async Task Should_Call_Test_In_SubSubDirectory()
        {
            var testFile = GetTestFile(Path.Combine("Grouped", "GroupedAgain", "SimpleTest2.cs"));
            var execute = await Execute("-r", "-f", testFile);
            Assert.Equal(ExitCode.Ok, execute);
        }
    }
}