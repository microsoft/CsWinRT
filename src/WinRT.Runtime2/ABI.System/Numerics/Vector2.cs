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
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Vector2>",
    target: typeof(ABI.System.Numerics.Vector2),
    trimTarget: typeof(Vector2))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Vector2), typeof(ABI.System.Numerics.Vector2))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.vector2"/>
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Numerics.Vector2>")]
[Vector2VtableProvider]
file static class Vector2;

/// <summary>
/// Marshaller for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class Vector2Marshaller
{
    /// <inheritdoc cref="WindowsRuntimeBlittableStructMarshaller.BoxToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(global::System.Numerics.Vector2? value)
    {
        return WindowsRuntimeBlittableStructMarshaller.BoxToUnmanaged(value, in WellKnownInterfaceIds.IID_IReferenceVector2);
    }

    /// <inheritdoc cref="WindowsRuntimeBlittableStructMarshaller.UnboxToManaged(void*)"/>
    public static global::System.Numerics.Vector2? UnboxToManaged(void* value)
    {
        return WindowsRuntimeBlittableStructMarshaller.UnboxToManaged<global::System.Numerics.Vector2>(value);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
file sealed class Vector2VtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferenceVector2,
            Vtable = Vector2Reference.AbiToProjectionVftablePtr
        }]);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Vector2"/>.
/// </summary>
file static unsafe class Vector2Reference
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedInspectableVtableUnsafe(
        type: typeof(global::System.Numerics.Vector2),
        fpEntry6: (delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Vector2*, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, global::System.Numerics.Vector2* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Numerics.Vector2 unboxedValue = (global::System.Numerics.Vector2)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(global::System.Numerics.Vector2));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
