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
    value: "Windows.Foundation.IReference<Windows.Foundation.Numerics.Quaternion>",
    target: typeof(ABI.System.Numerics.Quaternion),
    trimTarget: typeof(Quaternion))]

[assembly: TypeMapAssociation<WindowsRuntimeTypeMapUniverse>(typeof(Quaternion), typeof(ABI.System.Numerics.Quaternion))]

namespace ABI.System.Numerics;

/// <summary>
/// ABI type for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.numerics.quaternion"/>
[EditorBrowsable(EditorBrowsableState.Never)]
[QuaternionVtableProvider]
public static class Quaternion;

/// <summary>
/// A custom <see cref="WindowsRuntimeVtableProviderAttribute"/> implementation for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
file sealed class QuaternionVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
        bufferWriter.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IReferenceArrayOfQuaternion,
            Vtable = QuaternionReference.AbiToProjectionVftablePtr
        }]);
    }
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Numerics.Quaternion"/>.
/// </summary>
file static unsafe class QuaternionReference
{
    /// <summary>
    /// The vtable for the <c>IReference`1</c> implementation.
    /// </summary>
    public static nint AbiToProjectionVftablePtr { get; } = (nint)WindowsRuntimeHelpers.AllocateTypeAssociatedReferenceVtable(
        type: typeof(global::System.Numerics.Quaternion),
        fpValue: (delegate* unmanaged[MemberFunction]<void*, global::System.Numerics.Quaternion*, HRESULT>)&Value);

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Value(void* thisPtr, global::System.Numerics.Quaternion* result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            global::System.Numerics.Quaternion unboxedValue = (global::System.Numerics.Quaternion)ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            Unsafe.WriteUnaligned(result, unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            Unsafe.WriteUnaligned(result, default(global::System.Numerics.Quaternion));

            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
