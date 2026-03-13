// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A marshaller with some utility methods that directly wrap <see cref="ComWrappers"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeComWrappersMarshal
{
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
}