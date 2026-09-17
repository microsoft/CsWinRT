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

    [LibraryImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe partial int RoOriginateErrorW(int error, uint cchMax, char* message);

    /// <summary>
    /// Clears the <c>IRestrictedErrorInfo</c> object associated with the current thread.
    /// </summary>
    [LibraryImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    internal static partial void RoClearError();

    /// <summary>
    /// Associates an <c>IRestrictedErrorInfo</c> object with the current thread, exactly like a failing
    /// native call would. That state is ambient and sticky: it outlives the call that produced it, until
    /// something else originates a new error or clears it. Tests use this to reproduce a "dirty" thread.
    /// </summary>
    /// <param name="error">The <c>HRESULT</c> to originate the error info for.</param>
    /// <param name="message">The error message to associate with the error info.</param>
    /// <returns>
    /// Whether the error info was originated. This is <see langword="false"/> only if <paramref name="error"/>
    /// is a success code. Originating replaces whatever error info the thread already had, so callers don't
    /// need to clear it first.
    /// </returns>
    internal static unsafe bool OriginateError(int error, string message)
    {
        fixed (char* pMessage = message)
        {
            return RoOriginateErrorW(error, (uint)message.Length, pMessage) != 0;
        }
    }

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
