// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Describes a single interface an authored composable Windows Runtime class can expose while it is
/// taking part in COM aggregation.
/// </summary>
/// <remarks>
/// <para>
/// One entry is emitted by CsWinRT for each Windows Runtime interface in the transitive closure of the
/// interfaces implemented by a composable (unsealed) authored runtime class. The entries are handed to
/// <see cref="WindowsRuntimeComWrappersMarshal.CreateComposableInstanceUnsafe"/> by the generated
/// composition factory, and are only ever used when an instance is actually aggregated.
/// </para>
/// <para>
/// <see cref="VtableSize"/> is the size in bytes of the CCW vtable referenced by <see cref="Vtable"/>
/// (i.e. <c>sizeof(IFooVftbl)</c> for the generated vtable struct). It is required because the aggregation
/// support copies the vtable into a per-aggregate native allocation, replacing only its <c>IInspectable</c>
/// (and therefore <c>IUnknown</c>) entries with ones delegating to the controlling outer object.
/// </para>
/// </remarks>
[WindowsRuntimeImplementationOnlyMember]
public readonly struct WindowsRuntimeAggregationEntry
{
    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeAggregationEntry"/> value with the specified parameters.
    /// </summary>
    /// <param name="iid">The IID of the interface.</param>
    /// <param name="vtable">The CCW vtable for the interface.</param>
    /// <param name="vtableSize">The size in bytes of the vtable referenced by <paramref name="vtable"/>.</param>
    public WindowsRuntimeAggregationEntry(in Guid iid, nint vtable, int vtableSize)
    {
        IID = iid;
        Vtable = vtable;
        VtableSize = vtableSize;
    }

    /// <summary>
    /// Gets the IID of the interface.
    /// </summary>
    internal Guid IID { get; }

    /// <summary>
    /// Gets the CCW vtable for the interface.
    /// </summary>
    internal nint Vtable { get; }

    /// <summary>
    /// Gets the size in bytes of the vtable referenced by <see cref="Vtable"/>.
    /// </summary>
    internal int VtableSize { get; }
}
