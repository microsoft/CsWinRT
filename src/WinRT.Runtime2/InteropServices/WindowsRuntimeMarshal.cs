// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8909

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level marshalling functionality for Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeMarshal
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer, using <c>QueryInterface</c>.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <param name="iid">The IID that represents the interface implemented by <paramref name="externalComObject"/>.</param>
    /// <param name="wrapperFlags">The resulting <see cref="CreatedWrapperFlags"/> for <paramref name="externalComObject"/>.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
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
        ArgumentNullException.ThrowIfNull(externalComObject);

        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReference(externalComObject, in iid);

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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is only meant to be used when creating a managed object reference around native objects. It should not
    /// be used when dealing with Windows Runtime types instantiated from C# (which includes COM aggregation scenarios too).
    /// </para>
    /// <para>
    /// Unlike <see cref="CreateObjectReference"/>, this method assumes <paramref name="externalComObject"/> is exactly the
    /// right interface pointer for <paramref name="iid"/>, and will therefore skip doing a <c>QueryInterface</c> call on it.
    /// </para>
    /// <para>
    /// This method should only be used to create <see cref="WindowsRuntimeObjectReference"/> in projection scenarios.
    /// </para>
    /// </remarks>
    public static WindowsRuntimeObjectReference CreateObjectReferenceUnsafe(void* externalComObject, in Guid iid, out CreatedWrapperFlags wrapperFlags)
    {
        ArgumentNullException.ThrowIfNull(externalComObject);

        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReferenceUnsafe(externalComObject, in iid);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
    }

    /// <summary>
    /// Checks whether a pointer to a COM object is actually a reference to a CCW produced for a managed object that was marshalled to native code.
    /// </summary>
    /// <param name="externalComObject">The external COM object to check.</param>
    /// <returns>Whether <paramref name="externalComObject"/> refers to a CCW for a managed object, rather than a native COM object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReferenceToManagedObject(void* externalComObject)
    {
        ArgumentNullException.ThrowIfNull(externalComObject);

        IUnknownVftbl* unknownVftbl = (IUnknownVftbl*)*(void***)externalComObject;
        IUnknownVftbl* runtimeVftbl = (IUnknownVftbl*)IUnknownImpl.Vtable;

        return
            unknownVftbl->QueryInterface == runtimeVftbl->QueryInterface &&
            unknownVftbl->AddRef == runtimeVftbl->AddRef &&
            unknownVftbl->Release == runtimeVftbl->Release;
    }

    /// <summary>
    /// Tries to retrieve a managed object from a pointer to a COM object, if it is actually a reference to a CCW that was marshalled to native code.
    /// </summary>
    /// <param name="externalComObject">The external COM object to try to get a managed object from.</param>
    /// <param name="result">The resulting managed object, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="externalComObject"/> was a reference to a managed object, and <paramref name="result"/> could be retrieved.</returns>
    public static bool TryGetManagedObject(void* externalComObject, [NotNullWhen(true)] out object? result)
    {
        // If the input pointer is a reference to a managed object, we can resolve the original managed object
        if (externalComObject != null && IsReferenceToManagedObject(externalComObject))
        {
            result = ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)externalComObject);

            return true;
        }

        result = null;

        return false;
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
    /// Marshals a given object as a COM pointer that can be passed to native code through the Windows Runtime ABI.
    /// </summary>
    /// <param name="instance">The managed object to expose outside the .NET runtime.</param>
    /// <param name="flags">Flags used to configure the generated interface.</param>
    /// <returns>The generated COM interface that can be passed outside the .NET runtime.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
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
    /// manner will not result in correct behavior. Use <see cref="Marshalling.WindowsRuntimeObjectMarshaller"/> instead.
    /// </para>
    /// <para>
    /// If a COM representation was previously created for the specified <paramref name="instance" /> using the built-in <see cref="ComWrappers"/>
    /// implementation in CsWinRT, the previously created COM interface will be returned. If not, a new one will be created.
    /// </para>
    /// </remarks>
    /// <seealso cref="ComWrappers.GetOrCreateComInterfaceForObject"/>"/>
    public static void* GetOrCreateComInterfaceForObject(object instance, CreateComInterfaceFlags flags)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(instance, flags);
    }
}
