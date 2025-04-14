// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IUnknown</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iunknown"/>
internal static unsafe class IUnknownImpl
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
    /// Gets a pointer to the managed <c>IUnknown</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }
}
