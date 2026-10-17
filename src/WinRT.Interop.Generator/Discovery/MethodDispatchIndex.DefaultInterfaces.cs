// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="MethodDispatchIndex"/>
internal partial class MethodDispatchIndex
{
    /// <summary>
    /// Gets the selected default bodies for an actually observed receiver definition.
    /// </summary>
    /// <param name="receiver">The non-interface receiver definition.</param>
    /// <returns>The bodies in stable interface and method order.</returns>
    public IReadOnlyList<DefaultBody> GetDefaultBodies(TypeDefinition receiver)
    {
        return _defaultBodies.TryGetValue(receiver, out DefaultBody[]? bodies) ? bodies : [];
    }

    /// <summary>
    /// Gets the metadata bound that prevented precise default-body selection.
    /// </summary>
    /// <param name="receiver">The observed receiver definition.</param>
    /// <returns>The exceeded bound, or <see cref="DefaultHierarchyLimit.None"/>.</returns>
    public DefaultHierarchyLimit GetDefaultHierarchyLimit(TypeDefinition receiver)
    {
        return _defaultHierarchyLimits.GetValueOrDefault(receiver);
    }

    /// <summary>
    /// Composes only bounded metadata graphs and never selects from a truncated hierarchy.
    /// </summary>
    /// <param name="type">The definition whose formal hierarchy is needed.</param>
    /// <returns>The complete bounded hierarchy, or an empty array on overflow.</returns>
    private HierarchyType[] GetDefaultHierarchy(TypeDefinition type)
    {
        return GetBoundedInterfaceHierarchy(type, InterfaceDispatchKind.Instance);
    }

    /// <summary>
    /// Composes an interface hierarchy within the selected discovery path's independent caches.
    /// </summary>
    /// <param name="type">The definition whose formal hierarchy is needed.</param>
    /// <param name="kind">The interface dispatch path that owns the cached result.</param>
    /// <returns>The complete bounded hierarchy, or an empty array on overflow.</returns>
    private HierarchyType[] GetBoundedInterfaceHierarchy(TypeDefinition type, InterfaceDispatchKind kind)
    {
        Dictionary<TypeDefinition, HierarchyType[]> hierarchies = kind == InterfaceDispatchKind.Static
            ? _staticHierarchies
            : _defaultHierarchies;
        Dictionary<TypeDefinition, DefaultHierarchyLimit> limits = kind == InterfaceDispatchKind.Static
            ? _staticHierarchyLimits
            : _defaultHierarchyLimits;

        if (hierarchies.TryGetValue(type, out HierarchyType[]? hierarchy))
        {
            return hierarchy;
        }

        DefaultHierarchyLimit limit = DefaultHierarchyLimit.None;

        // This cache is separate from generic dispatch: bounded default selection must not change
        // which receiver projections the existing index exposes to the primary traversal.
        hierarchy = CreateHierarchy(
            type: type,
            runtimeContext: _runtimeContext,
            comparer: _comparer,
            token: _token,
            canVisit: (signature, depth) =>
            {
                // Latch the first exceeded bound so later branches cannot contribute to a partial selection
                if (limit != DefaultHierarchyLimit.None)
                {
                    return false;
                }

                // This depth measures the inheritance path, not a queued method body's traversal
                if (depth > MaxDefaultHierarchyDepth)
                {
                    limit = DefaultHierarchyLimit.Depth;

                    return false;
                }

                // Substitution can grow nested arguments even along a short inheritance path
                if (signature.GetSignatureElementCount(MaxDefaultHierarchyComplexity + 1) > MaxDefaultHierarchyComplexity)
                {
                    limit = DefaultHierarchyLimit.Complexity;

                    return false;
                }

                return true;
            });

        if (limit != DefaultHierarchyLimit.None)
        {
            limits.Add(type, limit);

            // Missing a more-specific ancestor could select a body that was actually re-abstracted or replaced
            hierarchy = [];
        }

        hierarchies.Add(type, hierarchy);

        return hierarchy;
    }

    /// <summary>
    /// Checks definition relationships before composing any interface signatures.
    /// </summary>
    /// <param name="type">The possible receiver.</param>
    /// <returns>Whether its interface graph contains a non-generic instance default body.</returns>
    private bool HasDefaultBodies(TypeDefinition type)
    {
        // Most types cannot supply an eligible default body; reject those graphs without
        // constructing receiver-specific signatures or deserializing any method bodies.
        return HasReachableDefinition(
            type: type,
            cache: _hasDefaultBodies,
            isRelevant: HasEligibleDefaultBody);
    }

