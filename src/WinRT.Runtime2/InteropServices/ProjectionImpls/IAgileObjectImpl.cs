// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IAgileObject</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidlbase/nn-objidlbase-iagileobject"/>
public static class IAgileObjectImpl
{
    /// <summary>
    /// Gets a pointer to the managed <c>IAgileObject</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        // The 'IAgileObject' interface is a marker interface, so we can reuse 'IUnknown'
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IUnknownImpl.Vtable;
    }
}
#endif