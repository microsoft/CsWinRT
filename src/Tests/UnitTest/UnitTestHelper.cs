// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;

namespace UnitTest;

public static partial class UnitTestHelper
{
    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    internal static unsafe partial char* WindowsGetStringRawBuffer(void* hstring, uint* length);

    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    internal static unsafe partial int WindowsDeleteString(void* hstring);

    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
    internal partial interface IWindowNative
    {
        IntPtr get_WindowHandle();
    }

    [GeneratedComInterface]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    internal partial interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }
}
