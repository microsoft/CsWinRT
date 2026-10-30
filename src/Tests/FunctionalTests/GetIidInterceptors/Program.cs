// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices;

if (WindowsRuntimeMarshal.GetIid<object>() != new Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90"))
    return 101;
if (WindowsRuntimeMarshal.GetIid<IDisposable>() != new Guid("30D5A829-7FA4-4026-83BB-D75BAE4EA99E"))
    return 102;
if (WindowsRuntimeMarshal.GetIid<int?>() != new Guid("548CEFBD-BC8A-5FA0-8DF2-957440FC8BF4"))
    return 103;
if (WindowsRuntimeMarshal.GetIid<IList<int>>() != new Guid("B939AF5B-B45D-5489-9149-61442C1905FE"))
    return 104;
if (WindowsRuntimeMarshal.GetIid<int[]>() != new Guid("A6D080A5-B087-5BC2-9A9F-5CD687B4D1F7"))
    return 105;
if (WindowsRuntimeMarshal.GetIid<Windows.Storage.Streams.DataReader>() != new Guid("E2B50029-B4C1-4314-A4B8-FB813A2F275E"))
    return 106;
if (WindowsRuntimeMarshal.GetIid<Windows.System.Threading.WorkItemHandler>() != new Guid("1D1A8B8B-FA66-414F-9CBD-B65FC99D17FA"))
    return 107;

#if IID_FALLBACK
if (Lookup<IList<int>>() != new Guid("B939AF5B-B45D-5489-9149-61442C1905FE"))
    return 108;
if (Lookup<int[]>() != new Guid("A6D080A5-B087-5BC2-9A9F-5CD687B4D1F7"))
    return 109;
try
{
    _ = Lookup<int>();
    return 110;
}
catch (NotSupportedException)
{
}
#endif

Console.WriteLine("All expected IIDs matched.");
return 100;

#if IID_FALLBACK
[MethodImpl(MethodImplOptions.NoInlining)]
static Guid Lookup<T>() => WindowsRuntimeMarshal.GetIid<T>();
#endif
