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

namespace Loadit.VisualStudio.Build
{
    internal class ActiveDocumentMonitor : MarshalByRefObject, IDisposable, IVsRunningDocTableEvents, IVsSelectionEvents
    {
        private readonly IVsRunningDocumentTable? _rdt;
        private readonly uint _rdtCookie;
        private readonly IVsMonitorSelection? _selection;
        private readonly uint _selectionCookie;
        private string _flagFile;
        private string _projectDirectory;

        private string _launchProfilesPath;
        private Dictionary<string, string> _startupFiles = new();
        private string _userFile;
        private readonly FileSystemWatcher _watcher;

        public ActiveDocumentMonitor(string launchProfilesPath, string userFile, string flagFile, string projectDirectory, IServiceProvider services)
        {
            _launchProfilesPath = launchProfilesPath;
            _userFile = userFile;
            _flagFile = flagFile;
            _projectDirectory = projectDirectory;

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(launchProfilesPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "launchSettings.json"
            };

            _watcher.Changed += (_, _) => ReloadProfiles();
            _watcher.Created += (_, _) => ReloadProfiles();
            _watcher.EnableRaisingEvents = true;
            ReloadProfiles();

            _rdt = (IVsRunningDocumentTable) services.GetService(typeof(SVsRunningDocumentTable));
            _rdt?.AdviseRunningDocTableEvents(this, out _rdtCookie);

            _selection = (IVsMonitorSelection) services.GetService(typeof(SVsShellMonitorSelection));
            _selection?.AdviseSelectionEvents(this, out _selectionCookie);
        }

        void IDisposable.Dispose()
        {
            if (_rdtCookie != 0 && _rdt != null)
            {
                _rdt.UnadviseRunningDocTableEvents(_rdtCookie);
            }

            if (_selectionCookie != 0 && _selection != null)
            {
                _selection.UnadviseSelectionEvents(_selectionCookie);
            }

            _watcher.Dispose();
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            try
            {
                if ((grfAttribs & (uint) __VSRDTATTRIB.RDTA_DocDataReloaded) != 0 ||
                    (grfAttribs & (uint) __VSRDTATTRIB.RDTA_MkDocument) != 0)
                {
                    UpdateStartupFile(((IVsRunningDocumentTable4) _rdt!).GetDocumentMoniker(docCookie));
                }
            }
            catch
            {
                //Ignore
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
            // The MSBuild targets should have created it in target SelectStartupFile.
            try
            {
                UpdateStartupFile(((IVsRunningDocumentTable4) _rdt!).GetDocumentMoniker(docCookie));
            }
            catch
            {
                //Ignore
            }
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
            _launchProfilesPath = launchProfiles;
            _userFile = userFile;
            _flagFile = flagFile;
            _projectDirectory = projectDirectory;
            _watcher.Path = Path.GetDirectoryName(launchProfiles);
            ReloadProfiles();
        }

        private void ReloadProfiles(bool retry = true)
        {
            if (!File.Exists(_launchProfilesPath))
            {
                return;
            }

            try
            {
                var json = JObject.Parse(File.ReadAllText(_launchProfilesPath));
                if (json.Property("profiles") is not JProperty prop ||
                    prop.Value is not JObject profiles)
                {
                    return;
                }

                _startupFiles = profiles.Properties().Select(p => p.Name)
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
                //Debugger.Launch();
                if (!string.IsNullOrEmpty(path) && path!.IndexOfAny(Path.GetInvalidPathChars()) == -1)
                {
                    var startupFile = PathNetCore.GetRelativePath(_projectDirectory, path);
                    if (_startupFiles.ContainsKey(startupFile) && startupFile != "Startup.cs")
                    {
                        // Get the value as it was exists in the original dictionary, 
                        // since it has to match what the source generator created in the 
                        // launch profiles.
                        startupFile = _startupFiles[startupFile];
                        if (File.Exists(_userFile))
                        {
                            var xdoc = XDocument.Load(_userFile);
                            var active = xdoc
                                .Descendants("{http://schemas.microsoft.com/developer/msbuild/2003}ActiveDebugProfile")
                                .FirstOrDefault();

                            if (active != null && active.Value != startupFile)
                            {
                                active.Value = startupFile;
                                // First save to flag file so we don't cause another open 
                                // attempt via the OpenStartupFile task.
                                File.WriteAllText(_flagFile, startupFile);
                                xdoc.Save(_userFile);
                            }
                        }
                        else
                        {
                            Debug.Fail("The userfile: " + _userFile + " does not exist");
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