﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Buffers;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

[assembly: TypeMap<WindowsRuntimeTypeMapUniverse>(
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Matrix4x4>",
    target: typeof(ABI.System.Numerics.Matrix4x4),
    trimTarget: typeof(Matrix4x4))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Matrix4x4), typeof(ABI.System.Numerics.Matrix4x4))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.matrix4x4"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Matrix4x4>")]
[Matrix4x4ComWrappersMarshaller]
file static class Matrix4x4;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class Matrix4x4Marshaller
{
    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Matrix4x4? value)
    {
        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceMatrix4x4);
    }

    /// <inheritdoc cref="WindowsRuntimeValueTypeMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Matrix4x4? UnboxToManaged(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<global::System.Numerics.Matrix4x4>(value);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
internal sealed unsafe class Matrix4x4ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.None);
    }

    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferenceMatrix4x4,
            Vtable = Matrix4x4Reference.AbiToProjectionVftablePtr
        }]);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<global::System.Numerics.Matrix4x4>(value, in WellKnownInterfaceIds.IID_IReferenceMatrix4x4);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Matrix4x4"/>.
/// </summary>
file static unsafe class Matrix4x4Reference
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedInspectableVtableUnsafe(
        type: typeof(global::System.Numerics.Matrix4x4),
        fpEntry6: (delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Matrix4x4*, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, global::System.Numerics.Matrix4x4* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Numerics.Matrix4x4 unboxedValue = (global::System.Numerics.Matrix4x4)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(global::System.Numerics.Matrix4x4));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
