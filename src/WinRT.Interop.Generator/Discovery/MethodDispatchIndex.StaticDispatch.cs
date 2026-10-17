// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="MethodDispatchIndex"/>
internal partial class MethodDispatchIndex
{
    /// <summary>
    /// Resolves a static slot against its exact constructed constrained receiver.
    /// </summary>
    /// <param name="declaration">The resolved static interface declaration.</param>
    /// <param name="declaringType">The substituted declaring interface at the call site.</param>
    /// <param name="receiverType">The actual closed constrained receiver.</param>
    /// <param name="target">The unique selected implementation template.</param>
    /// <param name="bodyType">Its constructed declaring owner.</param>
    /// <returns>Whether selection is complete and identifies one implementation.</returns>
    public bool TryGetStaticTarget(
        MethodDefinition declaration,
        TypeSignature declaringType,
        TypeSignature receiverType,
        [NotNullWhen(true)] out Target? target,
        [NotNullWhen(true)] out TypeSignature? bodyType)
    {
        target = null;
        bodyType = null;

        // The cache is keyed by receiver definition; a truncated hierarchy cannot establish an exact winner
        if (!receiverType.TryResolve(_runtimeContext, out TypeDefinition? receiver) ||
            GetStaticHierarchyLimit(receiver) != DefaultHierarchyLimit.None)
        {
            return false;
        }

        if (!_staticTargets.TryGetValue(receiver, out Dictionary<MethodDefinition, List<Target>>? declarations) ||
            !declarations.TryGetValue(declaration, out List<Target>? targets))
        {
            return false;
        }

        // Cached slots and owners use formal receiver parameters; close both with this call's actual receiver
        GenericContext context = new(
            type: receiverType as GenericInstanceTypeSignature,
            method: null);

        foreach (Target candidate in targets)
        {
            // Static dispatch binds the actual interface construction, not a variant conversion or a method name
            if (!_comparer.Equals(declaringType, candidate.DeclarationType.InstantiateGenericTypes(context)))
            {
                continue;
            }

            TypeSignature owner = candidate.BodyType.InstantiateGenericTypes(context);

            // Distinct bodies or owners for the same slot are ambiguous; metadata order must not choose between them
            if (target is not null &&
                (!ReferenceEquals(target.Method, candidate.Method) || !_comparer.Equals(bodyType, owner)))
            {
                target = null;
                bodyType = null;

                return false;
            }

            target = candidate;
            bodyType = owner;
        }

        return target is not null && bodyType is not null;
    }

    /// <summary>
    /// Gets the bound that prevented complete static slot selection.
    /// </summary>
    /// <param name="receiver">The actual receiver definition.</param>
    /// <returns>The exceeded bound, or <see cref="DefaultHierarchyLimit.None"/>.</returns>
    public DefaultHierarchyLimit GetStaticHierarchyLimit(TypeDefinition receiver)
    {
        return _staticHierarchyLimits.GetValueOrDefault(receiver);
    }

    /// <summary>
    /// Selects static implementations without adding targets or ordinals to generic dispatch.
    /// </summary>
    /// <param name="receiver">The possible constrained receiver definition.</param>
    private void IndexStaticDispatch(TypeDefinition receiver)
    {
        // Reject graphs with no static virtual slot before composing their constructed ancestry
        if (!HasReachableDefinition(
            type: receiver,
            cache: _hasStaticDispatch,
            isRelevant: DeclaresStaticVirtualSlot))
        {
            return;
        }

        // Ineligible class replacements still matter when they hide an eligible interface default
        // Requiring some eligible static body in the graph avoids indexing unrelated framework-only implementations.
        if (!HasReachableDefinition(
            type: receiver,
            cache: _hasEligibleStaticBodies,
            isRelevant: HasEligibleStaticBody))
        {
            return;
        }

        // Select once in receiver-parameter space; actual method arguments are supplied later by observed calls
        HierarchyType[] hierarchy = GetBoundedInterfaceHierarchy(receiver, InterfaceDispatchKind.Static);
        Dictionary<MethodDefinition, List<Target>> targets = new(ReferenceEqualityComparer.Instance);

        foreach (HierarchyType iface in hierarchy.Where(static entry => entry.Definition.IsInterface))
        {
            foreach (MethodDefinition declaration in iface.Definition.Methods)
            {
                _token.ThrowIfCancellationRequested();

                // Overrides are bodies for another slot, so start from the original static virtual declarations
                if (!declaration.IsStatic ||
                    !declaration.IsVirtual ||
                    declaration.Signature is null)
                {
                    continue;
                }

                if (iface.Definition.IsExplicitImplementationBody(declaration, _runtimeContext))
                {
                    continue;
                }

                DefaultBody? selected = null;

                // Static class implementations use 'MethodImpl'-s, including implicit C# implementations
                // A same-named hiding method without a remap does not replace an inherited implementation.
                // The hierarchy visits the receiver before its bases, so the first class remap takes precedence.
                foreach (HierarchyType owner in hierarchy)
                {
                    if (owner.Definition.IsInterface)
                    {
                        continue;
                    }

                    selected = GetExplicitDefaultImplementations(owner, declaration, iface.Signature, InterfaceDispatchKind.Static)
                        .FirstOrDefault();

                    if (selected is not null)
                    {
                        break;
                    }
                }

                // Class remaps take precedence; only receivers without one use interface-default specificity
                if (selected is null)
                {
                    DefaultBody[] candidates = GetMostSpecificInterfaceBodies(
                        hierarchy: hierarchy,
                        iface: iface,
                        declaration: declaration,
                        receiver: receiver,
                        kind: InterfaceDispatchKind.Static);

                    // An incomplete ancestor comparison invalidates every slot collected for this receiver
                    if (GetStaticHierarchyLimit(receiver) != DefaultHierarchyLimit.None)
                    {
                        return;
                    }

                    if (candidates is not [DefaultBody implementation])
                    {
                        continue;
                    }

                    selected = implementation;
                }

                // A most-specific re-abstraction hides inherited code without providing an executable replacement
                if (selected.Method.IsAbstract ||
                    !selected.Method.IsStatic)
                {
                    continue;
                }

                if (!targets.TryGetValue(declaration, out List<Target>? implementations))
                {
                    implementations = [];

                    targets.Add(declaration, implementations);
                }

                // Body policy is checked at admission, after selection has ruled out hidden defaults
                implementations.Add(new Target(
                    Receiver: receiver,
                    DeclarationType: iface.Signature,
                    Method: selected.Method,
                    BodyType: selected.Owner));
            }
        }

        if (targets.Count > 0)
        {
            _staticTargets.Add(receiver, targets);
        }
    }

