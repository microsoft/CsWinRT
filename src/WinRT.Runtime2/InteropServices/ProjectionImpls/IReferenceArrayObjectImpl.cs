// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IReferenceArray&lt;Object&gt;</c> implementation shared by covariant reference-type arrays.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IReferenceArrayObjectImpl
{
    /// <summary>
    /// The vtable for the managed inspectable array implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IReferenceArrayVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IReferenceArrayObjectImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = (delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT>)(delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT>)&InspectableArrayPropertyValueImpl.GetInspectableArray;
    }

    /// <summary>
    /// Gets the IID of <c>IReferenceArray&lt;Object&gt;</c>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data =
            [
                0x4F, 0xA8, 0xD7, 0x9C, 0x80, 0x0C, 0xC5, 0x59,
                0xB4, 0x4E, 0x97, 0x78, 0x41, 0xBB, 0x43, 0xD9
            ];

            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    /// <summary>
    /// Gets a pointer to the managed inspectable array implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }
}
