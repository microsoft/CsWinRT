// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// Collects receiver contexts and unresolved dispatch demands during a parallel discovery pass.
/// </summary>
internal sealed class DispatchContextRegistry
{
    /// <summary>
    /// The modules whose transitive instantiation limit warning was already reported. This set is shared by
    /// linked registries, so each module is only reported once across all of them.
    /// </summary>
    private readonly ConcurrentDictionary<ModuleDefinition, byte> _transitiveLimitReportedModules;

    /// <summary>
    /// Creates a new <see cref="DispatchContextRegistry"/> instance.
    /// </summary>
    /// <param name="transitiveLimitReportedModules">The shared set of modules whose transitive instantiation limit warning was already reported.</param>
    private DispatchContextRegistry(ConcurrentDictionary<ModuleDefinition, byte> transitiveLimitReportedModules)
    {
        _transitiveLimitReportedModules = transitiveLimitReportedModules;
    }

    /// <summary>
    /// Creates a pair of linked <see cref="DispatchContextRegistry"/> instances.
    /// </summary>
    /// <returns>The two linked registries.</returns>
    public static (DispatchContextRegistry First, DispatchContextRegistry Second) CreateLinkedPair()
    {
        ConcurrentDictionary<ModuleDefinition, byte> transitiveLimitReportedModules = [];

        return (new(transitiveLimitReportedModules), new(transitiveLimitReportedModules));
    }

    /// <summary>The actual receiver contexts, read only after the parallel publishing barrier.</summary>
    public ConcurrentBag<Receiver> Receivers { get; } = [];

    /// <summary>The dispatch bindings that may need receiver parameters from another module.</summary>
    public ConcurrentBag<Demand> Demands { get; } = [];

    /// <summary>Dispatch-capable body contexts already expanded by the module enumerations.</summary>
    public ConcurrentBag<ExpandedBody> ExpandedBodies { get; } = [];

    /// <summary>
    /// Marks the transitive instantiation limit warning as reported for a module.
    /// </summary>
    /// <param name="module">The module that reached the limit.</param>
    /// <returns>Whether this is the first report for the module (and the warning should be emitted).</returns>
    public bool TryMarkTransitiveLimitReported(ModuleDefinition module)
    {
        return _transitiveLimitReportedModules.TryAdd(module, 0);
    }

    /// <summary>
    /// An observed receiver context with its traversal provenance.
    /// </summary>
    /// <param name="Signature">The observed constructed receiver.</param>
    /// <param name="Depth">Its traversal depth.</param>
    /// <param name="Origin">The owning module's stable ordinal.</param>
    /// <param name="Sequence">The stable publication sequence within that enumeration.</param>
    /// <param name="Phase">The discovery phase that published this observation.</param>
    public sealed record Receiver(
        TypeSignature Signature,
        int Depth,
        int Origin,
        int Sequence,
        DiscoveryPhase Phase);

    /// <summary>
    /// A dispatch binding retained until additional receiver contexts become available.
    /// </summary>
    /// <param name="Target">The indexed metadata relationship.</param>
    /// <param name="DeclarationType">The observed declaring context.</param>
    /// <param name="Arguments">The observed method type arguments.</param>
    /// <param name="Depth">The expansion depth in the owning enumeration.</param>
    /// <param name="Origin">The owning module's stable ordinal.</param>
    /// <param name="Sequence">The stable publication sequence within that enumeration.</param>
    /// <param name="Phase">The discovery phase that published this observation.</param>
    public sealed record Demand(
        MethodDispatchIndex.Target Target,
        TypeSignature DeclarationType,
        GenericInstanceMethodSignature Arguments,
        int Depth,
        int Origin,
        int Sequence,
        DiscoveryPhase Phase);

    /// <summary>
    /// A dispatch-capable method context already queued and expanded before the publishing barrier.
    /// </summary>
    /// <param name="Method">The resolved body definition.</param>
    /// <param name="Arguments">The combined declaring-type and method arguments.</param>
    /// <param name="Depth">The depth at which the body was expanded.</param>
    /// <param name="Phase">The discovery phase that published this observation.</param>
    public sealed record ExpandedBody(
        MethodDefinition Method,
        IList<TypeSignature> Arguments,
        int Depth,
        DiscoveryPhase Phase);

    /// <summary>The ordered publication phases used by supplemental cross-module joins.</summary>
    public enum DiscoveryPhase
    {
        /// <summary>Generic dispatch and shorter-path replay.</summary>
        GenericDispatch = 0,

        /// <summary>Receiver-driven instance default bodies.</summary>
        DefaultInterfaces = 1,

        /// <summary>Additional constrained static interface calls.</summary>
        StaticDispatch = 2
    }
}
