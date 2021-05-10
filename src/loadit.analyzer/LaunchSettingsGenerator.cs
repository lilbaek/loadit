/*
MIT License

Copyright (c) 2020 Daniel Cazzulino

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Loadit.Analyzer
{
    [Generator]
    // ReSharper disable once UnusedType.Global
    public class LaunchSettingsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();
            var documents = new List<string>();
            foreach (var additional in context.AdditionalFiles)
            {
                context.AnalyzerConfigOptions.GetOptions(additional).TryGetValue("build_metadata.AdditionalFiles.SourceItemType", out var itemType);
                if (itemType == "Compile" && !additional.Path.Contains("obj") && !additional.Path.ToLower().Contains("startup.cs"))
                {
                    documents.Add(additional.Path);
                }
            }
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory))
            {
                return;
            }
            var profile = new LaunchProfile();
            foreach (var entry in documents.OrderBy(x => PathNetCore.GetRelativePath(projectDirectory, x)))
            {
                profile.Profiles.Add(PathNetCore.GetRelativePath(projectDirectory, entry), new Profile()
                {
                    CommandName = "Project"
                });
            }
            Directory.CreateDirectory(Path.Combine(projectDirectory, "Properties"));
            var filePath = Path.Combine(projectDirectory, "Properties", "launchSettings.json");
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            if (File.Exists(filePath) && File.ReadAllText(filePath) == json)
            {
                return;
            }

            File.WriteAllText(filePath, json);
        }
    }

    public class LaunchProfile
    {
        [JsonPropertyName("profiles")]
        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<string, Profile> Profiles { get; } = new();
    }

    public class Profile
    {
        [JsonPropertyName("commandName")]
        public string CommandName { get; set; } = default!;
    }
}