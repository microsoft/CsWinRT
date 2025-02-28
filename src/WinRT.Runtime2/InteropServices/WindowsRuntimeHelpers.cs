// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Interop helpers for interacting with Windows Runtime types.
/// </summary>
public static unsafe class WindowsRuntimeHelpers
{
    /// <summary>
    /// Allocates an initializes an <c>IInspectable</c> vtable that's associated with a given type.
    /// </summary>
    /// <param name="type">The type associated with the allocated memory.</param>
    /// <param name="count">The number of total entries in the resulting vtable.</param>
    /// <returns>The resulting <c>IInspectable</c> vtable.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than <c>6</c> (the size of <c>IInspectable</c>).</exception>
    public static void** AllocateTypeAssociatedInspectableVtable(Type type, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 6);

        void** vftbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(type, sizeof(void*) * count);

        Unsafe.Copy(vftbl, in *(IInspectableVftbl*)IInspectable.AbiToProjectionVftablePtr);

        return vftbl;
    }

    /// <summary>
    /// Allocates an initializes an <c>IInspectable</c> vtable that's associated with a given type, with additional vtable entries.
    /// </summary>
    /// <param name="type">The type associated with the allocated memory.</param>
    /// <param name="fpEntry6">The function pointer at index <c>6</c> in the vtable.</param>
    /// <returns>The resulting <c>IInspectable</c> vtable.</returns>
    /// <remarks>
    /// It is the caller's responsibility to ensure the additional vtable entry is valid and has the correct signature for the resulting vtable.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <see langword="null"/>.</exception>
    public static void** AllocateTypeAssociatedInspectableVtableUnsafe(Type type, void* fpEntry6)
    {
        void** vftbl = AllocateTypeAssociatedInspectableVtable(type, 7);

        vftbl[6] = fpEntry6;

        return vftbl;
    }
}