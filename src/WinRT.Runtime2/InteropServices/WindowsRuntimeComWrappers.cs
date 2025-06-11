// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

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
    internal static WindowsRuntimeMarshallingInfo? MarshallingInfo;

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
    internal static WindowsRuntimeObjectComWrappersCallback? ObjectComWrappersCallback;

    /// <summary>
    /// The <see cref="WindowsRuntimeUnsealedObjectComWrappersCallback"/> instance passed by callers where the target type was statically-visible,
    /// enabling the unsealed <see cref="CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> fast-path, if available.
    /// </summary>
    /// <remarks>
    /// The same remarks as with <see cref="ObjectComWrappersCallback"/> apply here, the only difference is this callback is for unsealed types.
    /// </remarks>
    [ThreadStatic]
    internal static WindowsRuntimeUnsealedObjectComWrappersCallback? UnsealedObjectComWrappersCallback;

    /// <summary>
    /// The statically-visible object type that should be used by <see cref="CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>, if available.
    /// </summary>
    /// <remarks><inheritdoc cref="ObjectComWrappersCallback" path="/remarks/node()"/></remarks>
    [ThreadStatic]
    internal static Type? CreateObjectTargetType;

    /// <summary>
    /// The derived interface pointer to use for marshalling, if available.
    /// </summary>
    [ThreadStatic]
    internal static void* CreateObjectTargetInterfacePointer;

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
    /// Calls <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> and then <c>QueryInterface</c> on the result.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <remarks>
    /// This method differs from <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> in that it will always marshal
    /// <paramref name="instance"/> as an <c>IInspectable</c> object. It will do so by using the associated marshaller, if
    /// available. Otherwise, it will fallback to the generalized <see cref="object"/> marshaller for all other types.
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="instance"/> cannot be marshalled.</exception>
    /// <seealso cref="ComWrappers.GetOrCreateComInterfaceForObject"/>
    public nint GetOrCreateInspectableInterfaceForObject(object instance)
    {
        void* thisPtr;

        // If 'value' is not a projected Windows Runtime class, just marshal it via 'ComWrappers'. This will rely on 'ComputeVtables' to
        // lookup the proxy type for the object, which will allow scenarios such as custom mapped types, generic type instantiations, and
        // user-defined types implementing projected interfaces, to also work. If that's missing, we'll just get an opaque 'IInspectable'.
        if (WindowsRuntimeMarshallingInfo.TryGetInfo(instance.GetType(), out WindowsRuntimeMarshallingInfo? info))
        {
            MarshallingInfo = info;

            thisPtr = info.GetComWrappersMarshaller().GetOrCreateComInterfaceForObject(instance);

            MarshallingInfo = null;
        }
        else
        {
            // Special case 'Exception', see notes in 'ComputeVtables' below for more details. Repeating this here
            // allows us to still stip the repeated lookup, as we already know we won't find a matching key pair.
            MarshallingInfo = instance is Exception
                ? WindowsRuntimeMarshallingInfo.GetInfo(typeof(Exception))
                : WindowsRuntimeMarshallingInfo.GetInfo(typeof(object));

            thisPtr = (void*)GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.TrackerSupport);

            MarshallingInfo = null;
        }

        return (nint)thisPtr;
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

        // 'ComWrappers' returns an 'IUnknown' pointer, so we need to do an actual 'QueryInterface' for the interface IID
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in iid, out void* interfacePtr);

        // We can release the 'IUnknown' reference now, it's no longer needed
        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        // Ensure the 'QueryInterface' succeeded (if it doesn't, it's some kind of authoring error)
        Marshal.ThrowExceptionForHR(hresult);

        return (nint)interfacePtr;
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
        // If we have a callback instance, it means this invocation was for a statically visible type that can
        // always be marshalled directly (eg. a delegate type, or a sealed type). In that case, just use it.
        if (ObjectComWrappersCallback is { } createObjectCallback)
        {
            void* interfacePointer = CreateObjectTargetInterfacePointer;

            // Callers will have provided a derived interface pointer, if statically visible. For instance, if
            // a native API returned an instance of a delegate type, we can reuse that, rather than doing a
            // 'QueryInterface' call again from here (because 'CreateObject' only ever gets an 'IUnknown'
            // object as input). So we can just forward that here directly to the provided callback.
            return createObjectCallback.CreateObject(interfacePointer, out wrapperFlags);
        }

        HSTRING className = null;

        try
        {
            scoped ReadOnlySpan<char> runtimeClassName = default;

            if (UnsealedObjectComWrappersCallback is { } unsealedObjectCallback)
            {
                void* interfacePointer = CreateObjectTargetInterfacePointer;

                HRESULT hresult = IInspectableVftbl.GetRuntimeClassNameUnsafe(interfacePointer, &className);

                Marshal.ThrowExceptionForHR(hresult);

                runtimeClassName = HStringMarshaller.ConvertToManagedUnsafe(className);

                if (unsealedObjectCallback.TryCreateObject(
                    value: interfacePointer,
                    runtimeClassName: runtimeClassName,
                    out object? wrapperObject,
                    out wrapperFlags))
                {
                    return wrapperObject;
                }

                if (!WindowsRuntimeTypeHierarchy.TryGetBaseRuntimeClassName(
                    runtimeClassName: runtimeClassName,
                    out ReadOnlySpan<char> baseRuntimeClassName,
                    out int nextBaseRuntimeClassNameIndex))
                {
                    WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.AsUnsafe(interfacePointer, in WellKnownInterfaceIds.IID_IInspectable)!;

                    wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

                    return new WindowsRuntimeInspectable(objectReference) { InspectableObjectReference = objectReference };
                }

                if (WindowsRuntimeMarshallingInfo.TryGetInfo(baseRuntimeClassName, out WindowsRuntimeMarshallingInfo? baseMarshallingInfo))
                {
                    return baseMarshallingInfo.GetComWrappersMarshaller().CreateObject(interfacePointer, out wrapperFlags);
                }

                while (WindowsRuntimeTypeHierarchy.TryGetNextBaseRuntimeClassName(
                    baseRuntimeClassNameIndex: nextBaseRuntimeClassNameIndex,
                    baseRuntimeClassName: out baseRuntimeClassName,
                    nextBaseRuntimeClassNameIndex: out nextBaseRuntimeClassNameIndex))
                {

                }
            }
            else
            {

            }

            // Store the type so we do a single read from TLS
            Type? objectType = CreateObjectTargetType;

            // If we don't have an available callback, and also no statically visible type at all, the only possible
            // scenario is that the statically visible type was 'IInspectable'. In that case, we can opportunistically
            // skip the 'QueryInterface' call for it, as that interface pointer can be set by callers instead.
            void* inspectablePtr = null;

            // This optimization is only valid for 'IInspectable' object, not for specific types
            if (objectType is null)
            {
                inspectablePtr = CreateObjectTargetInterfacePointer;
            }
            else
            {
                // For all other supported objects (ie. Windows Runtime objects), we expect to have an input 'IInspectable' object.
                HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe((void*)externalComObject, in WellKnownInterfaceIds.IID_IInspectable, out inspectablePtr);

                // The input object is not some 'IInspectable', so we can't handle it in this 'ComWrappers' implementation.
                // We return 'null' so that the runtime can still do its logic as a fallback for 'IUnknown' and 'IDispatch'.
                Marshal.ThrowExceptionForHR(hresult);
            }

            try
            {
                // TODO
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
        finally
        {
            HStringMarshaller.Free(className);
        }

        wrapperFlags = CreatedWrapperFlags.None;

        return null;
    }

    /// <inheritdoc/>
    protected override void ReleaseObjects(IEnumerable objects)
    {
    }
}