// Copyright (c) Microsoft Corporation.
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
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Plane>",
    target: typeof(ABI.System.Numerics.Plane),
    trimTarget: typeof(Plane))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Plane), typeof(ABI.System.Numerics.Plane))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.plane"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Plane>")]
[PlaneVtableProvider]
file static class Plane;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class PlaneMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeBlittableStructMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Plane? value)
    {
        return WindowsRuntimeBlittableStructMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferencePlane);
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableStructMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Plane? UnboxToManaged(void* value)
    {
        return WindowsRuntimeBlittableStructMarshaller.UnboxToManaged<global::System.Numerics.Plane>(value);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
file sealed class PlaneVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferencePlane,
            Vtable = PlaneReference.AbiToProjectionVftablePtr
        }]);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Plane"/>.
/// </summary>
file static unsafe class PlaneReference
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedInspectableVtableUnsafe(
        type: typeof(global::System.Numerics.Plane),
        fpEntry6: (delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Plane*, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, global::System.Numerics.Plane* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Numerics.Plane unboxedValue = (global::System.Numerics.Plane)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(global::System.Numerics.Plane));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
