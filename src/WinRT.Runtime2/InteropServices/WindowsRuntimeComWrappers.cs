// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
    /// Tries to marshal a managed object using the appropriate marshaller, only if exact marshalling info is available for that type.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <param name="comObject">The generated COM interface that can be passed outside the .NET runtime.</param>
    /// <returns>Whether <paramref name="comObject"/> was retrieved successfully.</returns>
    /// <remarks>
    /// This method differs from <see cref="GetOrCreateComInterfaceForObject(object)"/> in that it will only succeed if
    /// exact marshalling info is available for <paramref name="instance"/>. Otherwise, it will fail and not marshal the
    /// managed object at all, rather than trying to marshal it with an approximate type (e.g. as an opaque <c>IInspectable</c>).
    /// </remarks>
    public static bool TryGetOrCreateComInterfaceForObjectExact(object instance, out nint comObject)
    {
        // Try to retrieve the marshalling info for exactly the current type, and use it to marshal the info if available.
        // If we don't have an exact match, we stop here and fail rather than marshalling as an opaque 'IInspectable' object.
        if (WindowsRuntimeMarshallingInfo.TryGetInfo(instance.GetType(), out WindowsRuntimeMarshallingInfo? info))
        {
            MarshallingInfo = info;

            comObject = (nint)info.GetComWrappersMarshaller().GetOrCreateComInterfaceForObject(instance);

            return true;
        }

        comObject = default;

        return false;
    }

    /// <summary>
    /// Marshals a managed object using the appropriate marshaller, assuming that exact marshalling info is available for that type.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <remarks><inheritdoc cref="TryGetOrCreateComInterfaceForObjectExact" path="/remarks/node()"/></remarks>
    public static nint GetOrCreateComInterfaceForObjectExact(object instance)
    {
        WindowsRuntimeMarshallingInfo info = WindowsRuntimeMarshallingInfo.GetInfo(instance.GetType());

        MarshallingInfo = info;

        return (nint)info.GetComWrappersMarshaller().GetOrCreateComInterfaceForObject(instance);
    }

    /// <summary>
    /// Marshals a managed object using the appropriate marshaller, assuming that exact marshalling info is available for that type.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <param name="iid">The IID of the interface to query after creating the object wrapper.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="instance"/> cannot be marshalled.</exception>
    /// <remarks><inheritdoc cref="TryGetOrCreateComInterfaceForObjectExact" path="/remarks/node()"/></remarks>
    public static nint GetOrCreateComInterfaceForObjectExact(object instance, in Guid iid)
    {
        void* thisPtr = (void*)GetOrCreateComInterfaceForObjectExact(instance);

        // 'ComWrappers' always returns an 'IUnknown' pointer, so we need to do an actual 'QueryInterface'
        // for the interface IID. This is always the case, even if we know the exact type being marshalled.
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

        // We can release the 'IUnknown' reference now, it's no longer needed.
        // The 'thisPtr' value is always guaranteed to not be 'null' here.
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (if it doesn't, it's some kind of authoring error)
        Marshal.ThrowExceptionForHR(hresult);

        return (nint)interfacePtr;
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
            // If we couldn't retrieve the marshalling info, get the one to marshal anonymous objects.
            // E.g. this would be the case when marshalling a custom exception type, or some 'Type'.
            MarshallingInfo = GetAnonymousInspectableMarshallingInfo(instance);

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

        // Release the original 'IUnknown' reference (same as above)
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (same as above)
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
        MarshallingInfo = null;

        // Marshal the object ('ComputeVtables' will lookup the proxy type to resolve the right vtable for it)
        void* thisPtr = (void*)GetOrCreateComInterfaceForObject(instance, flags);

        // Do the 'QueryInterface' call for the target interface IID, same as above
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

        // Release the original 'IUnknown' reference (same as above)
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (same as above)
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
            marshallingInfo = GetAnonymousInspectableMarshallingInfo(obj);
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

        // There is a rare case where we might not have an input interface pointer, and that is if this
        // call was directly to 'ComWrappers' through the rehydration path in 'WeakReference<T>'. That
        // is, if we had a 'WeakReference<T>' instance pointing to an RCW object which got collected,
        // if someone tries to access the target object, the 'WeakReference<T>' instance will directly
        // use 'ComWrappers' to try to create a new equivalent RCW object. In that case, it would not
        // go through the normal marshalling path that we expect, and no static type information would
        // be provided (since 'WeakReference<T>' is not passing the 'T' context to 'ComWrappers').
        if (interfacePointer is null)
        {
            // In this scenario, we need to ensure that no static callback is available. This is needed because
            // these callbacks expect a specific interface pointer as input, which we can't ever guarantee here.
            if (ObjectComWrappersCallback is not null || UnsealedObjectComWrappersCallback is not null)
            {
                WindowsRuntimeComWrappersExceptions.ThrowInvalidOperationException();
            }

            // Manually 'QueryInterface' to retrieve the 'IInspectable' object we need
            IUnknownVftbl.QueryInterfaceUnsafe((void*)externalComObject, in WellKnownWindowsInterfaceIIDs.IID_IInspectable, out interfacePointer).Assert();
        }

        try
        {
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

            // Get the runtime class name (we need it to figure out the most derived type to marshal).
            // This should generally always succeed, but there are some cases where a public API
            // returns a non-projected object that implements a projected Windows Runtime interface,
            // without implementing 'GetRuntimeClassName' (e.g. 'MemoryBuffer.CreateReference').
            if (IInspectableVftbl.GetRuntimeClassNameUnsafe(interfacePointer, &className).Succeeded())
            {
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
                }
                finally
                {
                    HStringMarshaller.Free(className);
                }
            }

            // If we get here, it means that we couldn't find any partially derived type to marshal. However, we want to leverage
            // as much static type information as possible, so first we check if we have a callback for an unsealed type. If we do,
            // we can delegate to it to create the resulting object. Consider a scenario where some code called a Windows Runtime
            // API returning an anonymous object implementing an interface type, but without implementing 'GetRuntimeClassName'
            // (or with it returning an incorrect value that we can't recognize). Even in that case, we still have the static type
            // information about the returned interface type, meaning we know the object to return should be "at least that interface
            // type". And we know that the interface pointer is also already an interface pointer for that specific interface. So by
            // invoking the provided callback, if available, we still manage to return a specialized RCW type, which will have the
            // requested interface (and possibly others too) implemented in metadata. This allows callers to not have to go through
            // dynamic interface casts to consume those interfaces, which improves performance.
            if (UnsealedObjectComWrappersCallback is { } fallbackUnsealedObjectCallback)
            {
                return fallbackUnsealedObjectCallback.CreateObject(interfacePointer, out wrapperFlags);
            }

            // As a last resort, if we couldn't find any partially derived type to marshal and we also had no static type
            // information at all, we just return an opaque object. It can still be used via interfaces (including generics)
            // by doing 'IDynamicInterfaceCastable' casts on it. It will still work the same, just with lower performance.
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
                externalComObject: interfacePointer,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IInspectable,
                wrapperFlags: out wrapperFlags);

            // Because we created object reference for exactly 'IInspectable', we can optimize things here by pre-initializing that
            // property, so we can reuse that same instance later, rather than creating a new one and doing 'QueryInterface' again.
            return new WindowsRuntimeInspectable(objectReference) { InspectableObjectReference = objectReference };
        }
        finally
        {
            // If we hit the special case mentioned above where we had to manually 'QueryInterface' for 'IInspectable', as the caller
            // hadn't provided a valid interface pointer, we need to also make sure to release that acquired interface pointer here.
            if (CreateObjectTargetInterfacePointer is null)
            {
                _ = IUnknownVftbl.ReleaseUnsafe(interfacePointer);
            }
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
            if (WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(obj, out WindowsRuntimeObjectReference? objReference))
            {
                objReference.Dispose();
            }
        }
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeMarshallingInfo"/> value to marshal an anonymous (managed) object.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <returns>The <see cref="WindowsRuntimeMarshallingInfo"/> value to use to marshal <paramref name="instance"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static WindowsRuntimeMarshallingInfo GetAnonymousInspectableMarshallingInfo(object instance)
    {
        // Special case for (derived) exception types, which won't have their own type map entry, but should all map
        // to the same marshalling info for 'Exception'. Since we have an instance here, we can just check directly.
        // Note that custom exception types that might implement additional interfaces will still just be marshalled
        // as any other exception type (i.e. as just 'HResult'). This is intended and by design.
        if (instance is Exception)
        {
            return WindowsRuntimeMarshallingInfo.GetInfo(typeof(Exception));
        }

        // Special case for 'Type' instances too. This is needed even without considering custom user-defined types
        // (which shouldn't really be common anyway), because 'Type' itself is just a base type and not instantiated.
        // That is, when e.g. doing 'typeof(Foo)', the actual object is some 'RuntimeType' object itself (non public).
        if (instance is Type)
        {
            return WindowsRuntimeMarshallingInfo.GetInfo(typeof(Type));
        }

        // For all other cases, we fallback to the marshalling info for 'object'. This is the
        // shared marshalling mode for all unknown objects, ie. just an opaque 'IInspectable'. 
        return WindowsRuntimeMarshallingInfo.GetInfo(typeof(object));
    }
}

/// <summary>
/// Exception stubs for <see cref="WindowsRuntimeComWrappers"/>.
/// </summary>
file static class WindowsRuntimeComWrappersExceptions
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when in an invalid state.
    /// </summary>
    [DoesNotReturn]
    [StackTraceHidden]
    public static void ThrowInvalidOperationException()
    {
        throw new InvalidOperationException(
            $"The Windows Runtime 'ComWrappers' instance is not in a valid state to marshal an opaque 'IInspectable' object. " +
            $"If no static type information is available, it is not possible to leverage any of the static callback types. " +
            $"This scenario should never be hit. Please file an issue at https://github.com/microsoft/CsWinRT.");
    }
}