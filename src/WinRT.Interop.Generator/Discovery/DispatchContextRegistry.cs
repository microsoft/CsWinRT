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
    /// <summary>The actual receiver contexts, read only after the parallel publishing barrier.</summary>
    public ConcurrentBag<Receiver> Receivers { get; } = [];

    /// <summary>The dispatch bindings that may need receiver parameters from another module.</summary>
    public ConcurrentBag<Demand> Demands { get; } = [];

    /// <summary>Dispatch-capable body contexts already expanded by the module enumerations.</summary>
    public ConcurrentBag<ExpandedBody> ExpandedBodies { get; } = [];

    /// <summary>An observed receiver context with its traversal provenance.</summary>
    /// <param name="Signature">The observed constructed receiver.</param>
    /// <param name="Depth">Its traversal depth.</param>
    /// <param name="Origin">The owning module's stable ordinal.</param>
    /// <param name="Sequence">The stable publication sequence within that enumeration.</param>
    internal sealed record Receiver(TypeSignature Signature, int Depth, int Origin, int Sequence);

    /// <summary>A dispatch binding retained until additional receiver contexts become available.</summary>
    /// <param name="Target">The indexed metadata relationship.</param>
    /// <param name="DeclarationType">The observed declaring context.</param>
    /// <param name="Arguments">The observed method type arguments.</param>
    /// <param name="Depth">The expansion depth in the owning enumeration.</param>
    /// <param name="Origin">The owning module's stable ordinal.</param>
    /// <param name="Sequence">The stable publication sequence within that enumeration.</param>
    internal sealed record Demand(
        MethodDispatchIndex.Target Target,
        TypeSignature DeclarationType,
        GenericInstanceMethodSignature Arguments,
        int Depth,
        int Origin,
        int Sequence);

    /// <summary>A dispatch-capable method context already queued and expanded before the publishing barrier.</summary>
    /// <param name="Method">The resolved body definition.</param>
    /// <param name="Arguments">The combined declaring-type and method arguments.</param>
    /// <param name="Depth">The depth at which the body was expanded.</param>
    internal sealed record ExpandedBody(MethodDefinition Method, IList<TypeSignature> Arguments, int Depth);
}
