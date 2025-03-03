// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Interop helpers for interacting with Windows Runtime types.
/// </summary>
public static unsafe class WindowsRuntimeHelpers
{
    /// <summary>
    /// Allocates an initializes an <c>IUnknown</c> vtable that's associated with a given type.
    /// </summary>
    /// <param name="type">The type associated with the allocated memory.</param>
    /// <param name="count">The number of total entries in the resulting vtable.</param>
    /// <returns>The resulting <c>IUnknown</c> vtable.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than <c>3</c> (the size of <c>IUnknown</c>).</exception>
    public static void** AllocateTypeAssociatedUnknownVtable(Type type, int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 3);

        void** vftbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(type, sizeof(void*) * count);

        Unsafe.Copy(vftbl, in *(IUnknownVftbl*)IUnknown.AbiToProjectionVftablePtr);

        return vftbl;
    }

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

    /// <summary>
    /// Checks whether a pointer to a COM object is actually a reference to a CCW produced for a managed object that was marshalled to native code.
    /// </summary>
    /// <param name="unknown">The pointer to a COM object.</param>
    /// <returns>Whether <paramref name="unknown"/> refers to a CCW for a managed object, rather than a native COM object.</returns>
    /// <remarks>
    /// This method does not validate the input <paramref name="unknown"/> pointer not being <see langword="null"/>.
    /// </remarks>
    public static bool IsReferenceToManagedObject(void* unknown)
    {
        IUnknownVftbl* unknownVftbl = (IUnknownVftbl*)(void**)unknown;
        IUnknownVftbl* runtimeVftbl = (IUnknownVftbl*)IUnknown.AbiToProjectionVftablePtr;

        return
            unknownVftbl->QueryInterface == runtimeVftbl->QueryInterface &&
            unknownVftbl->AddRef == runtimeVftbl->AddRef &&
            unknownVftbl->Release == runtimeVftbl->Release;
    }
}