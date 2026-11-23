// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="IPropertyValueImpl"/>
public unsafe partial class IPropertyValueImpl
{
    /// <summary>
    /// Gets the <c>IPropertyValue</c> vtable for signed 32-bit enums.
    /// </summary>
    public static nint Int32EnumVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Int32EnumPropertyValueImpl.Vftbl);
    }

    /// <summary>
    /// Gets the <c>IPropertyValue</c> vtable for unsigned 32-bit enums.
    /// </summary>
    public static nint UInt32EnumVtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in UInt32EnumPropertyValueImpl.Vftbl);
    }
}

/// <summary>
/// Numeric coercion for signed enums, preserving their <c>OtherType</c> identity.
/// </summary>
file static unsafe class Int32EnumPropertyValueImpl
{
    /// <summary>
    /// The enum vtable, sharing numeric getters with the underlying primitive.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static Int32EnumPropertyValueImpl()
    {
        // Boxed enums can be unboxed as their underlying primitive, so all coercion and overflow
        // checks can be shared without reflection, per-enum code, or additional boxing.
        Vftbl = *(IPropertyValueVftbl*)ABI.System.Int32PropertyValueImpl.Vtable;
        Vftbl.get_Type = ((IPropertyValueVftbl*)IPropertyValueImpl.OtherTypeVtable)->get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
    }
}

/// <summary>
/// Numeric coercion for unsigned enums, preserving their <c>OtherType</c> identity.
/// </summary>
file static unsafe class UInt32EnumPropertyValueImpl
{
    /// <summary>
    /// The enum vtable, sharing numeric getters with the underlying primitive.
    /// </summary>
    [FixedAddressValueType]
    public static readonly IPropertyValueVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static UInt32EnumPropertyValueImpl()
    {
        Vftbl = *(IPropertyValueVftbl*)ABI.System.UInt32PropertyValueImpl.Vtable;
        Vftbl.get_Type = ((IPropertyValueVftbl*)IPropertyValueImpl.OtherTypeVtable)->get_Type;
        Vftbl.get_IsNumericScalar = &IPropertyValueImpl.get_IsNumericScalarFalse;
    }
}