    /// <summary>
    /// Groups an owner's static remaps without repeatedly resolving every declaration for every slot.
    /// </summary>
    /// <param name="owner">The definition carrying the remaps.</param>
    /// <param name="declaration">The resolved slot whose rows are needed.</param>
    /// <returns>The matching metadata rows in their original order.</returns>
    private IEnumerable<MethodImplementation> GetStaticMethodImplementations(TypeDefinition owner, MethodDefinition declaration)
    {
        if (!_staticMethodImplementations.TryGetValue(owner, out Dictionary<MethodDefinition, List<MethodImplementation>>? declarations))
        {
            declarations = new Dictionary<MethodDefinition, List<MethodImplementation>>(ReferenceEqualityComparer.Instance);

            // Group only declaration identities here; body resolution and constructed-slot checks remain demand-specific
            foreach (MethodImplementation implementation in owner.MethodImplementations)
            {
                _token.ThrowIfCancellationRequested();

                if (implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? slot) is not true ||
                    !slot.IsStatic)
                {
                    continue;
                }

                if (!declarations.TryGetValue(slot, out List<MethodImplementation>? rows))
                {
                    rows = [];

                    declarations.Add(slot, rows);
                }

                rows.Add(implementation);
            }

            _staticMethodImplementations.Add(owner, declarations);
        }

        return declarations.TryGetValue(declaration, out List<MethodImplementation>? implementations) ? implementations : [];
    }

    /// <summary>
    /// Checks whether a definition declares a static virtual interface slot.
    /// </summary>
    /// <param name="type">The reachable definition.</param>
    /// <returns>Whether this interface can participate in constrained static dispatch.</returns>
    private static bool DeclaresStaticVirtualSlot(TypeDefinition type)
    {
        if (!type.IsInterface)
        {
            return false;
        }

        foreach (MethodDefinition method in type.Methods)
        {
            if (!method.IsStatic ||
                !method.IsVirtual)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks definition metadata for a policy-eligible static body without loading its IL.
    /// </summary>
    /// <param name="type">The reachable definition.</param>
    /// <returns>Whether the definition could supply a static implementation.</returns>
    private bool HasEligibleStaticBody(TypeDefinition type)
    {
        if (type.DeclaringModule is not ModuleDefinition module ||
            !_shouldProcessModule(module))
        {
            return false;
        }

        // A declaration's own static default needs no 'MethodImpl' remap
        if (type.IsInterface)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                if (!method.IsStatic ||
                    !method.IsVirtual ||
                    method.IsAbstract)
                {
                    continue;
                }

                return true;
            }
        }

        // Replacements can be private and non-virtual, unlike the original slot
        // Inspect 'MethodImpl'-s to recognize both explicit interface overrides and C# static class implementations.
        foreach (MethodImplementation implementation in type.MethodImplementations)
        {
            if (implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? declaration) is not true ||
                !declaration.IsStatic ||
                !declaration.IsVirtual)
            {
                continue;
            }

            if (implementation.Body?.TryResolve(_runtimeContext, out MethodDefinition? body) is true &&
                body.IsStatic &&
                !body.IsAbstract)
            {
                return true;
            }
        }

        return false;
    }
}
