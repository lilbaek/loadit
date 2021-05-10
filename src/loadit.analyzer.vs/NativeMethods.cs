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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using IBindCtx = System.Runtime.InteropServices.ComTypes.IBindCtx;
using IRunningObjectTable = System.Runtime.InteropServices.ComTypes.IRunningObjectTable;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace loadit.analyzer.vs
{
    internal static class NativeMethods
    {
        public const int ERROR_INVALID_PARAMETER = 0x57;
        public const int INVALID_HANDLE_VALUE = -1;
        public const int MAX_PATH = 260;
        public const int PROCESS_QUERY_INFORMATION = 0x400;
        public const int TH32CS_SNAPPROCESS = 2;

        public static readonly Guid IID_IServiceProvider = typeof(IServiceProvider).GUID;
        public static readonly Guid IID_IObjectWithSite = typeof(IObjectWithSite).GUID;
        public static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        [DllImport("ole32.dll")]
        internal static extern int CoRegisterMessageFilter(IMessageFilter lpMessageFilter, out IMessageFilter lplpMessageFilter);

        [DllImport("ole32.dll")]
        internal static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        internal static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        internal static bool Succeeded(int hr)
        {
            return hr >= 0;
        }

        internal static bool Failed(int hr)
        {
            return hr < 0;
        }

        [Serializable] [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct INTERFACEINFO
        {
            [MarshalAs(UnmanagedType.IUnknown)]
            public object punk;

            public Guid iid;

            [ComAliasName("Microsoft.VisualStudio.OLE.Interop.WORD")]
            public ushort wMethod;
        }

        /// <summary>
        ///     Enables handling of incoming and outgoing COM messages while waiting for responses from synchronous calls. You
        ///     can use message filtering to prevent waiting on a synchronous call from blocking another application. For more
        ///     information, see IMessageFilter.
        /// </summary>
        [ComImport] [ComConversionLoss] [InterfaceType(1)] [Guid("00000016-0000-0000-C000-000000000046")]
        public interface IMessageFilter
        {
            [return: ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
            [PreserveSig] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint HandleInComingCall([In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
                uint dwCallType, [In]
                IntPtr htaskCaller, [In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
                uint dwTickCount, [In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.INTERFACEINFO")] [MarshalAs(UnmanagedType.LPArray)]
                INTERFACEINFO[] lpInterfaceInfo);

            [return: ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
            [PreserveSig] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint RetryRejectedCall([In]
                IntPtr htaskCallee, [In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
                uint dwTickCount, [In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
                uint dwRejectType);

            [return: ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
            [PreserveSig] [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint MessagePending([In]
                IntPtr htaskCallee, [In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
                uint dwTickCount, [In] [ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
                uint dwPendingType);
        }
    }
}