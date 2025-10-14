// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

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
    /// <exception cref="NullReferenceException">Thrown if <paramref name="externalComObject"/> is <see langword="null"/>.</exception>
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
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.InitializeObjectReferenceUnsafe(externalComObject, in iid);

        wrapperFlags = objectReference.GetReferenceTrackerPtrUnsafe() is null ? CreatedWrapperFlags.None : CreatedWrapperFlags.TrackerObject;

        return objectReference;
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
}