    /// <summary>
    /// Checks whether an interface declares a default body eligible for this invocation.
    /// </summary>
    /// <param name="type">The reachable definition.</param>
    /// <returns>Whether the definition supplies a non-generic instance default under the module policy.</returns>
    private bool HasEligibleDefaultBody(TypeDefinition type)
    {
        if (!type.IsInterface)
        {
            return false;
        }

        // Metadata alone is insufficient: only policy-eligible bodies participate in discovery
        if (type.DeclaringModule is not ModuleDefinition module ||
            !_shouldProcessModule(module))
        {
            return false;
        }

        foreach (MethodDefinition method in type.Methods)
        {
            if (!method.IsVirtual ||
                method.IsStatic ||
                method.IsAbstract)
            {
                continue;
            }

            if (method.Signature is { GenericParameterCount: 0 })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves default bodies without broadening the generic dispatch target index.
    /// </summary>
    /// <param name="receiver">The receiver definition being indexed.</param>
    private void IndexDefaultBodies(TypeDefinition receiver)
    {
        // Non-generic receivers already encode their closed interfaces in explicit 'TypeSpec' roots
        // Abstract receivers cannot dispatch, and projecting onto a base would lose derived shadowing.
        if (receiver.IsInterface ||
            receiver.IsAbstract ||
            receiver.GenericParameters.Count == 0)
        {
            return;
        }

        if (!HasDefaultBodies(receiver))
        {
            return;
        }

        // Keep interface owners in receiver parameters, such as 'IFactory<T>' on 'Factory<T>'
        // Discovery supplies the closed arguments only when it observes a constructed receiver.
        HierarchyType[] hierarchy = GetDefaultHierarchy(receiver);
        List<DefaultBody> bodies = [];

        foreach (HierarchyType interfaceType in hierarchy.Where(static entry => entry.Definition.IsInterface))
        {
            foreach (MethodDefinition declaration in interfaceType.Definition.Methods)
            {
                _token.ThrowIfCancellationRequested();

                // Generic slots stay on the 'MethodSpec' path; receiver arguments alone do not justify that scan
                if (!declaration.IsVirtual ||
                    declaration.IsStatic ||
                    declaration.Signature is not { GenericParameterCount: 0 } signature)
                {
                    continue;
                }

                // Explicit implementation methods are bodies for another slot, not independent entry points
                if (interfaceType.Definition.IsExplicitImplementationBody(declaration, _runtimeContext))
                {
                    continue;
                }

                // Compare slot and implementation signatures in the same receiver parameter space
                MethodSignature slotSignature = signature.InstantiateGenericTypes(new GenericContext(
                    type: interfaceType.Signature as GenericInstanceTypeSignature,
                    method: null));

                if (HasClassImplementation(hierarchy, interfaceType, declaration, slotSignature))
                {
                    continue;
                }

                DefaultBody[] mostSpecific = GetMostSpecificInterfaceBodies(
                    hierarchy: hierarchy,
                    interfaceType: interfaceType,
                    declaration: declaration,
                    receiver: receiver,
                    kind: InterfaceDispatchKind.Instance);

                // The ancestry predicate records an owner-hierarchy limit on the receiver if comparison is incomplete
                // Discard the whole selection, including earlier slots, rather than risk choosing a replaced body.
                if (GetDefaultHierarchyLimit(receiver) != DefaultHierarchyLimit.None)
                {
                    return;
                }

                // An ambiguous default has no callable implementation; never select by metadata order
                if (mostSpecific is not [DefaultBody selected])
                {
                    continue;
                }

                // A most-specific re-abstraction hides inherited defaults without supplying a callable body
                if (selected.Method.IsAbstract)
                {
                    continue;
                }

                if (selected.Method.DeclaringModule is not ModuleDefinition module ||
                    !_shouldProcessModule(module))
                {
                    continue;
                }

                // Multiple interface slots can resolve to the same body in the same constructed owner
                if (!bodies.Any(body =>
                    ReferenceEquals(body.Method, selected.Method) &&
                    _comparer.Equals(body.Owner, selected.Owner)))
                {
                    bodies.Add(selected);
                }
            }
        }

        if (bodies.Count > 0)
        {
            _defaultBodies.Add(receiver, [.. bodies]);
        }
    }

    /// <summary>
    /// Selects maximal interface implementations while retaining re-abstractions in the comparison.
    /// </summary>
    /// <param name="hierarchy">The receiver hierarchy in formal receiver parameters.</param>
    /// <param name="interfaceType">The constructed interface declaring the slot.</param>
    /// <param name="declaration">The slot definition.</param>
    /// <param name="receiver">The receiver whose bounds constrain selection.</param>
    /// <param name="kind">The discovery path owning the hierarchy caches.</param>
    /// <returns>The most-specific candidates, including abstract entries.</returns>
    private DefaultBody[] GetMostSpecificInterfaceBodies(
        HierarchyType[] hierarchy,
        HierarchyType interfaceType,
        MethodDefinition declaration,
        TypeDefinition receiver,
        InterfaceDispatchKind kind)
    {
        List<DefaultBody> candidates = [];

        // Abstract 'MethodImpl'-s must participate so re-abstraction can hide an inherited body
        foreach (HierarchyType owner in hierarchy.Where(static entry => entry.Definition.IsInterface))
        {
            candidates.AddRange(GetExplicitDefaultImplementations(owner, declaration, interfaceType.Signature, kind));
        }

        // The declaration's own default survives when no more-derived interface remaps its slot
        candidates.Add(new DefaultBody(
            Method: declaration,
            Owner: interfaceType.Signature));

        // A more-derived constructed owner supersedes an ancestor, regardless of whether it supplies a body
        return [.. candidates.Where(candidate => !HasMoreSpecificDefaultBody(candidate, candidates, receiver, kind))];
    }

    /// <summary>
    /// Checks whether a class implementation takes precedence over an interface default.
    /// </summary>
    /// <param name="hierarchy">The receiver hierarchy in its formal type context.</param>
    /// <param name="interfaceType">The constructed interface declaring the slot.</param>
    /// <param name="declaration">The slot definition.</param>
    /// <param name="slotSignature">The slot signature after substituting receiver arguments.</param>
    /// <returns>Whether the receiver or a base class implements the slot.</returns>
    private bool HasClassImplementation(
        HierarchyType[] hierarchy,
        HierarchyType interfaceType,
        MethodDefinition declaration,
        MethodSignature slotSignature)
    {
        foreach (HierarchyType owner in hierarchy)
        {
            if (owner.Definition.IsInterface)
            {
                continue;
            }

            // 'MethodImpl' identifies explicit implementations and compiler-generated stubs without name matching
            if (GetExplicitDefaultImplementations(owner, declaration, interfaceType.Signature).Any())
            {
                return true;
            }

            // Implicit implementations must be public virtual instance methods on the class or a base class
            foreach (MethodDefinition body in owner.Definition.Methods)
            {
                if (!body.IsPublic ||
                    !body.IsVirtual ||
                    body.IsStatic)
                {
                    continue;
                }

                if (body.Name != declaration.Name ||
                    body.Signature is not { } bodySignature)
                {
                    continue;
                }

                // Compare the full substituted signature, not just the method name
                MethodSignature implementationSignature = bodySignature.InstantiateGenericTypes(new GenericContext(
                    type: owner.Signature as GenericInstanceTypeSignature,
                    method: null));

                if (_comparer.Equals(slotSignature, implementationSignature))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether another candidate has a more-derived constructed interface owner.
    /// </summary>
    /// <param name="candidate">The body whose precedence is being checked.</param>
    /// <param name="candidates">All bodies and re-abstractions for the slot, in metadata order.</param>
    /// <param name="receiver">The receiver whose hierarchy bounds constrain selection.</param>
    /// <param name="kind">The discovery path owning the hierarchy caches.</param>
    /// <returns>Whether another owner supersedes the candidate.</returns>
    private bool HasMoreSpecificDefaultBody(
        DefaultBody candidate,
        IReadOnlyList<DefaultBody> candidates,
        TypeDefinition receiver,
        InterfaceDispatchKind kind)
    {
        foreach (DefaultBody other in candidates)
        {
            // Entries on the same constructed owner do not establish an inheritance relationship
            if (_comparer.Equals(candidate.Owner, other.Owner))
            {
                continue;
            }

            // Preserve metadata order: ancestry checks can record a bound that invalidates the whole selection
            if (IsDefaultInterfaceAncestor(candidate.Owner, other.Owner, receiver, kind))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves explicit slot remaps in the receiver's formal argument context.
    /// </summary>
    /// <param name="owner">The possible implementation owner.</param>
    /// <param name="declaration">The original slot definition.</param>
    /// <param name="slotType">The slot's constructed interface context.</param>
    /// <param name="kind">Whether declaration indexing can use the static path's owner cache.</param>
    /// <returns>The matching body definitions, including abstract re-abstractions.</returns>
    private IEnumerable<DefaultBody> GetExplicitDefaultImplementations(
        HierarchyType owner,
        MethodDefinition declaration,
        TypeSignature slotType,
        InterfaceDispatchKind kind = InterfaceDispatchKind.Instance)
    {
        IEnumerable<MethodImplementation> implementations = kind == InterfaceDispatchKind.Static
            ? GetStaticMethodImplementations(owner.Definition, declaration)
            : owner.Definition.MethodImplementations;

        foreach (MethodImplementation implementation in implementations)
        {
            _token.ThrowIfCancellationRequested();

            // Resolve the declaration identity before reading a potentially unrelated implementation body
            if (implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? slot) is not true ||
                !ReferenceEquals(slot, declaration))
            {
                continue;
            }

            if (implementation.Body?.TryResolve(_runtimeContext, out MethodDefinition? body) is not true)
            {
                continue;
            }

            if (!TryGetDeclaringSignature(implementation.Declaration, out TypeSignature? declaringType))
            {
                continue;
            }

            // Matching the resolved method alone would conflate different constructions of the same interface
            // Substitute the declaring type of the 'MethodImpl' so each remap binds to the correct constructed slot.
            TypeSignature constructedSlotType = declaringType.InstantiateGenericTypes(new GenericContext(
                type: owner.Signature as GenericInstanceTypeSignature,
                method: null));

            if (!_comparer.Equals(slotType, constructedSlotType))
            {
                continue;
            }

            yield return new DefaultBody(
                Method: body,
                Owner: owner.Signature);
        }
    }

    /// <summary>
    /// Checks specificity using constructed interface ancestry rather than definition names.
    /// </summary>
    /// <param name="ancestor">The less-specific candidate owner.</param>
    /// <param name="descendant">The possible overriding owner.</param>
    /// <param name="receiver">The receiver whose selection must be discarded if specificity cannot be determined.</param>
    /// <param name="kind">The discovery path owning the hierarchy caches.</param>
    /// <returns>Whether the latter inherits the former in this receiver context.</returns>
    private bool IsDefaultInterfaceAncestor(
        TypeSignature ancestor,
        TypeSignature descendant,
        TypeDefinition receiver,
        InterfaceDispatchKind kind)
    {
        if (!descendant.TryResolve(_runtimeContext, out TypeDefinition? definition))
        {
            return false;
        }

        // The cached ancestry uses the descendant definition's parameters, not the candidate owner's arguments
        // Apply those arguments before comparing reordered or nested constructed ancestors.
        GenericContext context = new(
            type: descendant as GenericInstanceTypeSignature,
            method: null);

        HierarchyType[] hierarchy = GetBoundedInterfaceHierarchy(definition, kind);
        Dictionary<TypeDefinition, DefaultHierarchyLimit> limits = kind == InterfaceDispatchKind.Static
            ? _staticHierarchyLimits
            : _defaultHierarchyLimits;

        // An incomplete candidate ancestry also makes the receiver's entire specificity selection inconclusive
        if (limits.TryGetValue(definition, out DefaultHierarchyLimit limit))
        {
            _ = limits.TryAdd(receiver, limit);

            return false;
        }

        return hierarchy
            .Skip(1)
            .Any(entry => _comparer.Equals(ancestor, entry.Signature.InstantiateGenericTypes(context)));
    }

    /// <summary>
    /// An interface implementation body whose owner is expressed in receiver parameters.
    /// </summary>
    /// <param name="Method">The implementation body definition.</param>
    /// <param name="Owner">Its declaring owner in the receiver's formal context.</param>
    public sealed record DefaultBody(MethodDefinition Method, TypeSignature Owner);

    /// <summary>The interface dispatch path whose hierarchy and bounds are being evaluated.</summary>
    private enum InterfaceDispatchKind
    {
        /// <summary>Receiver-driven instance default bodies.</summary>
        Instance,

        /// <summary>Constrained static virtual calls.</summary>
        Static
    }

    /// <summary>
    /// The discovery bound reached while composing a default-interface hierarchy.
    /// </summary>
    public enum DefaultHierarchyLimit
    {
        /// <summary>The hierarchy was composed completely.</summary>
        None,

        /// <summary>An inheritance path exceeded the discovery depth.</summary>
        Depth,

        /// <summary>A substituted ancestor exceeded the signature complexity limit.</summary>
        Complexity
    }
}
