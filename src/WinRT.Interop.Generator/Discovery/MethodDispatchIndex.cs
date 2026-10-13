// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// An invocation's read-only generic dispatch and default-body relationships, built without reading method bodies.
/// </summary>
internal sealed partial class MethodDispatchIndex
{
    /// <summary>The default-interface ancestry bound, mirroring the member discovery depth limit.</summary>
    private const int MaxDefaultHierarchyDepth = 32;

    /// <summary>The default-interface signature bound, mirroring the member discovery complexity limit.</summary>
    private const int MaxDefaultHierarchyComplexity = 256;

    /// <summary>The runtime context shared by the loaded input modules.</summary>
    private readonly RuntimeContext _runtimeContext;

    /// <summary>The resolution-aware comparer for signatures and substituted relationships.</summary>
    private readonly SignatureComparer _comparer;

    /// <summary>Whether bodies from a loaded module participate in transitive discovery.</summary>
    private readonly Func<ModuleDefinition, bool> _shouldProcessModule;

    /// <summary>The token checked while indexing metadata relationships.</summary>
    private readonly CancellationToken _token;

    /// <summary>Metadata hierarchies in each type's formal parameter context.</summary>
    private readonly Dictionary<TypeDefinition, HierarchyType[]> _hierarchies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Whether a definition's metadata hierarchy contains any generic virtual declaration.</summary>
    private readonly Dictionary<TypeDefinition, bool> _hasGenericDispatch = new(ReferenceEqualityComparer.Instance);

    /// <summary>Whether a definition's interface graph can supply non-generic default bodies.</summary>
    private readonly Dictionary<TypeDefinition, bool> _hasDefaultBodies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Applicable default bodies in each concrete receiver's formal type context.</summary>
    private readonly Dictionary<TypeDefinition, DefaultBody[]> _defaultBodies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Bounded default-interface hierarchies, separate from the existing generic dispatch cache.</summary>
    private readonly Dictionary<TypeDefinition, HierarchyType[]> _defaultHierarchies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Incomplete default hierarchies, diagnosed only if their receiver is actually observed.</summary>
    private readonly Dictionary<TypeDefinition, DefaultHierarchyLimit> _defaultHierarchyLimits = new(ReferenceEqualityComparer.Instance);

    /// <summary>Possible implementations, indexed by declaration identity rather than method name.</summary>
    private readonly Dictionary<MethodDefinition, List<Target>> _targets = new(ReferenceEqualityComparer.Instance);

    /// <summary>Generic receiver definitions whose actual contexts can close dispatch bodies.</summary>
    private readonly HashSet<TypeDefinition> _receivers = new(ReferenceEqualityComparer.Instance);

    /// <summary>Only the hierarchy entries that can contribute receiver arguments to dispatch.</summary>
    private readonly Dictionary<TypeDefinition, HierarchyType[]> _receiverHierarchies = new(ReferenceEqualityComparer.Instance);

    /// <summary>The stable ordinals of all loaded input modules.</summary>
    private readonly Dictionary<ModuleDefinition, int> _moduleOrdinals = new(ReferenceEqualityComparer.Instance);

    /// <summary>Methods that can be reached as indexed dispatch bodies.</summary>
    private readonly HashSet<MethodDefinition> _bodyMethods = new(ReferenceEqualityComparer.Instance);

    /// <summary>The next stable binding ordinal.</summary>
    private int _targetOrdinal;

    /// <summary>
    /// Builds the index after all input modules have been loaded.
    /// </summary>
    /// <param name="modules">The eligible input modules, in stable metadata order.</param>
    /// <param name="runtimeContext">The invocation's resolution context.</param>
    /// <param name="comparer">The invocation's signature comparer.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="shouldProcessModule">The policy for indexed implementation bodies.</param>
    public MethodDispatchIndex(
        IEnumerable<ModuleDefinition> modules,
        RuntimeContext runtimeContext,
        SignatureComparer comparer,
        CancellationToken token,
        Func<ModuleDefinition, bool> shouldProcessModule)
    {
        _runtimeContext = runtimeContext;
        _comparer = comparer;
        _token = token;
        _shouldProcessModule = shouldProcessModule;

        // Stable module and metadata order also determines bounded dispatch scheduling order
        foreach (ModuleDefinition module in modules)
        {
            _moduleOrdinals.Add(module, _moduleOrdinals.Count);

            foreach (TypeDefinition type in module.GetAllTypes())
            {
                token.ThrowIfCancellationRequested();
                IndexType(type);

                // Default selection has separate caches and does not add generic dispatch targets
                IndexDefaultBodies(type);
            }
        }

        // Retain only ancestors that can supply actual generic receiver arguments
        foreach ((TypeDefinition type, HierarchyType[] hierarchy) in _hierarchies)
        {
            HierarchyType[] receivers = [.. hierarchy.Where(entry => _receivers.Contains(entry.Definition))];

            if (receivers.Length > 0)
            {
                _receiverHierarchies.Add(type, receivers);
            }
        }
    }

