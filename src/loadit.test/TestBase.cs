using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Loadit.Tool;
using Xunit.Abstractions;

namespace Loadit.Test
{
    public class TestBase : Startup
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ITestOutputHelper _testOutputHelper;
        protected TestBase(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }
        
        protected string GetTestFile(string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory(); 
            //_testOutputHelper.WriteLine("Current dir: " + currentDirectory);
            var directoryInfo = Directory.GetParent(currentDirectory)?.Parent?.Parent?.Parent;
            return Path.Combine(directoryInfo?.FullName!, "loadit.test.tests", fileName);
        }
        
        
        protected async Task<int> Execute(params string[] args)
        {
            int returnValue = await CreateCommandLineBuilder(args).Build().InvokeAsync(args);
            return returnValue;
        }
    }
}