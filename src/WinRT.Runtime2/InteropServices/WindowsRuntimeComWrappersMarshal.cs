// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable CS1573, CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A marshaller with some utility methods that directly wrap <see cref="ComWrappers"/>.
/// </summary>
/// <remarks>
/// No method in this class performs input validation. If any parameter is <see langword="null"/>,
/// the code will throw <see cref="NullReferenceException"/>. It is the caller's responsibility
/// to validate inputs before calling any method in this class.
/// </remarks>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class WindowsRuntimeComWrappersMarshal
{
    /// <summary>
    /// Completes the activation of an authored composable Windows Runtime class instance, taking part in COM
    /// aggregation when a controlling outer object was supplied by the caller of the composition factory.
    /// </summary>
    /// <typeparam name="T">The type of the authored composable runtime class.</typeparam>
    /// <param name="instance">The newly constructed authored object.</param>
    /// <param name="defaultInterfaceIid">The IID of the default interface of the authored runtime class.</param>
    /// <param name="aggregationEntries">The interfaces the composable runtime class of <paramref name="instance"/> can expose.</param>
    /// <param name="baseInterface">The controlling outer <c>IInspectable</c> object, or <see langword="null"/> for standalone activation.</param>
    /// <param name="innerInterface">The resulting non-delegating inner <c>IInspectable</c> object (<see langword="null"/> for standalone activation).</param>
    /// <returns>The default interface pointer for <paramref name="instance"/>, to return to the composing caller.</returns>
    /// <remarks>
    /// <para>
    /// For standalone activation this is just a normal CCW creation, followed by a <c>QueryInterface</c> call for
    /// the default interface of the runtime class. In particular, <paramref name="aggregationEntries"/> is not used
    /// at all, and the resulting CCW is exactly the same one any other authored object would get, including the
    /// <c>IUnknown</c> implementation provided by the runtime.
    /// </para>
    /// <para>
    /// For composition, the returned pointer holds a reference on <paramref name="baseInterface"/>, while the
    /// non-delegating inner object returned through <paramref name="innerInterface"/> has a reference count of
    /// <c>1</c>. This matches the contract implemented by <c>winrt::impl::composable_factory</c> in C++/WinRT.
    /// </para>
    /// <para>
    /// The controlling outer object is deliberately never <c>QueryInterface</c>'d here: it is still being
    /// constructed at this point, so only the pointer that was handed to us may be used.
    /// </para>
    /// </remarks>
    public static void* CreateComposableInstanceUnsafe<T>(
        T instance,
        in Guid defaultInterfaceIid,
        ReadOnlySpan<WindowsRuntimeAggregationEntry> aggregationEntries,
        void* baseInterface,
        void** innerInterface)
        where T : class
    {
        *innerInterface = null;

        // Standalone activation: there is no controlling outer, so the instance is marshalled as usual and
        // no non-delegating inner object is produced (composing callers only ask for one when aggregating).
        if (baseInterface is null)
        {
            return (void*)WindowsRuntimeComWrappers.GetOrCreateComInterfaceForObjectExact(instance, in defaultInterfaceIid);
        }

        // Mark the instance as aggregated before its CCW can be handed out to native code, so that marshalling
        // it out to native code hands out the identity of the controlling outer object from the start.
        WindowsRuntimeAggregation.Register(instance, baseInterface);

        void* innerPtr = null;
        bool isRegistered = true;

        try
        {
            // Create the non-delegating inner object. It owns the CCW of the aggregated object, along with the
            // per-aggregate delegating vtables that CCW uses (see 'WindowsRuntimeAggregationInner').
            innerPtr = WindowsRuntimeAggregationInner.Create(instance, baseInterface, aggregationEntries);

            // From this point on the inner object owns the registration, and will undo it when it is destroyed
            isRegistered = false;

            // Query the default interface through the inner object, so the returned pointer ends up holding a
            // reference on the controlling outer, exactly like 'inner.as<I>()' does in C++/WinRT.
            IUnknownVftbl.QueryInterfaceUnsafe(innerPtr, in defaultInterfaceIid, out void* defaultInterfacePtr).Assert();

            *innerInterface = innerPtr;

            innerPtr = null;

            return defaultInterfacePtr;
        }
        finally
        {
            if (innerPtr is not null)
            {
                // Releasing the inner object also undoes the registration
                _ = IUnknownVftbl.ReleaseUnsafe(innerPtr);
            }
            else if (isRegistered)
            {
                WindowsRuntimeAggregation.Unregister(instance);
            }
        }
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer, using <c>QueryInterface</c>.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="externalComObject"/>.</param>
    /// <param name="wrapperFlags">The resulting <see cref="CreatedWrapperFlags"/> for <paramref name="externalComObject"/>.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <exception cref="NullReferenceException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is only meant to be used when creating a managed object reference around native objects. It should not
    /// be used when dealing with Windows Runtime types instantiated from C# (which includes COM aggregation scenarios too).
    /// </para>
    /// <para>
    /// This method should only be used to create <see cref="WindowsRuntimeObjectReference"/> in projection scenarios.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference CreateObjectReference(void* externalComObject, in Guid iid, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReference(
            externalComObject: externalComObject,
            iid: in iid,
            marshalingType: CreateObjectReferenceMarshalingType.Unknown);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }

    /// <inheritdoc cref="CreateObjectReference(void*, in Guid, out CreatedWrapperFlags)"/>
    /// <param name="marshalingType">The <see cref="CreateObjectReferenceMarshalingType"/> value available in metadata for the type being marshalled.</param>
    public static WindowsRuntimeObjectReference CreateObjectReference(
        void* externalComObject,
        in Guid iid,
        CreateObjectReferenceMarshalingType marshalingType,
        out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReference(
            externalComObject: externalComObject,
            iid: in iid,
            marshalingType: marshalingType);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }

    /// <summary>
    /// Initializes a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="externalComObject"/>.</param>
    /// <param name="wrapperFlags">The resulting <see cref="CreatedWrapperFlags"/> for <paramref name="externalComObject"/>.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <exception cref="NullReferenceException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is only meant to be used when creating a managed object reference around native objects. It should not
    /// be used when dealing with Windows Runtime types instantiated from C# (which includes COM aggregation scenarios too).
    /// </para>
    /// <para>
    /// Unlike <see cref="CreateObjectReferenceValue(void*, in Guid)"/>, this method assumes <paramref name="externalComObject"/> is exactly
    /// the right interface pointer for <paramref name="iid"/>, and will therefore skip doing a <c>QueryInterface</c> call on it.
    /// </para>
    /// <para>
    /// This method should only be used to create <see cref="WindowsRuntimeObjectReference"/> in projection scenarios.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference CreateObjectReferenceUnsafe(void* externalComObject, in Guid iid, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReferenceUnsafe(
            externalComObject: externalComObject,
            iid: in iid,
            marshalingType: CreateObjectReferenceMarshalingType.Unknown);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }

    /// <inheritdoc cref="CreateObjectReferenceUnsafe(void*, in Guid, out CreatedWrapperFlags)"/>
    /// <param name="marshalingType">The <see cref="CreateObjectReferenceMarshalingType"/> value available in metadata for the type being marshalled.</param>
    public static WindowsRuntimeObjectReference CreateObjectReferenceUnsafe(
        void* externalComObject,
        in Guid iid,
        CreateObjectReferenceMarshalingType marshalingType,
        out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReferenceUnsafe(
            externalComObject: externalComObject,
            iid: in iid,
            marshalingType: marshalingType);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReferenceValue"/> value for a given COM pointer, using <c>QueryInterface</c>.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="externalComObject"/>.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <exception cref="NullReferenceException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method should only be used to create <see cref="WindowsRuntimeObjectReferenceValue"/> in projection scenarios.
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue CreateObjectReferenceValue(void* externalComObject, in Guid iid)
    {
        // Do a 'QueryInterface' to actually get the interface pointer we're looking for. We don't need
        // an explicit 'null' check: the 'QueryInterface' call will trigger it if the pointer is 'null'.
        IUnknownVftbl.QueryInterfaceUnsafe(externalComObject, in iid, out void* interfacePtr).Assert();

        return new(interfacePtr);
    }

    /// <summary>
    /// Marshals a given object as a COM pointer that can be passed to native code through the Windows Runtime ABI.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <param name="flags">Flags used to configure the generated interface.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method exposes the functionality from <see cref="ComWrappers.GetOrCreateComInterfaceForObject"/> using the
    /// built-in <see cref="ComWrappers"/> implementation in CsWinRT. This method is primarily meant to be used by
    /// implementations of <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/>. Specifically, derived attributes
    /// can override <see cref="WindowsRuntimeComWrappersMarshallerAttribute.CreateObject"/> and call this method from
    /// there, so that the most optimal <see cref="CreateComInterfaceFlags"/> value can be used for the object type.
    /// </para>
    /// <para>
    /// This method is not meant to be used as a general marshalling method for managed objects, and using it in that
    /// manner will not result in correct behavior. Use <see cref="WindowsRuntimeObjectMarshaller"/> instead.
    /// </para>
    /// <para>
    /// If a COM representation was previously created for the specified <paramref name="instance" /> using the built-in <see cref="ComWrappers"/>
    /// implementation in CsWinRT, the previously created COM interface will be returned. If not, a new one will be created.
    /// </para>
    /// </remarks>
    /// <seealso cref="ComWrappers.GetOrCreateComInterfaceForObject"/>"/>
    public static void* GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(instance, flags);
    }

    /// <summary>
    /// Checks whether a pointer to a COM object is actually a reference to a CCW produced for a managed object that was marshalled to native code.
    /// </summary>
    /// <param name="externalComObject">The external COM object to check.</param>
    /// <returns>Whether <paramref name="externalComObject"/> refers to a CCW for a managed object, rather than a native COM object.</returns>
    /// <remarks>
    /// <para>
    /// This method is the same as <see cref="WindowsRuntimeMarshal.IsReferenceToManagedObject"/>, but without performing a
    /// <see langword="null"/> check on <paramref name="externalComObject"/>. Callers should validate the input pointers.
    /// </para>
    /// <para>
    /// Ordinary CCW vtables produced by CsWinRT all use the <c>IUnknown</c> implementation provided by the runtime (see
    /// <see cref="IUnknownImpl"/>), which is what the fast path below checks for. The only other CCW vtables CsWinRT ever
    /// produces are the per-aggregate copies used by an authored object taking part in COM aggregation, whose
    /// <c>IUnknown</c> entries delegate to the controlling outer object (see
    /// <see cref="WindowsRuntimeAggregationIInspectableImpl"/>). Those are still valid
    /// <see cref="ComWrappers.ComInterfaceDispatch"/> pointers, so they have to be recognized here as well, or the
    /// managed object behind them would not be resolved when they are marshalled back into managed code.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReferenceToManagedObjectUnsafe(void* externalComObject)
    {
        IUnknownVftbl* unknownVftbl = (IUnknownVftbl*)*(void***)externalComObject;
        IUnknownVftbl* runtimeVftbl = (IUnknownVftbl*)IUnknownImpl.Vtable;

        return unknownVftbl->QueryInterface == runtimeVftbl->QueryInterface
            || IsReferenceToAggregatedManagedObjectUnsafe(externalComObject);
    }

    /// <summary>
    /// Checks whether a given COM object is one of the per-aggregate delegating interface pointers CsWinRT produces.
    /// </summary>
    /// <param name="externalComObject">The external COM object to check.</param>
    /// <returns>Whether <paramref name="externalComObject"/> delegates its <c>IUnknown</c> methods when aggregated.</returns>
    /// <remarks>
    /// This is kept out of line so that the (overwhelmingly more common) check for the <c>IUnknown</c> implementation
    /// provided by the runtime stays as small as possible, and so that the static constructor check for
    /// <see cref="WindowsRuntimeAggregationIInspectableImpl"/> never shows up on that fast path.
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsReferenceToAggregatedManagedObjectUnsafe(void* externalComObject)
    {
        return WindowsRuntimeAggregation.HasAggregatedInstances
            && WindowsRuntimeAggregationIInspectableImpl.IsDelegatingInterfacePointer(externalComObject);
    }

    /// <summary>
    /// Attempts to extract a <see cref="WindowsRuntimeObjectReference"/> from the specified object.
    /// </summary>
    /// <param name="value">The object to attempt to unwrap.</param>
    /// <param name="objectReference">The unwrapped <see cref="WindowsRuntimeObjectReference"/> object, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="objectReference"/> was successfully unwrapped.</returns>
    /// <remarks>
    /// This method supports unwrapping objects that are either:
    /// <list type="bullet">
    ///   <item>A <see cref="WindowsRuntimeObject"/> with a native object reference that can be unwrapped.</item>
    ///   <item>
    ///     A <see cref="Delegate"/> whose target is a <see cref="WindowsRuntimeObjectReference"/>. Such instances
    ///     are created by the generated projections, for all projected Windows runtime delegate types.
    ///   </item>
    /// </list>
    /// If the object does not meet these criteria, this method will just return <see langword="null"/>.
    /// </remarks>
    public static bool TryUnwrapObjectReference(
        [NotNullWhen(true)] object? value,
        [NotNullWhen(true)] out WindowsRuntimeObjectReference? objectReference)
    {
        switch (value)
        {
            // If 'value' is a 'WindowsRuntimeObject' that can be unwrapped, return the wrapped object reference
            case WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject:
                objectReference = windowsRuntimeObject.NativeObjectReference;
                return true;

            // If 'value' is a marshalled delegate, return the target object reference directly
            case Delegate { Target: WindowsRuntimeObjectReference targetObjectReference }:
                objectReference = targetObjectReference;
                return true;

            // Otherwise, we can't unwrap the value at all
            default:
                objectReference = null;
                return false;
        }
    }

    /// <summary>
    /// Unwraps the <see cref="WindowsRuntimeObjectReference"/> from the specified <see cref="WindowsRuntimeObject"/>
    /// instance and returns it directly.
    /// </summary>
    /// <param name="value">The <see cref="WindowsRuntimeObject"/> instance to unwrap.</param>
    /// <returns>The <see cref="WindowsRuntimeObjectReference"/> wrapping the native object from <paramref name="value"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method does not validate whether <paramref name="value"/> can actually be unwrapped (i.e. whether
    /// <see cref="WindowsRuntimeObject.HasUnwrappableNativeObjectReference"/> is <see langword="true"/>). It is
    /// the caller's responsibility to ensure that the object is in a valid state for unwrapping.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WindowsRuntimeObjectReference UnwrapObjectReferenceUnsafe(WindowsRuntimeObject value)
    {
        return value.NativeObjectReference;
    }
}