    /// <summary>
    /// Gets the possible bodies for an observed generic virtual declaration.
    /// </summary>
    /// <param name="method">The resolved declaration.</param>
    /// <returns>The targets in stable metadata order.</returns>
    public IEnumerable<Target> GetTargets(MethodDefinition method)
    {
        return _targets.TryGetValue(method, out List<Target>? targets) ? targets : [];
    }

    /// <summary>Gets a loaded root module's stable invocation ordinal.</summary>
    /// <param name="module">The loaded root module.</param>
    /// <returns>The stable ordinal.</returns>
    public int GetModuleOrdinal(ModuleDefinition module)
    {
        // A completion-only root outside the index sorts after the loaded publishing modules
        return _moduleOrdinals.TryGetValue(module, out int ordinal) ? ordinal : _moduleOrdinals.Count;
    }

    /// <summary>Checks whether a method can be used as an indexed dispatch body.</summary>
    /// <param name="method">The resolved method definition.</param>
    /// <returns>Whether it is a dispatch body.</returns>
    public bool IsDispatchBody(MethodDefinition method)
    {
        return _bodyMethods.Contains(method);
    }

    /// <summary>
    /// Projects an observed constructed type onto relevant receiver definitions in its hierarchy.
    /// </summary>
    /// <param name="signature">An actually observed type context.</param>
    /// <returns>The receiver contexts, without inventing type arguments.</returns>
    public IEnumerable<TypeSignature> GetReceiverContexts(TypeSignature signature)
    {
        if (!signature.TryResolve(_runtimeContext, out TypeDefinition? type) ||
            !_receiverHierarchies.TryGetValue(type, out HierarchyType[]? hierarchy))
        {
            yield break;
        }

        GenericContext context = new(
            type: signature as GenericInstanceTypeSignature,
            method: null);

        foreach (HierarchyType entry in hierarchy)
        {
            yield return entry.Signature.InstantiateGenericTypes(context);
        }
    }

