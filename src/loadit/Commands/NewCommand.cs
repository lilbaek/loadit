using System;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using loadit.shared.Commandline;
using Spectre.Console;

namespace Loadit.Tool.Commands
{
    public class NewCommand : ICommandHandler
    {
        public async Task<int> InvokeAsync(InvocationContext context)
        {
            var directory = Directory.GetCurrentDirectory();
            var bindingContextParseResult = context.BindingContext.ParseResult;
            var name = bindingContextParseResult.ValueForOption<string>("--name");
            if (name == null)
            {
                AnsiConsole.Markup("[red]No project name specified[/]");
                return ExitCode.Error;
            }

            name = String.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            var rootDir = Path.Combine(directory, name);
            if (Directory.Exists(rootDir))
            {
                AnsiConsole.Markup($"[red]The folder {name} already exist. Please specify a new name.[/]");
                return ExitCode.Error;
            }

            Directory.CreateDirectory(rootDir);

            await File.WriteAllTextAsync(Path.Combine(rootDir, name + ".sln"), await GetReplacedTemplate(name, "sln_template"));

            var projectDir = Path.Combine(rootDir, name);
            Directory.CreateDirectory(projectDir);
            var propertiesDirectory = Path.Combine(projectDir, "Properties");
            Directory.CreateDirectory(propertiesDirectory);
            var libProjectDir = Path.Combine(rootDir, name + ".lib");
            Directory.CreateDirectory(libProjectDir);

            await File.WriteAllTextAsync(Path.Combine(projectDir, name + ".csproj"), await GetReplacedTemplate(name, "csproj_template"));
            await File.WriteAllTextAsync(Path.Combine(projectDir, name + ".csproj.user"), await GetReplacedTemplate(name, "csproj_user_template"));
            await File.WriteAllTextAsync(Path.Combine(libProjectDir, name + ".lib.csproj"), await GetReplacedTemplate(name, "lib_csproj_template"));

            await File.WriteAllTextAsync(Path.Combine(projectDir, "Test1.cs"), await GetReplacedTemplate(name, "test_template"));
            await File.WriteAllTextAsync(Path.Combine(projectDir, "Test2.cs"), await GetReplacedTemplate(name, "test2_template"));
            await File.WriteAllTextAsync(Path.Combine(projectDir, "Startup.cs"), await GetReplacedTemplate(name, "startup_template"));
            await File.WriteAllTextAsync(Path.Combine(projectDir, "appsettings.json"), await GetReplacedTemplate(name, "app_settings_template"));
            await File.WriteAllTextAsync(Path.Combine(propertiesDirectory, "launchSettings.json"), await GetReplacedTemplate(name, "launchSettings_template"));
            
            Console.WriteLine($"Created a new project with name: {name}");
            return ExitCode.Ok;
        }

        private async Task<string> GetReplacedTemplate(string projectName, string templateName)
        {
            return (await GetResource(templateName)).Replace("{{projectName}}", projectName).Replace("{{version}}", VersionCommand.AssemblyVersion.Value);
        }

        private async Task<string> GetResource(string name)
        {
            var assembly = Assembly.GetEntryAssembly();
            var resourceStream = assembly!.GetManifestResourceStream($"Loadit.Tool.Templates.{name}.txt");
            if (resourceStream == null)
            {
                throw new InvalidOperationException("Could not get resource with name: " + name);
            }

            using var reader = new StreamReader(resourceStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}