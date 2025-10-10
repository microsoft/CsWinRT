// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <see cref="ComWrappers"/> implementation for Windows Runtime interop.
/// </summary>
internal sealed unsafe class WindowsRuntimeComWrappers : ComWrappers
{
    /// <summary>
    /// The <see cref="WindowsRuntimeMarshallingInfo"/> instance passed by callers that have already performed a lookup for it, enabling the <see cref="ComputeVtables"/> fast-path, if available.
    /// </summary>
    /// <remarks>
    /// This can be set by a thread right before calling the <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> method,
    /// to pass additional information to the <see cref="ComWrappers"/> instance. It should be set to <see langword="null"/>
    /// immediately afterwards, to ensure following calls won't accidentally see the wrong type.
    /// </remarks>
    [ThreadStatic]
    private static WindowsRuntimeMarshallingInfo? MarshallingInfo;

    /// <summary>
    /// The <see cref="WindowsRuntimeObjectComWrappersCallback"/> instance passed by callers where the target type was statically-visible,
    /// enabling the <see cref="CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> fast-path, if available.
    /// </summary>
    /// <remarks>
    /// This can be set by a thread right before calling the <see cref="ComWrappers.GetOrCreateObjectForComInstance(nint, CreateObjectFlags, object?)"/> method,
    /// to pass additional information to the <see cref="ComWrappers"/> instance. It should be set to <see langword="null"/>
    /// immediately afterwards, to ensure following calls won't accidentally see the wrong type.
    /// </remarks>
    [ThreadStatic]
    private static WindowsRuntimeObjectComWrappersCallback? ObjectComWrappersCallback;

    /// <summary>
    /// The <see cref="WindowsRuntimeUnsealedObjectComWrappersCallback"/> instance passed by callers where the target type was statically-visible,
    /// enabling the unsealed <see cref="CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> fast-path, if available.
    /// </summary>
    /// <remarks>
    /// The same remarks as with <see cref="ObjectComWrappersCallback"/> apply here, the only difference is this callback is for unsealed types.
    /// </remarks>
    [ThreadStatic]
    private static WindowsRuntimeUnsealedObjectComWrappersCallback? UnsealedObjectComWrappersCallback;

    /// <summary>
    /// The derived interface pointer to use for marshalling (this should always be supplied).
    /// </summary>
    [ThreadStatic]
    private static void* CreateObjectTargetInterfacePointer;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeComWrappers"/> instance.
    /// </summary>
    private WindowsRuntimeComWrappers()
    {
    }

    /// <summary>
    /// Gets the shared default instance of <see cref="WindowsRuntimeComWrappers"/>.
    /// </summary>
    /// <remarks>
    /// This instance is the one that CsWinRT will use to marshal all Windows Runtime objects.
    /// </remarks>
    public static WindowsRuntimeComWrappers Default { get; } = CreateAndRegisterDefault();

    /// <summary>
    /// Creates and registers the default instance of <see cref="WindowsRuntimeComWrappers"/>.
    /// </summary>
    /// <returns>The default instance of <see cref="WindowsRuntimeComWrappers"/>.</returns>
    private static WindowsRuntimeComWrappers CreateAndRegisterDefault()
    {
        WindowsRuntimeComWrappers instance = new();

        RegisterForTrackerSupport(instance);

        return instance;
    }

