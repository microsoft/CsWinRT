// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace UnitTest;
public static partial class UnitTestHelper
{
    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe partial char* WindowsGetStringRawBuffer(void* hstring, uint* length);

    [LibraryImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    public static unsafe partial int WindowsDeleteString(void* hstring);
}
