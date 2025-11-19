// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <see href="https://docs.rs/windows-sys/latest/windows_sys/Win32/System/Com/struct.ComCallData.html"/>
internal unsafe struct ComCallData
{
    public uint dwDispid;
    public uint dwReserved;

    /// <summary>
    /// Custom user-defined argument to pass to the callback invoked on the target context.
    /// </summary>
    public void* pUserDefined;
}