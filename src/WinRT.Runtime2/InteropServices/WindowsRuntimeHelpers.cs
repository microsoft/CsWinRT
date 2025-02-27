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

        Unsafe.Copy(vftbl, in *(IInspectableVftbl*)Inspectable.AbiToProjectionVftablePtr);

        return vftbl;
    }

    /// <summary>
    /// Allocates an initializes an <c>IReference`1</c> vtable that's associated with a given type.
    /// </summary>
    /// <param name="type">The type associated with the allocated memory.</param>
    /// <param name="fpValue">The function pointer for <c>IReference`1::Value</c>.</param>
    /// <returns>The resulting <c>IReference`1</c> vtable.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <see langword="null"/>.</exception>
    public static void** AllocateTypeAssociatedReferenceVtable(Type type, void* fpValue)
    {
        void** vftbl = AllocateTypeAssociatedInspectableVtable(type, 7);

        vftbl[6] = fpValue;

        return vftbl;
    }
}