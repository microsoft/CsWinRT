// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type holding infor on a computed vtable for a given managed type.
/// </summary>
internal sealed unsafe class WindowsRuntimeVtableInfo
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeVtableInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="vtableEntries">The computed vtable entries, allocated in native memory.</param>
    /// <param name="count">The number of elements in the vtable entries.</param>
    private WindowsRuntimeVtableInfo(ComInterfaceEntry* vtableEntries, int count)
    {
        VtableEntries = vtableEntries;
        Count = count;
    }

    /// <summary>
    /// Gets the computed vtable entries, allocated in native memory.
    /// </summary>
    public ComInterfaceEntry* VtableEntries { get; }

    /// <summary>
    /// Gets tt\he number of elements in <see cref="VtableEntries"/>.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeVtableInfo"/> instance with the computed info for a given type.
    /// </summary>
    /// <param name="info">The type info to compute the vtable for.</param>
    /// <returns>A new <see cref="WindowsRuntimeVtableInfo"/> instance for <paramref name="info"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be called on types that do support being marshalled from a managed CCW.
    /// </para>
    /// <para>
    /// It is responsibility of callers to avoid calling this method concurrently on the same type.
    /// Doing so would result in multiple native vtables being allocated and kept alive indefinitely.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance could be resolved.</exception>
    public static WindowsRuntimeVtableInfo CreateUnsafe(WindowsRuntimeMarshallingInfo info)
    {
        // Get the '[WindowsRuntimeComWrappersMarshaller]' attribute from the type, to get custom vtable entries.
        // This should always find the attribute. The attribute not being present would mean that somehow
        // our 'ComWrappers' instance tried creating a CCW for a type that had an associated marshalling
        // info, but not a vtable provider. That is, it could only mean the type is a projected type,
        // which should never hit this path, or that the generator somehow didn't generate the attribute.
        // That would be a bug, and it should never happen in practice (and we'd want to crash if it did).
        WindowsRuntimeComWrappersMarshallerAttribute comWrappersMarshaller = info.GetComWrappersMarshaller();

        using PooledComInterfaceEntryBufferWriter writer = new();

        // Delegate to the vtable provider to produce the first vtable entries.
        // Any additional "built-in" vtable entries are appended at the end.
        comWrappersMarshaller.ComputeVtables(writer);

        // Check for additional user defined interfaces (so we know which ones we should omit)
        CheckForUserImplementedInterfaces(
            publicType: info.PublicType,
            vtableEntries: writer.GetSpan(),
            out bool hasUserImplementedIMarshalInterface,
            out bool hasUserImplementedICustomPropertyProviderInterface);

        // Always add 'IStringable' to all types
        writer.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IStringable,
            Vtable = IStringableImpl.AbiToProjectionVftablePtr
        }]);

        // There are two scenarios where we want to support 'ICustomPropertyProvider':
        //   - The user is explicitly implementing the interface on their type
        //   - The user is using '[GeneratedBindableCustomProperty]', which uses our internal CCW
        if (!hasUserImplementedICustomPropertyProviderInterface)
        {
            writer.Write([new ComInterfaceEntry
            {
                IID = WellKnownInterfaceIds.IID_ICustomPropertyProvider,
                Vtable = (nint)null // TODO
            }]);
        }

        writer.Write([new ComInterfaceEntry
        {
            IID = WellKnownInterfaceIds.IID_IWeakReferenceSource,
            Vtable = (nint)null // TODO
        }]);

        // Add 'IMarhal' implemented using the free threaded marshaler to
        // all CCWs, unless the type has explictly implemented the interface
        if (!hasUserImplementedIMarshalInterface)
        {
            writer.Write([new ComInterfaceEntry
            {
                IID = WellKnownInterfaceIds.IID_IMarshal,
                Vtable = IMarshalImpl.AbiToProjectionVftablePtr
            }]);
        }

        // Add 'IAgileObject', 'IInspectable' and 'IUnknown' to all CCWs, always at the end.
        // Note that 'IUnknown' in particular is required to be the very last vtable entry
        // that we add, so that 'ComWrappers' can easily exclude it if the flags asks for it.
        // For 'IAgileObject', we reuse the 'IUnknown' vtable, as it's just a marker interface.
        writer.Write(
        [
            new ComInterfaceEntry
            {
                IID = WellKnownInterfaceIds.IID_IAgileObject,
                Vtable = IUnknownImpl.AbiToProjectionVftablePtr
            },
            new ComInterfaceEntry
            {
                IID = WellKnownInterfaceIds.IID_IInspectable,
                Vtable = IInspectableImpl.AbiToProjectionVftablePtr
            },
            new ComInterfaceEntry
            {
                IID = WellKnownInterfaceIds.IID_IUnknown,
                Vtable = IUnknownImpl.AbiToProjectionVftablePtr
            }
        ]);

        // Allocate the vtable entries, and associate this memory to the public type in use
        ComInterfaceEntry* entries = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(
            type: info.PublicType,
            size: sizeof(ComInterfaceEntry) * writer.Count);

        // Copy the vtable entries to the allocated memory
        writer.WrittenSpan.CopyTo(new Span<ComInterfaceEntry>(entries, writer.Count));

        return new(entries, writer.Count);
    }

    /// <summary>
    /// Checks for some well known interfaces that might have been explicitly implemented.
    /// </summary>
    /// <param name="publicType">The public type the vtable is being constructed for.</param>
    /// <param name="vtableEntries">The vtable entries currently available.</param>
    /// <param name="hasUserImplementedIMarshalInterface">Whether <c>IMarshal</c> is explicitly implemented.</param>
    /// <param name="hasUserImplementedICustomPropertyProviderInterface">Whether <c>ICustomPropertyProvider</c> is explicitly implemented.</param>
    private static void CheckForUserImplementedInterfaces(
        Type publicType,
        ReadOnlySpan<ComInterfaceEntry> vtableEntries,
        out bool hasUserImplementedIMarshalInterface,
        out bool hasUserImplementedICustomPropertyProviderInterface)
    {
        // We only need to check for 'IMarshal' and 'ICustomPropertyProvider' on class types
        if (publicType.IsClass)
        {
            // Manual helper to save binary size (no LINQ, no lambdas) and get better performance
            static bool GetHasCustomIMarshalInterface(ReadOnlySpan<ComInterfaceEntry> vtableEntries)
            {
                foreach (ref readonly ComInterfaceEntry entry in vtableEntries)
                {
                    if (entry.IID == WellKnownInterfaceIds.IID_IMarshal)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Same as above, for 'ICustomPropertyProvider' (separate method for a small perf boost).
            // The method is very tiny, so the code duplication is not really a concern here.
            static bool GetHasICustomPropertyProviderInterface(ReadOnlySpan<ComInterfaceEntry> vtableEntries)
            {
                foreach (ref readonly ComInterfaceEntry entry in vtableEntries)
                {
                    if (entry.IID == WellKnownInterfaceIds.IID_ICustomPropertyProvider)
                    {
                        return true;
                    }
                }

                return false;
            }

            hasUserImplementedIMarshalInterface = GetHasCustomIMarshalInterface(vtableEntries);
            hasUserImplementedICustomPropertyProviderInterface = GetHasICustomPropertyProviderInterface(vtableEntries);
        }
        else
        {
            // Otherwise we always consider these two interfaces as not implemented
            hasUserImplementedIMarshalInterface = false;
            hasUserImplementedICustomPropertyProviderInterface = false;
        }
    }
}
