// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <see cref="ComWrappers"/> instance used to create the CCW of an authored Windows Runtime object
/// taking part in COM aggregation.
/// </summary>
/// <remarks>
/// <para>
/// Aggregated objects deliberately do not share the CCW created by <see cref="WindowsRuntimeComWrappers"/>:
/// the interfaces they expose have to delegate their <c>IUnknown</c> methods to the controlling outer object,
/// and installing that behavior on the shared CCW would change it for every other (non aggregated) instance
/// of the same type as well. Using a dedicated <see cref="ComWrappers"/> instance keeps the per-aggregate
/// vtables completely separate, so ordinary CCW-s always keep the <c>IUnknown</c> implementation provided by
/// the runtime, which is implemented in native code and safe to call during a garbage collection.
/// </para>
/// <para>
/// The interface entries are supplied by <see cref="WindowsRuntimeAggregationInner"/>, which owns the native
/// memory they live in (as well as the per-aggregate vtable copies they point to), and keeps it alive for as
/// long as the CCW exists.
/// </para>
/// <para>
/// This instance is only ever used to create wrappers for managed objects. Marshalling native objects into
/// managed code always goes through <see cref="WindowsRuntimeComWrappers"/>.
/// </para>
/// </remarks>
internal sealed unsafe class WindowsRuntimeAggregationComWrappers : ComWrappers
{
    /// <summary>
    /// The shared instance to use for all aggregated objects.
    /// </summary>
    private static readonly WindowsRuntimeAggregationComWrappers Instance = new();

    /// <summary>
    /// The interface entries for the wrapper currently being created on this thread.
    /// </summary>
    [ThreadStatic]
    private static ComInterfaceEntry* CurrentInterfaceEntries;

    /// <summary>
    /// The number of elements in <see cref="CurrentInterfaceEntries"/>.
    /// </summary>
    [ThreadStatic]
    private static int CurrentInterfaceEntryCount;

    /// <summary>
    /// Creates the CCW for an aggregated managed object, with the specified interface entries.
    /// </summary>
    /// <param name="instance">The aggregated managed object.</param>
    /// <param name="interfaceEntries">The interface entries to expose (owned by the caller, must outlive the CCW).</param>
    /// <param name="interfaceEntryCount">The number of elements in <paramref name="interfaceEntries"/>.</param>
    /// <returns>The <c>IUnknown</c> pointer for the resulting CCW (ownership is transferred to the caller).</returns>
    /// <remarks>
    /// The wrapper is created without <see cref="CreateComInterfaceFlags.TrackerSupport"/>: an aggregated object
    /// never participates in reference tracking on its own, as its lifetime is entirely controlled by the
    /// controlling outer object (which is the one native code, including XAML, holds on to).
    /// </remarks>
    public static void* CreateComInterfaceForObject(object instance, ComInterfaceEntry* interfaceEntries, int interfaceEntryCount)
    {
        CurrentInterfaceEntries = interfaceEntries;
        CurrentInterfaceEntryCount = interfaceEntryCount;

        try
        {
            return (void*)Instance.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);
        }
        finally
        {
            CurrentInterfaceEntries = null;
            CurrentInterfaceEntryCount = 0;
        }
    }

    /// <inheritdoc/>
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        count = CurrentInterfaceEntryCount;

        return CurrentInterfaceEntries;
    }

    /// <inheritdoc/>
    protected override object? CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        throw new NotSupportedException("This 'ComWrappers' instance is only used to create wrappers for aggregated managed objects.");
    }

    /// <inheritdoc/>
    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotSupportedException("This 'ComWrappers' instance is only used to create wrappers for aggregated managed objects.");
    }
}
