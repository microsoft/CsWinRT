// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

/// <summary>
/// The <c>IReference`1</c> implementation for a given non blittable type.
/// </summary>
/// <typeparam name="TAbi">The ABI type to use.</typeparam>
/// <typeparam name="TEntry">The type of vtable entry value.</typeparam>
internal static unsafe class NonBlittableReference<TAbi, TEntry>
    where TAbi : unmanaged
    where TEntry : IReferenceVtableEntry<TAbi>
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = GetAbiToProjectionVftablePtr();

    /// <summary>
    /// Computes the <c>IReference`1</c> implementation vtable.
    /// </summary>
    private static nint GetAbiToProjectionVftablePtr()
    {
        void** vftbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(TAbi), sizeof(void*) * 7);

        // TODO: initialize 'IInspectable' vtable

        vftbl[6] = TEntry.Value;

        return (nint)vftbl;
    }
}