    /// <summary>
    /// Calls <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> with the appropriate marshaller and <see cref="CreateComInterfaceFlags"/> value.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <remarks>
    /// <para>
    /// This method differs from <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> in that it lookup the appropriate
    /// <see cref="WindowsRuntimeMarshallingInfo"/> value and marshal the object via its <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/>,
    /// if present. This is why no input <see cref="CreateComInterfaceFlags"/> value is needed: this method will figure out the
    /// right one to use automatically. If <paramref name="instance"/> is a custom mapped type that is marshalled as a native
    /// value directly, then this call may create such an object and bypass <see cref="ComWrappers"/> entirely. If no marshaller
    /// is available, the general <see cref="object"/> marshaller will be used.
    /// </para>
    /// <para>
    /// This method should only be used when no static type information is available. Otherwise, the overloads below should be used.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="instance"/> cannot be marshalled.</exception>
    /// <seealso cref="ComWrappers.GetOrCreateComInterfaceForObject"/>
    public nint GetOrCreateComInterfaceForObject(object instance)
    {
        void* thisPtr;

        // If 'value' is not a projected Windows Runtime class, just marshal it via 'ComWrappers'. This will rely on 'ComputeVtables' to
        // lookup the proxy type for the object, which will allow scenarios such as custom mapped types, generic type instantiations, and
        // user-defined types implementing projected interfaces, to also work. If that's missing, we'll just get an opaque 'IInspectable'.
        if (WindowsRuntimeMarshallingInfo.TryGetInfo(instance.GetType(), out WindowsRuntimeMarshallingInfo? info))
        {
            MarshallingInfo = info;

            thisPtr = info.GetComWrappersMarshaller().GetOrCreateComInterfaceForObject(instance);
        }
        else
        {
            // Special case 'Exception', see notes in 'ComputeVtables' below for more details. Repeating this here
            // allows us to still skip the repeated lookup, as we already know we won't find a matching key pair.
            MarshallingInfo = instance is Exception
                ? WindowsRuntimeMarshallingInfo.GetInfo(typeof(Exception))
                : WindowsRuntimeMarshallingInfo.GetInfo(typeof(object));

            thisPtr = (void*)GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.TrackerSupport);
        }

