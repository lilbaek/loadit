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

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Loadit.VisualStudio.Build
{
    public class OpenStartupFile : Task
    {
        [Required]
        public string? FlagFile { get; set; }
        
        [Required]
        public string? ProjectDirectory { get; set; }

        public string? StartupFile { get; set; }

        public override bool Execute()
        {
            if (FlagFile == null || StartupFile == null)
            {
                return true;
            }
            if (!File.Exists(FlagFile) || File.ReadAllText(FlagFile) != StartupFile)
            {
                // This defers the opening until the build completes.
                BuildEngine4.RegisterTaskObject(
                    StartupFile,
                    new DisposableAction(() => WindowsInterop.EnsureOpened(StartupFile)),
                    RegisteredTaskObjectLifetime.Build, false);

                File.WriteAllText(FlagFile, StartupFile);
            }

            return true;
        }
    }
}