// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IBufferByteAccess</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/robuffer/ns-robuffer-ibufferbyteaccess"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IBufferByteAccessVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, byte**, HRESULT> Buffer;

    /// <summary>
    /// Gets the array of bytes in the buffer.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="value">The byte array.</param>
    /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT BufferUnsafe(void* thisPtr, byte** value)
    {
        return ((IBufferByteAccessVftbl*)*(void***)thisPtr)->Buffer(thisPtr, value);
    }
}
#endif