        return (nint)thisPtr;
    }

    /// <summary>
    /// Calls <see cref="GetOrCreateComInterfaceForObject(object)"/> and then <c>QueryInterface</c> on the result.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <param name="iid">The IID of the interface to query after creating the object wrapper.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="instance"/> cannot be marshalled.</exception>
    /// <seealso cref="GetOrCreateComInterfaceForObject(object)"/>
    public nint GetOrCreateComInterfaceForObject(object instance, in Guid iid)
    {
        void* thisPtr = (void*)GetOrCreateComInterfaceForObject(instance);

        // 'ComWrappers' returns an 'IUnknown' pointer, so we need to do an actual 'QueryInterface' for the interface IID
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

        // We can release the 'IUnknown' reference now, it's no longer needed
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (if it doesn't, it's some kind of authoring error)
        Marshal.ThrowExceptionForHR(hresult);

        return (nint)interfacePtr;
    }

    /// <summary>
    /// Calls <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> and then <c>QueryInterface</c> on the result.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <param name="flags">Flags used to configure the generated interface.</param>
    /// <param name="iid">The IID of the interface to query after creating the object wrapper.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="instance"/> cannot be marshalled.</exception>
    /// <seealso cref="ComWrappers.GetOrCreateComInterfaceForObject"/>
    public nint GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags, in Guid iid)
    {
        // Marshal the object ('ComputeVtables' will lookup the proxy type to resolve the right vtable for it)
        void* thisPtr = (void*)GetOrCreateComInterfaceForObject(instance, flags);

        // Do the 'QueryInterface' call for the target interface IID, same as above
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

        // Release the original pointer, once again same as above (this is always guaranteed to not be 'null')
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        Marshal.ThrowExceptionForHR(hresult);

        return (nint)interfacePtr;
    }

    /// <summary>
    /// Get the currently registered managed object or creates a new managed object and registers it.
    /// </summary>
    /// <param name="externalComObject">Object to import for usage into the .NET runtime.</param>
    /// <param name="objectComWrappersCallback"><inheritdoc cref="ObjectComWrappersCallback" path="/summary/node()"/></param>
    /// <param name="unsealedObjectComWrappersCallback"><inheritdoc cref="UnsealedObjectComWrappersCallback" path="/summary/node()"/></param>
    /// <returns>Returns a managed object associated with the supplied external COM object.</returns>
    /// <remarks>
    /// <para>
    /// If a managed object was previously created for the specified <paramref name="externalComObject" />
    /// using this <see cref="ComWrappers" /> instance, the previously created object will be returned.
    /// If not, a new one will be created.
    /// </para>
    /// <para>
    /// Unlike the <see cref="ComWrappers.GetOrCreateObjectForComInstance(nint, CreateObjectFlags)"/> overloads,
    /// this method assumes that <paramref name="externalComObject"/> is some <c>IInspectable</c> interface pointer.
    /// Calling this method with a COM pointer that does not implement <c>IInspectable</c> is undefined behavior.
    /// </para>
    /// </remarks>
    public object GetOrCreateObjectForComInstanceUnsafe(
        nint externalComObject,
        WindowsRuntimeObjectComWrappersCallback? objectComWrappersCallback,
        WindowsRuntimeUnsealedObjectComWrappersCallback? unsealedObjectComWrappersCallback)
    {
        ObjectComWrappersCallback = objectComWrappersCallback;
        UnsealedObjectComWrappersCallback = unsealedObjectComWrappersCallback;
        CreateObjectTargetInterfacePointer = (void*)externalComObject;

        // We always pass no flags, as our implementation will use 'CreatedWrapperFlags' to signal the
        // appropriate flags back from the marshalling stubs. We do pass a user state so that we can
        // pick the overload that will actually observe the returned 'CreatedWrapperFlags' value.
        return GetOrCreateObjectForComInstance(externalComObject, CreateObjectFlags.None, userState: null);
    }

    /// <inheritdoc/>
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        WindowsRuntimeMarshallingInfo? marshallingInfo = MarshallingInfo;

        // Try to get the marshalling info for the input type. If we can't find it, we fallback to the marshalling info
        // for 'object'. This is the shared marshalling mode for all unknown objects, ie. just an opaque 'IInspectable'.
        // If we already have one available passed by callers up the stack, we can skip the lookup and just use it.
        if (marshallingInfo is null && !WindowsRuntimeMarshallingInfo.TryGetInfo(obj.GetType(), out marshallingInfo))
        {
            // Special case for exception types, which won't have their own type map entry, but should all map to the
            // same marshalling info for 'Exception'. Since we have an instance here, we can just check directly.
            marshallingInfo = obj is Exception
                ? WindowsRuntimeMarshallingInfo.GetInfo(typeof(Exception))
                : WindowsRuntimeMarshallingInfo.GetInfo(typeof(object));
        }

        // Get the vtable from the current marshalling info (it will get cached in that instance)
        WindowsRuntimeVtableInfo vtableInfo = marshallingInfo.GetVtableInfo();

        count = vtableInfo.Count;

        // The computed vtable will unconditionally include 'IUnknown' as the last vtable entry.
        // However, this entry should only be included if the 'CallerDefinedIUnknown' flag is set.
        // To achieve this, we can just decrement the count by 1 in case the flag is not set.
        if (count != 0 && !flags.HasFlag(CreateComInterfaceFlags.CallerDefinedIUnknown))
        {
            count--;
        }

        return vtableInfo.VtableEntries;
    }

    /// <inheritdoc/>
    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        return CreateObject(externalComObject, flags, userState: null, wrapperFlags: out _);
    }

    /// <inheritdoc/>
    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags, object? userState, out CreatedWrapperFlags wrapperFlags)
    {
        // Retrieve the 'IInspectable' interface pointer, to avoid 'QueryInterface' calls.
        // We always need this, and all callers are expected to be providing one.
        void* interfacePointer = CreateObjectTargetInterfacePointer;

        // Validate that we do have an input interface pointer, just in case.
        // We might want to relax this restriction later if actually needed.
        ArgumentNullException.ThrowIfNull(interfacePointer);

        // If we have a callback instance, it means this invocation was for a statically visible type that can
        // always be marshalled directly (eg. a delegate type, or a sealed type). In that case, just use it.
        if (ObjectComWrappersCallback is { } createObjectCallback)
        {
            // Callers will have provided a derived interface pointer, if statically visible. For instance, if
            // a native API returned an instance of a delegate type, we can reuse that, rather than doing a
            // 'QueryInterface' call again from here (because 'CreateObject' only ever gets an 'IUnknown'
            // object as input). So we can just forward that here directly to the provided callback.
            return createObjectCallback.CreateObject(interfacePointer, out wrapperFlags);
        }

        HSTRING className = null;

        // Get the runtime class name (we need it to figure out the most derived type to marshal)
        IInspectableVftbl.GetRuntimeClassNameUnsafe(interfacePointer, &className).Assert();

        try
        {
            // We only need the runtime class name in this scope, so just get the raw characters
            ReadOnlySpan<char> runtimeClassName = HStringMarshaller.ConvertToManagedUnsafe(className);

            // If we have a callback instance for an unsealed type, it means that this invocation was for either an
            // unsealed type or some interface type. In this case, we do logic analogous to the case above, just with
            // the additional complexity due to also needing the runtime class name to figure out the managed type.
            if (UnsealedObjectComWrappersCallback is { } unsealedObjectCallback)
            {
                // We can now invoke the callback. If we have an exact match with the runtime class name that
                // the callback is specialized for, then we can just directly return the object it produced.
                if (unsealedObjectCallback.TryCreateObject(
                    value: interfacePointer,
                    runtimeClassName: runtimeClassName,
                    out object? wrapperObject,
                    out wrapperFlags))
                {
                    return wrapperObject;
                }
            }

            // We didn't find an exact match, so we need to walk the parent types in the hierarchy. Note that
            // if the type is an interface type, this will simply not find any matches and return immediately.
            // This also covers cases where we have no static type information whatsoever (i.e. we're just
            // marshalling 'object'). This traversal allows us to still return derived types, when not trimmed.
            if (WindowsRuntimeMarshallingInfo.TryGetMostDerivedInfo(
                runtimeClassName: runtimeClassName,
                info: out WindowsRuntimeMarshallingInfo? unsealedMarshallingInfo))
            {
                return unsealedMarshallingInfo.GetComWrappersMarshaller().CreateObject(interfacePointer, out wrapperFlags);
            }

            // We couldn't find any partially derived type to marshal: just return an opaque object.
            // It can still be used via interfaces by doing 'IDynamicInterfaceCastable' casts on it.
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshaller.CreateObjectReference(
                externalComObject: interfacePointer,
                iid: in WellKnownInterfaceIds.IID_IInspectable,
                wrapperFlags: out wrapperFlags);

            // Because we created object reference for exactly 'IInspectable', we can optimize things here by pre-initializing that
            // property, so we can reuse that same instance later, rather than creating a new one and doing 'QueryInterface' again.
            return new WindowsRuntimeInspectable(objectReference) { InspectableObjectReference = objectReference };
        }
        finally
        {
            HStringMarshaller.Free(className);
        }
    }

    /// <inheritdoc/>
    protected override void ReleaseObjects(IEnumerable objects)
    {
        foreach (object? obj in objects)
        {
            // We are intentionally only releasing objects as a best effort, when we can unwrap them. This method is usually
            // only called when XAML notifies the tracker runtime that a thread is shutting down (e.g. when closing a XAML
            // window, such as 'CoreWindow'), so that objects that are tied to that context have a chance of being released.
            // 'ComWrappers' will pass all objects that have been created so far and that are eligible for release though,
            // meaning we can very well encounter objects that we cannot unwrap and dispose manually. In those cases, we
            // are just skipping them to avoid crashing the app (there were several reports of this causing issues before).
            if (WindowsRuntimeMarshal.TryUnwrapObjectReference(obj, out WindowsRuntimeObjectReference? objReference))
            {
                objReference.Dispose();
            }
        }
    }
}
