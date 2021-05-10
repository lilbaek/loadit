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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using EnvDTE;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Loadit.VisualStudio.Build
{
    internal static class WindowsInterop
    {
        private static readonly Regex versionExpr = new Regex(@"Microsoft Visual Studio (?<version>\d\d\.\d)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static void EnsureOpened(string filePath, TimeSpan delay = default)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }

            if (delay != default)
            {
                Thread.Sleep(delay);
            }

            try
            {
                var dte = GetDTE();
                if (dte == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(filePath))
                {
                    dte.ExecuteCommand("File.OpenFile", filePath);
                }
            }
            catch (Exception e)
            {
                Debug.Fail($"Failed to open {filePath}: " + e);
            }
        }

        public static IServiceProvider? GetServiceProvider(TimeSpan delay = default)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return null;
            }

            if (delay != default)
            {
                Thread.Sleep(delay);
            }

            try
            {
                var dte = GetDTE();
                if (dte == null)
                {
                    return null;
                }

                return new OleServiceProvider(dte);
            }
            catch
            {
                //Debug.Fail("Failed to get IDE service provider.");
                return null;
            }
        }

        private static DTE? GetDTE()
        {
            //TODO: Find a better way to do this. We cannot be sure that it is the actual foreground window :/
            var window = NativeMethods.GetForegroundWindow();
            var process = Process.GetProcessesByName("devenv").FirstOrDefault(x => x.MainWindowHandle == window);
            if (process == null)
            {
                //Try by just getting the process:
                process = Process.GetProcessesByName("devenv").First();
                if (process == null)
                {
                    return null;
                }
            }
            var devEnv = process.MainModule.FileName;
            var version = versionExpr.Match(devEnv).Groups["version"];
            if (!version.Success)
            {
                var ini = Path.ChangeExtension(devEnv, "isolation.ini");
                if (!File.Exists(ini))
                {
                    throw new NotSupportedException("Could not determine Visual Studio version from running process from " + devEnv);
                }

                if (!Version.TryParse(File
                    .ReadAllLines(ini)
                    .Where(line => line.StartsWith("InstallationVersion=", StringComparison.Ordinal))
                    .FirstOrDefault()?
                    .Substring(20), out var v))
                {
                    throw new NotSupportedException("Could not determine the version of Visual Studio from devenv.isolation.ini at " + ini);
                }

                return GetComObject<DTE>(string.Format("!{0}.{1}.0:{2}",
                    "VisualStudio.DTE", v.Major, process.Id), TimeSpan.FromSeconds(2));
            }

            return GetComObject<DTE>(string.Format("!{0}.{1}:{2}",
                "VisualStudio.DTE", version.Value, process.Id), TimeSpan.FromSeconds(2));
        }

        private static T? GetComObject<T>(string monikerName, TimeSpan retryTimeout)
        {
            object? comObject;
            var stopwatch = Stopwatch.StartNew();
            do
            {
                comObject = GetComObject(monikerName);
                if (comObject != null)
                {
                    break;
                }

                Thread.Sleep(100);
            } while (stopwatch.Elapsed < retryTimeout);

            return (T) comObject;
        }

        private static object? GetComObject(string monikerName)
        {
            object? comObject = null;
            try
            {
                IRunningObjectTable table;
                IEnumMoniker moniker;
                if (NativeMethods.Failed(NativeMethods.GetRunningObjectTable(0, out table)))
                {
                    return null;
                }

                table.EnumRunning(out moniker);
                moniker.Reset();
                var pceltFetched = IntPtr.Zero;
                var rgelt = new IMoniker[1];

                while (moniker.Next(1, rgelt, pceltFetched) == 0)
                {
                    IBindCtx ctx;
                    if (!NativeMethods.Failed(NativeMethods.CreateBindCtx(0, out ctx)))
                    {
                        string displayName;
                        rgelt[0].GetDisplayName(ctx, null, out displayName);
                        if (displayName == monikerName)
                        {
                            table.GetObject(rgelt[0], out comObject);
                            return comObject;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return comObject;
        }
    }

    internal class OleServiceProvider : IServiceProvider
    {
        private readonly Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;

        public OleServiceProvider(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public OleServiceProvider(DTE dte)
            : this((Microsoft.VisualStudio.OLE.Interop.IServiceProvider) dte)
        {
        }

        public object? GetService(Type serviceType)
        {
            return GetService((serviceType ?? throw new ArgumentNullException(nameof(serviceType))).GUID);
        }

        private object? GetService(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            if (guid == NativeMethods.IID_IServiceProvider)
            {
                return serviceProvider;
            }

            try
            {
                var riid = NativeMethods.IID_IUnknown;
                if (NativeMethods.Succeeded(serviceProvider.QueryService(ref guid, ref riid, out var zero)) && IntPtr.Zero != zero)
                {
                    try
                    {
                        return Marshal.GetObjectForIUnknown(zero);
                    }
                    finally
                    {
                        Marshal.Release(zero);
                    }
                }
            }
            catch (Exception exception) when (
                exception is OutOfMemoryException ||
                exception is StackOverflowException ||
                exception is AccessViolationException ||
                exception is AppDomainUnloadedException ||
                exception is BadImageFormatException ||
                exception is DivideByZeroException)
            {
                throw;
            }

            return null;
        }
    }
}