// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// Discovers type signatures from a module's metadata roots and instantiated members.
/// </summary>
/// <typeparam name="TResult">The type signatures selected by the result visitor.</typeparam>
internal sealed partial class TypeSignatureDiscovery<TResult>
    where TResult : TypeSignature
{
    /// <summary>The maximum depth at which discovered members can be queued.</summary>
    private const int MaxDiscoveryDepth = 32;

    /// <summary>The maximum signature element count for a transitive signature or method context.</summary>
    private const int MaxSignatureComplexity = 256;

    /// <summary>The shared budget for transitive type and method instantiations.</summary>
    private const int MaxTransitiveTypes = 1024;

    /// <summary>The root module, also used for resolution and diagnostics.</summary>
    private readonly ModuleDefinition _module;

    /// <summary>The visitor selecting the signatures to report.</summary>
    private readonly ITypeSignatureVisitor<IEnumerable<TResult>> _visitor;

    /// <summary>The resolution-aware comparer supplied for this invocation.</summary>
    private readonly SignatureComparer _signatureComparer;

    /// <summary>Determines whether a referenced module's members can be expanded transitively.</summary>
    private readonly Func<ModuleDefinition, bool> _shouldProcessModule;

    /// <summary>Identifies explicitly excluded modules, whose transitive static initializers are also skipped.</summary>
    private readonly Func<ModuleDefinition, bool> _isMarshallingDisabledModule;

    /// <summary>Whether discovery warnings are promoted to errors.</summary>
    private readonly bool _treatWarningsAsErrors;

    /// <summary>The cancellation token for this enumeration.</summary>
    private readonly CancellationToken _token;

    /// <summary>The invocation's complete, read-only dispatch metadata.</summary>
    private readonly MethodDispatchIndex _dispatchIndex;

    /// <summary>The write-only registry for late cross-module receiver contexts, or null in its completion pass.</summary>
    private readonly DispatchContextRegistry? _dispatchContexts;

    /// <summary>Whether this instance expands additive work instead of ordinary metadata roots.</summary>
    private readonly bool _isSupplemental;

    /// <summary>The completed primary traversal, read-only while supplemental work is running.</summary>
    private readonly TypeSignatureDiscovery<TResult>? _primary;

    /// <summary>The stable ordinal of this enumeration's root module.</summary>
    private readonly int _moduleOrdinal;

    /// <summary>The signatures already reported to the consumer.</summary>
    private readonly HashSet<TResult> _results;

    /// <summary>Explicit 'TypeSpec' roots, whose members remain eligible for a one-hop scan regardless of module policy.</summary>
    private readonly HashSet<TypeSignature> _typeSpecifications;

    /// <summary>Type contexts already considered for member expansion, distinct from reported results.</summary>
    private readonly Dictionary<TypeSignature, int> _visitedTypes;

    /// <summary>Argument contexts grouped by resolved method definition identity, not by method signature.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _visitedMethods;

    /// <summary>The FIFO worklist of type or method contexts awaiting member discovery.</summary>
    private readonly Queue<PendingMember> _pendingMembers;

    /// <summary>Actual scans recorded during traversal and consulted only by supplemental replay.</summary>
    private readonly Dictionary<TypeSignature, int> _coveredTypes;

    /// <summary>Method contexts actually scanned at their minimum observed depth.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _coveredMethods = new(ReferenceEqualityComparer.Instance);

    /// <summary>Shallower type sightings that may recover a depth-limited primary path.</summary>
    private readonly Dictionary<TypeSignature, int> _shallowerTypes;

    /// <summary>Shallower method sightings, retained in their first observation order.</summary>
    private readonly List<ShallowerMethodObservation> _shallowerMethods = [];

    /// <summary>Minimum method sighting depths, without repeated list entries for hot contexts.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, ShallowerMethodObservation>> _shallowerMethodContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Dispatch observations recorded without affecting the primary queue or budget.</summary>
    private readonly List<DispatchObservation> _dispatchObservations = [];

    /// <summary>Composed dispatch keys deduplicated before replaying the supplemental phase.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, DispatchObservation>> _dispatchObservationContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Observed dispatch contexts, deduplicated by slot definition identity.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _visitedDispatches = new(ReferenceEqualityComparer.Instance);

    /// <summary>Constructed type signatures already considered as sources of receiver contexts.</summary>
    private readonly Dictionary<TypeSignature, int> _receiverCandidates;

    /// <summary>Actually observed receiver contexts, indexed by receiver definition.</summary>
    private readonly Dictionary<TypeDefinition, Dictionary<TypeSignature, int>> _receiverContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Dispatch demands waiting for receiver type arguments, indexed by receiver definition.</summary>
    private readonly Dictionary<TypeDefinition, List<DispatchContextRegistry.Demand>> _receiverDemands = new(ReferenceEqualityComparer.Instance);

    /// <summary>Demand contexts grouped by stable binding and origin ordinals.</summary>
    private readonly Dictionary<(int Target, int Origin), Dictionary<IList<TypeSignature>, int>> _demandContexts = [];

    /// <summary>Dispatch-capable body contexts already expanded before the publishing barrier.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _expandedBodies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Actual default-body receiver observations in first-sighting order.</summary>
    private readonly List<DefaultReceiverObservation> _defaultReceiverObservations = [];

    /// <summary>Minimum observation depths for receivers with applicable default bodies.</summary>
    private readonly Dictionary<TypeSignature, DefaultReceiverObservation> _defaultReceiverContexts;

    /// <summary>Deferred default receivers, drained only after the existing supplemental worklist.</summary>
    private readonly Queue<DefaultReceiverObservation> _pendingDefaultReceivers = new();

    /// <summary>Type-only interface body contexts, separate from generic 'MethodSpec' contexts.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> _visitedDefaultBodies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Static call observations retained in first-sighting order without primary expansion.</summary>
    private readonly List<StaticCallObservation> _staticCallObservations = [];

    /// <summary>Minimum depths for exact static slot, receiver, and method-argument contexts.</summary>
    private readonly Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, StaticCallObservation>> _staticCallContexts = new(ReferenceEqualityComparer.Instance);

    /// <summary>Static calls deferred until earlier supplemental work and instance defaults have drained.</summary>
    private readonly Queue<StaticCallObservation> _pendingStaticCalls = new();

    /// <summary>Tags publications with the phase that owns their remaining supplemental allowance.</summary>
    private DispatchContextRegistry.DiscoveryPhase _dispatchPhase;

    /// <summary>Whether an oversized static call has already been retained for a deferred diagnostic.</summary>
    private bool _complexStaticCallRecorded;

    /// <summary>Whether a recursion warning has been reported for either a type or a method.</summary>
    private bool _recursionLimitReported;

    /// <summary>Whether the signature complexity warning has been reported.</summary>
    private bool _complexityLimitReported;

    /// <summary>Whether the shared transitive instantiation budget warning has been reported.</summary>
    private bool _transitiveTypeLimitReported;

    /// <summary>Whether an oversized dispatch observation has already been retained for diagnostics.</summary>
    private bool _complexDispatchRecorded;

    /// <summary>The number of transitive type and method instantiations reserved so far.</summary>
    private int _transitiveTypeCount;

    /// <summary>One oversized primary receiver retained silently for a supplemental diagnostic.</summary>
    private TypeSignature? _complexReceiver;

    /// <summary>The traversal depth of the retained oversized receiver.</summary>
    private int _complexReceiverDepth;

    /// <summary>The publication sequence within this enumeration.</summary>
    private int _publicationSequence;

    /// <summary>
    /// Creates the state for a single enumeration.
    /// </summary>
    /// <param name="module">The root module to analyze.</param>
    /// <param name="visitor">The visitor selecting the signatures to report.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures and argument contexts.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <param name="dispatchIndex">The complete dispatch metadata index.</param>
    /// <param name="dispatchContexts">The optional write-only cross-module registry.</param>
    /// <param name="isSupplemental">Whether this instance uses the isolated supplemental allowance.</param>
    /// <param name="primary">The completed primary enumeration, if this is its supplemental phase.</param>
    private TypeSignatureDiscovery(
        ModuleDefinition module,
        ITypeSignatureVisitor<IEnumerable<TResult>> visitor,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token,
        MethodDispatchIndex dispatchIndex,
        DispatchContextRegistry? dispatchContexts,
        bool isSupplemental = false,
        TypeSignatureDiscovery<TResult>? primary = null)
    {
        _module = module;
        _visitor = visitor;
        _signatureComparer = signatureComparer;
        _shouldProcessModule = shouldProcessModule;
        _isMarshallingDisabledModule = isMarshallingDisabledModule;
        _treatWarningsAsErrors = treatWarningsAsErrors;
        _token = token;
        _dispatchIndex = dispatchIndex;
        _dispatchContexts = dispatchContexts;
        _isSupplemental = isSupplemental;
        _primary = primary;
        _moduleOrdinal = dispatchIndex.GetModuleOrdinal(module);
        _receiverCandidates = new Dictionary<TypeSignature, int>(signatureComparer);
        _defaultReceiverContexts = new Dictionary<TypeSignature, DefaultReceiverObservation>(signatureComparer);
        _coveredTypes = new Dictionary<TypeSignature, int>(signatureComparer);
        _shallowerTypes = new Dictionary<TypeSignature, int>(signatureComparer);
        _results = new HashSet<TResult>(signatureComparer);
        _typeSpecifications = new HashSet<TypeSignature>(signatureComparer);
        _visitedTypes = new Dictionary<TypeSignature, int>(signatureComparer);
        _pendingMembers = new Queue<PendingMember>();
        _visitedMethods = new Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>>(ReferenceEqualityComparer.Instance);
    }

    /// <summary>
    /// Enumerates all unique type signatures selected by a visitor.
    /// </summary>
    /// <param name="module">The root module to analyze.</param>
    /// <param name="visitor">The visitor selecting the signatures to report.</param>
    /// <param name="signatureComparer">The comparer for discovered signatures and argument contexts.</param>
    /// <param name="shouldProcessModule">Determines whether to transitively discover members in a referenced module.</param>
    /// <param name="isMarshallingDisabledModule">Determines whether a module was explicitly excluded from member discovery.</param>
    /// <param name="treatWarningsAsErrors">Whether to promote discovery warnings to errors.</param>
    /// <param name="token">The cancellation token for discovery.</param>
    /// <param name="dispatchIndex">The complete dispatch metadata index.</param>
    /// <param name="dispatchContexts">The registry populated during parallel module processing.</param>
    /// <param name="completeCrossModuleJoins">Whether to join published contexts instead of reading metadata roots.</param>
    /// <returns>The unique type signatures of interest.</returns>
    public static IEnumerable<TResult> Enumerate(
        ModuleDefinition module,
        ITypeSignatureVisitor<IEnumerable<TResult>> visitor,
        SignatureComparer signatureComparer,
        Func<ModuleDefinition, bool> shouldProcessModule,
        Func<ModuleDefinition, bool> isMarshallingDisabledModule,
        bool treatWarningsAsErrors,
        CancellationToken token,
        MethodDispatchIndex dispatchIndex,
        DispatchContextRegistry dispatchContexts,
        bool completeCrossModuleJoins)
    {
        // Keep construction inside this iterator: repeated or interleaved enumerations must not share state
        // A join pass only reads the completed registry, so it publishes nothing and uses supplemental admission.
        TypeSignatureDiscovery<TResult> discovery = new(
            module: module,
            visitor: visitor,
            signatureComparer: signatureComparer,
            shouldProcessModule: shouldProcessModule,
            isMarshallingDisabledModule: isMarshallingDisabledModule,
            treatWarningsAsErrors: treatWarningsAsErrors,
            token: token,
            dispatchIndex: dispatchIndex,
            dispatchContexts: completeCrossModuleJoins ? null : dispatchContexts,
            isSupplemental: completeCrossModuleJoins);

        foreach (TResult result in completeCrossModuleJoins ? discovery.EnumerateCrossModuleJoins(dispatchContexts) : discovery.EnumerateRoots())
        {
            yield return result;
        }

        // Finish gathering explicit roots before applying their one-hop member eligibility to the worklist
        foreach (TResult result in discovery.EnumeratePendingMembers())
        {
            yield return result;
        }

        if (completeCrossModuleJoins)
        {
            // Only actual sightings in the join traversal enter this queue; imported receivers do not
            foreach (TResult result in discovery.EnumerateDefaultInterfaceMembers())
            {
                yield return result;
            }

            foreach (TResult result in discovery.EnumerateStaticDispatchMembers())
            {
                yield return result;
            }
        }
        else
        {
            // Receiver-only modules must still publish their contexts for the late cross-module join
            TypeSignatureDiscovery<TResult> supplemental = new(
                module: module,
                visitor: visitor,
                signatureComparer: signatureComparer,
                shouldProcessModule: shouldProcessModule,
                isMarshallingDisabledModule: isMarshallingDisabledModule,
                treatWarningsAsErrors: treatWarningsAsErrors,
                token: token,
                dispatchIndex: dispatchIndex,
                dispatchContexts: dispatchContexts,
                isSupplemental: true,
                primary: discovery);

            foreach (TResult result in supplemental.EnumerateSupplementalMembers())
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Visits metadata roots in field, method, 'TypeSpec', and 'MethodSpec' order, queuing their member contexts.
    /// </summary>
    /// <returns>The signatures visible from metadata roots.</returns>
    private IEnumerable<TResult> EnumerateRoots()
    {
        // Field definitions can have type signatures inline, without them appearing in the 'TypeSpec' table
        foreach (FieldDefinition field in _module.EnumerateTableMembers<FieldDefinition>(TableIndex.Field))
        {
            foreach (TResult result in EnumerateTypeSignatures(field.Signature?.FieldType))
            {
                yield return result;
            }
        }

        // Open method definitions can expose closed types through signatures, locals, and instruction operands
        foreach (MethodDefinition method in _module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes(_module.RuntimeContext))
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType))
                {
                    yield return result;
                }
            }
        }

        // 'TypeSpec'-s contain signatures referenced by metadata tokens, including base types and implemented interfaces
        foreach (TypeSpecification specification in _module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
        {
            foreach (TResult result in EnumerateTypeSignatures(specification.Signature))
            {
                yield return result;
            }

            // Keep scanning partially open specifications too, as their members can contain closed types
            if (specification.Signature is TypeSignature signature)
            {
                _ = _typeSpecifications.Add(signature);

                if (_visitedTypes.TryAdd(signature, 0))
                {
                    _pendingMembers.Enqueue(new PendingMember(
                        type: signature,
                        method: null,
                        context: default,
                        depth: 0));
                }
            }
        }

        // 'MethodSpec'-s supply arguments for generic calls and delegate targets. For example:
        //
        // static List<T> M<T>() => [];
        // static object N() => M<int>();
        //
        // Substitution reveals 'List<int>' even when it appears nowhere else in the caller's metadata.
        foreach (MethodSpecification specification in _module.EnumerateTableMembers<MethodSpecification>(TableIndex.MethodSpec))
        {
            foreach (TResult result in EnumerateMethodTypes(specification, default, depth: 0))
            {
                yield return result;
            }
        }

        // 'MethodSpec' rows carry no opcode; actual dispatch sites do not get the root's one-hop exemption
        foreach (MethodDefinition method in _module.EnumerateTableMembers<MethodDefinition>(TableIndex.Method))
        {
            foreach (MethodOperand operand in method.EnumerateMethodOperands())
            {
                RegisterDispatch(operand, default, 1);
            }
        }
    }
}
