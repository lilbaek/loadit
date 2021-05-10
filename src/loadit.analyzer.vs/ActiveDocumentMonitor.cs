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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json.Linq;

namespace loadit.analyzer.vs
{
    internal class ActiveDocumentMonitor : MarshalByRefObject, IDisposable, IVsRunningDocTableEvents, IVsSelectionEvents
    {
        private readonly IVsRunningDocumentTable? rdt;
        private readonly uint rdtCookie;
        private readonly IVsMonitorSelection? selection;
        private readonly uint selectionCookie;
        private string flagFile;
        private string projectDirectory;

        private string launchProfilesPath;
        private Dictionary<string, string> startupFiles = new();
        private string userFile;
        private readonly FileSystemWatcher watcher;

        public ActiveDocumentMonitor(string launchProfilesPath, string userFile, string flagFile, string projectDirectory, IServiceProvider services)
        {
            this.launchProfilesPath = launchProfilesPath;
            this.userFile = userFile;
            this.flagFile = flagFile;
            this.projectDirectory = projectDirectory;

            watcher = new FileSystemWatcher(Path.GetDirectoryName(launchProfilesPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "launchSettings.json"
            };

            watcher.Changed += (_, _) => ReloadProfiles();
            watcher.Created += (_, _) => ReloadProfiles();
            watcher.EnableRaisingEvents = true;
            ReloadProfiles();

            rdt = (IVsRunningDocumentTable) services.GetService(typeof(SVsRunningDocumentTable));
            if (rdt != null)
            {
                rdt.AdviseRunningDocTableEvents(this, out rdtCookie);
            }

            selection = (IVsMonitorSelection) services.GetService(typeof(SVsShellMonitorSelection));
            if (selection != null)
            {
                selection.AdviseSelectionEvents(this, out selectionCookie);
            }
        }

        void IDisposable.Dispose()
        {
            if (rdtCookie != 0 && rdt != null)
            {
                rdt.UnadviseRunningDocTableEvents(rdtCookie);
            }

            if (selectionCookie != 0 && selection != null)
            {
                selection.UnadviseSelectionEvents(selectionCookie);
            }

            watcher.Dispose();
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            // The MSBuild targets should have created it in target SelectStartupFile.
            if (!File.Exists(userFile))
            {
                return 0;
            }

            if ((grfAttribs & (uint) __VSRDTATTRIB.RDTA_DocDataReloaded) != 0 ||
                (grfAttribs & (uint) __VSRDTATTRIB.RDTA_MkDocument) != 0)
            {
                UpdateStartupFile(((IVsRunningDocumentTable4) rdt!).GetDocumentMoniker(docCookie));
            }

            return 0;
        }

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return 0;
        }

        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return 0;
        }

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            return 0;
        }

        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return 0;
        }

        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return 0;
        }

        int IVsSelectionEvents.OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            // No-op on multi-selection.
            if (pMISNew == null &&
                pHierNew != null &&
                pHierNew.GetCanonicalName(itemidNew, out var path) == 0)
            {
                UpdateStartupFile(path);
            }

            return 0;
        }

        int IVsSelectionEvents.OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            return 0;
        }

        int IVsSelectionEvents.OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return 0;
        }

        public void Refresh(string launchProfiles, string userFile, string flagFile, string projectDirectory)
        {
            launchProfilesPath = launchProfiles;
            this.userFile = userFile;
            this.flagFile = flagFile;
            this.projectDirectory = projectDirectory;
            watcher.Path = Path.GetDirectoryName(launchProfiles);
            ReloadProfiles();
        }

        private void ReloadProfiles(bool retry = true)
        {
            if (!File.Exists(launchProfilesPath))
            {
                return;
            }

            try
            {
                var json = JObject.Parse(File.ReadAllText(launchProfilesPath));
                if (json.Property("profiles") is not JProperty prop ||
                    prop.Value is not JObject profiles)
                {
                    return;
                }

                startupFiles = profiles.Properties().Select(p => p.Name)
                    .ToDictionary(x => x, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                if (retry)
                {
                    //Re-try once
                    Thread.Sleep(5);
                    ReloadProfiles(false);
                }
            }
            catch (Exception e)
            {
                Debug.Fail("Could not read launchSettings.json: " + e);
            }
        }

        private void UpdateStartupFile(string? path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && path!.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                {
                    var startupFile = PathNetCore.GetRelativePath(projectDirectory, path);
                    if (startupFiles.ContainsKey(startupFile))
                    {
                        // Get the value as it was exists in the original dictionary, 
                        // since it has to match what the source generator created in the 
                        // launch profiles.
                        startupFile = startupFiles[startupFile];
                        var xdoc = XDocument.Load(userFile);
                        var active = xdoc
                            .Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}ActiveDebugProfile")
                            .FirstOrDefault();

                        if (active != null && active.Value != startupFile)
                        {
                            active.Value = startupFile;
                            // First save to flag file so we don't cause another open 
                            // attempt via the OpenStartupFile task.
                            File.WriteAllText(flagFile, startupFile);
                            xdoc.Save(userFile);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Fail("Failed to load or update .user file.: " + e);
            }
        }
    }
}