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

using System;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace loadit.analyzer.vs
{
    public class MonitorActiveDocument : Task
    {
        [Required]
        public string? FlagFile { get; set; }

        [Required]
        public string? LaunchProfiles { get; set; }

        [Required]
        public string? UserFile { get; set; }

        [Required]
        public string? ProjectDirectory { get; set; }

        public override bool Execute()
        {
            if (LaunchProfiles == null || UserFile == null || FlagFile == null || ProjectDirectory == null)
            {
                return true;
            }

            try
            {
                if (BuildEngine4.GetRegisteredTaskObject(nameof(ActiveDocumentMonitor), RegisteredTaskObjectLifetime.AppDomain) is not ActiveDocumentMonitor monitor)
                {
                    if (WindowsInterop.GetServiceProvider() is IServiceProvider services)
                    {
                        BuildEngine4.RegisterTaskObject(nameof(ActiveDocumentMonitor),
                            new ActiveDocumentMonitor(LaunchProfiles, UserFile, FlagFile, ProjectDirectory, services),
                            RegisteredTaskObjectLifetime.AppDomain, false);
                    }
                }
                else
                {
                    // NOTE: this means we only support ONE project/launchProfiles per IDE.
                    monitor.Refresh(LaunchProfiles, UserFile, FlagFile, ProjectDirectory);
                }
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to start active document monitoring: {e}");
            }

            return true;
        }
    }
}