    /// <summary>
    /// Indexes class slots, explicit 'MethodImpl'-s, and implicit or inherited interface implementations.
    /// </summary>
    /// <param name="type">The receiver definition.</param>
    private void IndexType(TypeDefinition type)
    {
        // Most framework and projection types have no generic slots, so avoid composing their hierarchies
        if (!HasGenericDispatch(type))
        {
            return;
        }

        TypeSignature receiver = GetIdentitySignature(type);

        // Follow class slots and explicit remaps in their original metadata order
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsVirtual && method.Signature is { GenericParameterCount: > 0 })
            {
                foreach ((MethodDefinition declaration, TypeSignature declaringType) in GetSlots(method, receiver, new HashSet<MethodDefinition>(ReferenceEqualityComparer.Instance)))
                {
                    AddTarget(declaration, new Target(
                        Receiver: type,
                        DeclarationType: declaringType,
                        Method: method,
                        BodyType: receiver));
                }
            }
        }

        if (type.IsInterface)
        {
            return;
        }

        HierarchyType[] hierarchy = GetOrAddHierarchy(type);

        // Interfaces can be implemented explicitly, by an inherited method, or by a default body
        foreach (HierarchyType iface in hierarchy.Where(static entry => entry.Definition.IsInterface))
        {
            IndexInterfaceImplementations(type, iface, hierarchy);
        }
    }

    /// <summary>
    /// Indexes each generic declaration against the receiver's substitution-aware hierarchy.
    /// </summary>
    /// <param name="receiver">The receiver definition.</param>
    /// <param name="iface">The implemented interface in the receiver's formal parameters.</param>
    /// <param name="hierarchy">The possible owners of implementation bodies.</param>
    private void IndexInterfaceImplementations(TypeDefinition receiver, HierarchyType iface, HierarchyType[] hierarchy)
    {
        foreach (MethodDefinition declaration in iface.Definition.Methods)
        {
            _token.ThrowIfCancellationRequested();

            if (!declaration.IsVirtual || declaration.Signature is not { GenericParameterCount: > 0 })
            {
                continue;
            }

            // Compare candidate bodies against the interface signature expressed in receiver parameters
            MethodSignature signature = declaration.Signature.InstantiateGenericTypes(new GenericContext(
                type: iface.Signature as GenericInstanceTypeSignature,
                method: null));

            foreach (HierarchyType owner in hierarchy)
            {
                IndexExplicitImplementations(receiver, iface, declaration, owner);
                IndexImplicitImplementations(receiver, iface, declaration, signature, owner);
            }
        }
    }

    /// <summary>
    /// Indexes 'MethodImpl' bindings, including covariant returns and default-interface overrides.
    /// </summary>
    /// <param name="receiver">The receiver definition.</param>
    /// <param name="iface">The implemented interface context.</param>
    /// <param name="declaration">The declaration identity.</param>
    /// <param name="owner">The owner of possible implementation bodies.</param>
    private void IndexExplicitImplementations(
        TypeDefinition receiver,
        HierarchyType iface,
        MethodDefinition declaration,
        HierarchyType owner)
    {
        foreach (MethodImplementation implementation in owner.Definition.MethodImplementations)
        {
            // Explicit metadata identifies the exact slot and body, allowing renamed members and covariant returns
            if (implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? slot) is true &&
                ReferenceEquals(slot, declaration) &&
                implementation.Body?.TryResolve(_runtimeContext, out MethodDefinition? body) is true &&
                TryGetDeclaringSignature(implementation.Declaration, out TypeSignature? declaringSignature))
            {
                TypeSignature slotType = declaringSignature.InstantiateGenericTypes(new GenericContext(
                    type: owner.Signature as GenericInstanceTypeSignature,
                    method: null));

                if (_comparer.Equals(slotType, iface.Signature))
                {
                    AddTarget(declaration, new Target(
                        Receiver: receiver,
                        DeclarationType: iface.Signature,
                        Method: body,
                        BodyType: owner.Signature));
                }
            }
        }
    }

    /// <summary>
    /// Indexes signature-matching class implementations and the declaration's own default body.
    /// </summary>
    /// <param name="receiver">The receiver definition.</param>
    /// <param name="iface">The implemented interface context.</param>
    /// <param name="declaration">The declaration identity.</param>
    /// <param name="signature">The declaration signature in the receiver's formal parameters.</param>
    /// <param name="owner">The owner of possible implementation bodies.</param>
    private void IndexImplicitImplementations(
        TypeDefinition receiver,
        HierarchyType iface,
        MethodDefinition declaration,
        MethodSignature signature,
        HierarchyType owner)
    {
        foreach (MethodDefinition body in owner.Definition.Methods)
        {
            // Other interface bodies require an explicit 'MethodImpl'; only this declaration supplies its default body
            if (owner.Definition.IsInterface)
            {
                if (ReferenceEquals(body, declaration) && !body.IsAbstract)
                {
                    AddTarget(declaration, new Target(
                        Receiver: receiver,
                        DeclarationType: iface.Signature,
                        Method: body,
                        BodyType: owner.Signature));
                }

                continue;
            }

            // Match public class implementations after substituting inherited receiver arguments
            if (body.IsPublic && body.IsStatic == declaration.IsStatic &&
                body.Name == declaration.Name &&
                body.Signature is { GenericParameterCount: > 0 } bodySignature &&
                _comparer.Equals(signature, bodySignature.InstantiateGenericTypes(new GenericContext(
                    type: owner.Signature as GenericInstanceTypeSignature,
                    method: null))))
            {
                AddTarget(declaration, new Target(
                    Receiver: receiver,
                    DeclarationType: iface.Signature,
                    Method: body,
                    BodyType: owner.Signature));
            }
        }
    }

    /// <summary>
    /// Follows explicit slot remaps and nearest matching reuse-slot declarations.
    /// </summary>
    /// <param name="method">The overriding or implementing method.</param>
    /// <param name="declaringType">Its declaring context in the receiver's type parameters.</param>
    /// <param name="visited">The declaration identities already on this path.</param>
    /// <returns>The declarations that dispatch to this method.</returns>
    private IEnumerable<(MethodDefinition Declaration, TypeSignature DeclaringType)> GetSlots(
        MethodDefinition method,
        TypeSignature declaringType,
        HashSet<MethodDefinition> visited)
    {
        _token.ThrowIfCancellationRequested();

        if (!visited.Add(method))
        {
            yield break;
        }

        yield return (method, declaringType);

        GenericContext context = new(
            type: declaringType as GenericInstanceTypeSignature,
            method: null);

        // Follow explicit remaps before looking for implicit base slots with matching signatures
        foreach (MethodImplementation implementation in method.DeclaringType!.MethodImplementations)
        {
            if (implementation.Body?.TryResolve(_runtimeContext, out MethodDefinition? body) is true &&
                ReferenceEquals(body, method) &&
                implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? declaration) is true &&
                TryGetDeclaringSignature(implementation.Declaration, out TypeSignature? declaringSignature))
            {
                TypeSignature slotType = declaringSignature.InstantiateGenericTypes(context);

                foreach ((MethodDefinition slot, TypeSignature type) in GetSlots(declaration, slotType, visited))
                {
                    yield return (slot, type);
                }
            }
        }

        // Reused instance slots inherit the nearest matching base declaration; new or static slots do not
        if (!method.IsNewSlot && !method.IsStatic)
        {
            MethodSignature signature = method.Signature!.InstantiateGenericTypes(context);

            foreach (HierarchyType ancestor in GetOrAddHierarchy(method.DeclaringType).Skip(1).Where(static entry => !entry.Definition.IsInterface))
            {
                TypeSignature ancestorType = ancestor.Signature.InstantiateGenericTypes(context);
                MethodDefinition? declaration = ancestor.Definition.Methods.FirstOrDefault(candidate =>
                    candidate.IsVirtual && !candidate.IsStatic &&
                    candidate.Name == method.Name &&
                    candidate.Signature is { } candidateSignature &&
                    _comparer.Equals(signature, candidateSignature.InstantiateGenericTypes(new GenericContext(
                        type: ancestorType as GenericInstanceTypeSignature,
                        method: null))));

                if (declaration is not null)
                {
                    foreach ((MethodDefinition slot, TypeSignature type) in GetSlots(declaration, ancestorType, visited))
                    {
                        yield return (slot, type);
                    }

                    break;
                }
            }
        }

        _ = visited.Remove(method);
    }

    /// <summary>
    /// Adds a metadata binding, excluding abstract targets without forcing body deserialization.
    /// </summary>
    /// <param name="declaration">The declaration identity.</param>
    /// <param name="target">The body and receiver templates.</param>
    private void AddTarget(MethodDefinition declaration, Target target)
    {
        if (target.Method.IsAbstract || target.Method.DeclaringModule is not ModuleDefinition module || !_shouldProcessModule(module))
        {
            return;
        }

        if (!_targets.TryGetValue(declaration, out List<Target>? targets))
        {
            targets = [];
            _targets.Add(declaration, targets);
        }

        // Assign an ordinal only to distinct receiver/body/slot bindings so cutoff ordering stays stable
        if (!targets.Any(existing => ReferenceEquals(existing.Receiver, target.Receiver) &&
            ReferenceEquals(existing.Method, target.Method) &&
            _comparer.Equals(existing.DeclarationType, target.DeclarationType) &&
            _comparer.Equals(existing.BodyType, target.BodyType)))
        {
            targets.Add(target with { Ordinal = _targetOrdinal++ });
            _ = _bodyMethods.Add(target.Method);
        }

        if (target.Receiver.GenericParameters.Count > 0)
        {
            _ = _receivers.Add(target.Receiver);
        }
    }

    /// <summary>
    /// A possible implementation in its receiver's formal type context.
    /// </summary>
    /// <param name="Receiver">The receiver whose type arguments parameterize the binding.</param>
    /// <param name="DeclarationType">The slot's declaring type in receiver parameters.</param>
    /// <param name="Method">The body definition.</param>
    /// <param name="BodyType">The body's declaring type in receiver parameters.</param>
    public sealed record Target(
        TypeDefinition Receiver,
        TypeSignature DeclarationType,
        MethodDefinition Method,
        TypeSignature BodyType)
    {
        /// <summary>The binding's stable ordinal in the invocation metadata index.</summary>
        public int Ordinal { get; init; }
    }
}
