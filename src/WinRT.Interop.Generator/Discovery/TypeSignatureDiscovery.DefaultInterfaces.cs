// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Records an actual receiver without changing primary or existing supplemental admission.
    /// </summary>
    /// <param name="signature">The observed closed receiver signature.</param>
    /// <param name="depth">Its minimum sighting depth.</param>
    private void RecordDefaultReceiver(TypeSignature signature, int depth)
    {
        // A truncated hierarchy has no selected bodies, but its receiver still needs a deferred diagnostic
        // Definitions that are never observed must not report discovery warnings from metadata indexing alone.
        if (!signature.TryResolve(_module.RuntimeContext, out TypeDefinition? receiver))
        {
            return;
        }

        if (_dispatchIndex.GetDefaultBodies(receiver).Count == 0 &&
            _dispatchIndex.GetDefaultHierarchyLimit(receiver) == MethodDispatchIndex.DefaultHierarchyLimit.None)
        {
            return;
        }

        // Preserve first-sighting order, but let a shallower path expose descendants beyond the earlier depth cutoff
        if (_defaultReceiverContexts.TryGetValue(signature, out DefaultReceiverObservation? observation))
        {
            if (observation.Depth <= depth)
            {
                return;
            }

            observation.Depth = depth;
        }
        else
        {
            observation = new DefaultReceiverObservation(
                signature: signature,
                definition: receiver,
                depth: depth);

            _defaultReceiverContexts.Add(signature, observation);
            _defaultReceiverObservations.Add(observation);
        }

        if (_isSupplemental)
        {
            // A late body can rediscover an origin's receiver; unlike imported entries, that actual
            // sighting remains eligible because origin-local body coverage is not shared across the barrier.
            _pendingDefaultReceivers.Enqueue(observation);
        }
    }

    /// <summary>
    /// Expands selected defaults to a fixpoint using only the remaining supplemental allowance.
    /// </summary>
    /// <returns>The additional signatures discovered in selected bodies and their descendants.</returns>
    private IEnumerable<TResult> EnumerateDefaultInterfaceMembers()
    {
        // Tag contexts found by these bodies and their callees for the later cross-module stage
        _isDefaultInterfaceTraversal = true;

        // Initial supplemental work drains before this phase, including ordinary callees of dispatch bodies
        // Later batches handle receivers discovered by defaults without granting them another allowance.
        while (_pendingDefaultReceivers.Count > 0 &&
            !_transitiveTypeLimitReported)
        {
            while (_pendingDefaultReceivers.TryDequeue(out DefaultReceiverObservation? observation))
            {
                _token.ThrowIfCancellationRequested();

                if (_transitiveTypeLimitReported)
                {
                    break;
                }

                // The same mutable sighting may be queued repeatedly; only an improved minimum depth warrants replay
                if (observation.ScheduledDepth <= observation.Depth)
                {
                    continue;
                }

                observation.ScheduledDepth = observation.Depth;

                MethodDispatchIndex.DefaultHierarchyLimit limit = _dispatchIndex.GetDefaultHierarchyLimit(observation.Definition);

                // Reuse the shared one-time diagnostics for metadata bounds, without a root exemption
                if (limit == MethodDispatchIndex.DefaultHierarchyLimit.Depth &&
                    !_recursionLimitReported)
                {
                    _recursionLimitReported = true;

                    WellKnownInteropExceptions.GenericTypeDiscoveryRecursionLimitExceededWarning(
                        observation.Signature,
                        _module,
                        MaxDiscoveryDepth,
                        _isSupplemental)
                        .LogOrThrow(_treatWarningsAsErrors);
                }
                else if (limit == MethodDispatchIndex.DefaultHierarchyLimit.Complexity)
                {
                    _ = IsComplexityWithinLimit(MaxSignatureComplexity + 1, 1);
                }

                GenericContext context = new(
                    type: observation.Signature as GenericInstanceTypeSignature,
                    method: null);

                // Bodies are members one level below their receiver and never inherit the root exemption
                int depth = Math.Max(1, observation.Depth + 1);

                foreach (MethodDispatchIndex.DefaultBody body in _dispatchIndex.GetDefaultBodies(observation.Definition))
                {
                    _token.ThrowIfCancellationRequested();

                    if (_transitiveTypeLimitReported)
                    {
                        break;
                    }

                    if (IsSignatureWithinComplexityLimit(body.Owner, depth))
                    {
                        // Close the indexed owner template with this receiver's arguments, even though the slot is non-generic
                        EnqueueDefaultBody(body.Method, body.Owner.InstantiateGenericTypes(context), depth);
                    }
                }
            }

            // Draining selected bodies can discover more receivers for the next outer-loop batch
            foreach (TResult result in EnumeratePendingMembers())
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Queues a type-only interface body without inventing a generic method signature.
    /// </summary>
    /// <param name="method">The selected interface method.</param>
    /// <param name="owner">Its constructed declaring interface.</param>
    /// <param name="depth">The supplemental body depth.</param>
    private void EnqueueDefaultBody(
        MethodDefinition method,
        TypeSignature owner,
        int depth)
    {
        // Keep admission defensive even though the index filters signatures and policy
        // Indexing does not deserialize IL, and only available bodies can consume worklist slots.
        if (_transitiveTypeLimitReported ||
            method.Signature is not { GenericParameterCount: 0 } ||
            method.IsAbstract)
        {
            return;
        }

        if (method.CilMethodBody is null)
        {
            return;
        }

        if (method.DeclaringModule is not ModuleDefinition module ||
            !_shouldProcessModule(module))
        {
            return;
        }

        // Closed receiver arguments can exceed the bound even when the indexed owner template fits
        if (!IsSignatureWithinComplexityLimit(owner, depth))
        {
            return;
        }

        // A type scan covers bodies at 'typeDepth + 1', so its type depth must be strictly smaller than this request
        // Check both traversals to avoid spending another reservation on an already-covered owner.
        if (_primary?._coveredTypes.TryGetValue(owner, out int primaryDepth) is true &&
            primaryDepth < depth)
        {
            return;
        }

        if (_coveredTypes.TryGetValue(owner, out int coveredDepth) &&
            coveredDepth < depth)
        {
            return;
        }

        IList<TypeSignature> arguments = [.. (owner as GenericInstanceTypeSignature)?.TypeArguments ?? []];
        Dictionary<IList<TypeSignature>, int> contexts = GetMethodContexts(_visitedDefaultBodies, method);
        bool visited = contexts.TryGetValue(arguments, out int priorDepth);

        // These bodies have owner arguments but no 'MethodSpec'; keep their keys separate from generic calls
        // A shallower sighting reuses its reservation, just like other supplemental member contexts.
        if (visited && priorDepth <= depth)
        {
            return;
        }

        if (!IsMethodWithinDepthLimit(method, depth))
        {
            return;
        }

        // Only first admission consumes a slot; a shallower revisit keeps its original reservation
        if (!visited && !TryReserveTransitiveInstantiation(depth))
        {
            return;
        }

        contexts[arguments] = depth;

        _pendingMembers.Enqueue(new PendingMember(
            type: null,
            method: method,
            context: new GenericContext(
                type: owner as GenericInstanceTypeSignature,
                method: null),
            depth: depth,
            arguments: arguments));
    }

    /// <summary>
    /// An ordered receiver sighting whose minimum depth can improve without changing its place.
    /// </summary>
    /// <param name="signature">The actual closed receiver.</param>
    /// <param name="definition">The receiver definition owning the selected default set.</param>
    /// <param name="depth">The first sighting depth.</param>
    private sealed class DefaultReceiverObservation(
        TypeSignature signature,
        TypeDefinition definition,
        int depth)
    {
        /// <summary>The actual closed receiver.</summary>
        public TypeSignature Signature { get; } = signature;

        /// <summary>The receiver definition.</summary>
        public TypeDefinition Definition { get; } = definition;

        /// <summary>The minimum observed depth.</summary>
        public int Depth { get; set; } = depth;

        /// <summary>The minimum depth already considered for scheduling.</summary>
        public int ScheduledDepth { get; set; } = int.MaxValue;
    }
}
