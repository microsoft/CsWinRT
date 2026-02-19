// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IMemoryBufferByteAccess</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/previous-versions//mt297505(v=vs.85)"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IMemoryBufferByteAccessVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, byte**, uint*, HRESULT> GetBuffer;

    /// <summary>
    /// Gets an <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybuffer"><c>IMemoryBuffer</c></see> as an array of bytes.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="value">A pointer to a byte array containing the buffer data.</param>
    /// <param name="capacity">The number of bytes in the returned array.</param>
    /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetBufferUnsafe(void* thisPtr, byte** value, uint* capacity)
    {
        return ((IMemoryBufferByteAccessVftbl*)*(void***)thisPtr)->GetBuffer(thisPtr, value, capacity);
    }
}