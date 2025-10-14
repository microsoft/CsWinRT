// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IUnknown</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iunknown"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IUnknownImpl
{
    /// <summary>
    /// The <see cref="IUnknownVftbl"/> value for the managed <c>IUnknown</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IUnknownVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IUnknownImpl()
    {
        // Get the 'IUnknown' implementation from the runtime. This is implemented in native code,
        // so that it can work correctly even when used from native code during a GC (eg. from XAML).
        // Note: ILC has special support for this pattern, and can still pre-initialize this type.
        // The same applies to all other implementation types that also copy this base vtable type.
        ComWrappers.GetIUnknownImpl(
            fpQueryInterface: out *(nint*)&((IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl))->QueryInterface,
            fpAddRef: out *(nint*)&((IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl))->AddRef,
            fpRelease: out *(nint*)&((IUnknownVftbl*)Unsafe.AsPointer(ref Vftbl))->Release);
    }

    /// <summary>
    /// Gets the IID for the <c>IUnknown</c> interface.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IUnknown;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IUnknown</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }
}
