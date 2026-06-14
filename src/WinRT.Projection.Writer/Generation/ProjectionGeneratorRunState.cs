// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Generation;

/// <summary>
/// Cross-work-item shared state for a single <see cref="ProjectionGenerator.Run"/> invocation.
/// Bundles the concurrent collections each per-namespace work item contributes to, the
/// pre-discovered component-activation lookup, and the "did any work item write a projection
/// file?" flag (tracked via <see cref="Interlocked"/> so concurrent writes from any work item
/// produce a deterministic post-loop value).
/// </summary>
internal sealed class ProjectionGeneratorRunState
{
    /// <summary>
    /// Gets the activatable runtime classes discovered during the (sequential) discovery phase.
    /// Read-only after construction; safe for concurrent reads from any work item.
    /// </summary>
    public HashSet<TypeDefinition> ComponentActivatable { get; }

    /// <summary>
    /// Gets the activatable runtime classes grouped by source <c>.winmd</c> module name.
    /// Read-only after construction; consumed by <see cref="WorkItems.ComponentModuleWorkItem"/>.
    /// </summary>
    public Dictionary<string, HashSet<TypeDefinition>> ComponentByModule { get; }

    /// <summary>
    /// Gets the (projected-type-name -> default-interface-name) map populated by namespace
    /// work items via <see cref="Factories.MetadataAttributeFactory.AddDefaultInterfaceEntry"/>.
    /// </summary>
    public ConcurrentDictionary<string, string> DefaultInterfaceEntries { get; } = [];

    /// <summary>
    /// Gets the (interface-name, parent-class-name) pairs populated by namespace work items via
    /// <see cref="Factories.MetadataAttributeFactory.AddExclusiveToInterfaceEntries"/>.
    /// </summary>
    public ConcurrentBag<KeyValuePair<string, string>> ExclusiveToInterfaceEntries { get; } = [];

    /// <summary>
    /// Gets the (projected-type-name -> CCW-type-name) metadata map populated by namespace work
    /// items via <see cref="Factories.ComponentFactory.AddMetadataTypeEntry"/>.
    /// </summary>
    public ConcurrentDictionary<string, string> AuthoredTypeNameToMetadataMap { get; } = [];

    /// <summary>
    /// Tracked via <see cref="Interlocked"/> so any number of work items can mark "I wrote a
    /// projection file" concurrently without a torn read. Use <see cref="ProjectionFileWritten"/>
    /// to query (after the parallel loop completes) and <see cref="MarkProjectionFileWritten"/>
    /// from inside a work item.
    /// </summary>
    private int _projectionFileWritten;

    /// <summary>
    /// Initializes a new <see cref="ProjectionGeneratorRunState"/> with the discovered
    /// activatable-class lookups.
    /// </summary>
    /// <param name="componentActivatable">The flat set of activatable runtime classes.</param>
    /// <param name="componentByModule">The activatable classes grouped by source module name.</param>
    public ProjectionGeneratorRunState(
        HashSet<TypeDefinition> componentActivatable,
        Dictionary<string, HashSet<TypeDefinition>> componentByModule)
    {
        ComponentActivatable = componentActivatable;
        ComponentByModule = componentByModule;
    }

    /// <summary>
    /// Gets whether any work item has marked a projection file as written.
    /// Should only be queried after the parallel loop has completed.
    /// </summary>
    public bool ProjectionFileWritten => Volatile.Read(ref _projectionFileWritten) != 0;

    /// <summary>
    /// Records that the calling work item wrote a projection file.
    /// Safe to call concurrently from any number of work items.
    /// </summary>
    public void MarkProjectionFileWritten()
    {
        _ = Interlocked.Exchange(ref _projectionFileWritten, 1);
    }
}
