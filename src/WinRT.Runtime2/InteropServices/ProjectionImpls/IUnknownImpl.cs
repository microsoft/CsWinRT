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
    /// The vtable for the <c>IUnknown</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = GetAbiToProjectionVftablePtr();

    /// <summary>
    /// Computes the <c>IUnknown</c> implementation vtable.
    /// </summary>
    private static nint GetAbiToProjectionVftablePtr()
    {
        IUnknownVftbl* vftbl = (IUnknownVftbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IUnknownImpl), sizeof(IUnknownVftbl));

        // Get the 'IUnknown' implementation from the runtime. This is implemented in native code,
        // so that it can work correctly even when used from native code during a GC (eg. from XAML).
        ComWrappers.GetIUnknownImpl(
            fpQueryInterface: out *(nint*)&vftbl->QueryInterface,
            fpAddRef: out *(nint*)&vftbl->AddRef,
            fpRelease: out *(nint*)&vftbl->Release);

        return (nint)vftbl;
    }
}
