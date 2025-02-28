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
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Vector4>",
    target: typeof(ABI.System.Numerics.Vector4),
    trimTarget: typeof(Vector4))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Vector4), typeof(ABI.System.Numerics.Vector4))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Vector4"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.vector4"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[Vector4VtableProvider]
public static class Vector4;

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.Numerics.Vector4"/>.
/// </summary>
file sealed class Vector4VtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferenceArrayOfVector4,
            Vtable = Vector4Reference.AbiToProjectionVftablePtr
        }]);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Vector4"/>.
/// </summary>
file static unsafe class Vector4Reference
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedReferenceVtable(
        type: typeof(global::System.Numerics.Vector4),
        fpValue: (delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Vector4*, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, global::System.Numerics.Vector4* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Numerics.Vector4 unboxedValue = (global::System.Numerics.Vector4)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(global::System.Numerics.Vector4));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
