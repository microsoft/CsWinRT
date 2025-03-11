// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <see cref="ComWrappers"/> implementation for Windows Runtime interop.
/// </summary>
internal sealed unsafe class WindowsRuntimeComWrappers : ComWrappers
{
    /// <summary>
    /// The statically-visible delegate type that should be used by <see cref="CreateObject"/>, if available.
    /// </summary>
    /// <remarks>
    /// This can be set by a thread right before calling <see cref="CreateObject"/>, to pass additional
    /// information to the <see cref="ComWrappers"/> instance. It should be set to <see langword="null"/>
    /// immediately afterwards, to ensure following calls won't accidentally see the wrong type.
    /// </remarks>
    [ThreadStatic]
    internal static Type? CreateDelegateTargetType;

    /// <summary>
    /// The statically-visible object type that should be used by <see cref="CreateObject"/>, if available.
    /// </summary>
    /// <remarks><inheritdoc cref="CreateDelegateTargetType" path="/remarks/node()"/></remarks>
    [ThreadStatic]
    internal static Type? CreateObjectTargetType;

    /// <summary>
    /// Gets the shared default instance of <see cref="WindowsRuntimeComWrappers"/>.
    /// </summary>
    /// <remarks>
    /// This instance is the one that CsWinRT will use to marshall all Windows Runtime objects.
    /// </remarks>
    public static WindowsRuntimeComWrappers Default { get; } = new();

    /// <inheritdoc/>
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        // Try to get the marshalling info for the input type. If we can't find it, we fallback to the marshalling info
        // for 'object'. This is the shared marshalling mode for all unknown objects, ie. just an opaque 'IInspectable'.
        if (!WindowsRuntimeMarshallingInfo.TryGetInfo(obj.GetType(), out WindowsRuntimeMarshallingInfo? marshallingInfo))
        {
            marshallingInfo = WindowsRuntimeMarshallingInfo.GetInfo(typeof(object));
        }

        // Get the vtable from the current marshalling info (it will get cached in that instance)
        WindowsRuntimeVtableInfo vtableInfo = marshallingInfo.GetVtableInfo();

        count = vtableInfo.Count;

        // The computed vtable will unconditionally include 'IUnknown' as the last vtable entry.
        // However, this entry should only be included if the 'CallerDefinedIUnknown' flag is set.
        // To achieve this, we can just decrement the coutn by 1 in case the flag is not set.
        if (count != 0 && !flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
        {
            count--;
        }

        return vtableInfo.VtableEntries;
    }

    /// <inheritdoc/>
    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        // If we have a target delegate type, it means that we are creating an RCW for a statically visible
        // Windows Runtime delegate type (eg. a return type, or a parameter type). In this case, we can look
        // up the mapped type for it, and use its delegate marshaller. In this scenario, the marshalling logic
        // must exist, and there's no support for marshalling "some opaque object". That is, if we fail to get
        // the mapped type, or if we can't find the marshalling logic on the mapped type, we just throw.
        if (CreateDelegateTargetType is Type delegateType)
        {
            return WindowsRuntimeMarshallingInfo.GetInfo(delegateType).GeDelegateMarshaller().ConvertToManaged((void*)externalComObject);
        }

        // For all other supported objects (ie. Windows Runtime objects), we expect to have an input 'IInspectable' object.
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe((void*)externalComObject, in WellKnownInterfaceIds.IID_IInspectable, out void* inspectablePtr);

        // The input object is not some 'IInspectable', so we can't handle it in this 'ComWrappers' implementation.
        // We return 'null' so that the runtime can still do its logic as a fallback for 'IUnknown' and 'IDispatch'.
        if (!WellKnownErrorCodes.Succeeded(hresult))
        {
            return null;
        }

        try
        {
            if (CreateObjectTargetType is Type { IsSealed: true } objectType)
            {
                return WindowsRuntimeMarshallingInfo.GetInfo(objectType).GetObjectMarshaller().ConvertToManaged(inspectablePtr);
            }
        }
        finally
        {
            // Make sure not to leak the object, if someone else hasn't taken ownership of it just yet
            if (inspectablePtr != null)
            {
                _ = IUnknownVftbl.ReleaseUnsafe(inspectablePtr);
            }
        }
    }

    /// <inheritdoc/>
    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotImplementedException();
    }
}