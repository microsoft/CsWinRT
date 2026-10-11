// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <summary>
/// An invocation's read-only generic dispatch relationships, built without reading method bodies.
/// </summary>
internal sealed partial class MethodDispatchIndex
{
    /// <summary>The runtime context shared by the loaded input modules.</summary>
    private readonly RuntimeContext _runtimeContext;

    /// <summary>The resolution-aware comparer for signatures and substituted relationships.</summary>
    private readonly SignatureComparer _comparer;

    /// <summary>Possible implementations, indexed by declaration identity rather than method name.</summary>
    private readonly Dictionary<MethodDefinition, List<Target>> _targets = new(ReferenceEqualityComparer.Instance);

    /// <summary>Metadata hierarchies in each type's formal parameter context.</summary>
    private readonly Dictionary<TypeDefinition, HierarchyType[]> _hierarchies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Generic receiver definitions whose actual contexts can close dispatch bodies.</summary>
    private readonly HashSet<TypeDefinition> _receivers = new(ReferenceEqualityComparer.Instance);

    /// <summary>Only the hierarchy entries that can contribute receiver arguments to dispatch.</summary>
    private readonly Dictionary<TypeDefinition, HierarchyType[]> _receiverHierarchies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Whether a definition's metadata hierarchy contains any generic virtual declaration.</summary>
    private readonly Dictionary<TypeDefinition, bool> _hasGenericDispatch = new(ReferenceEqualityComparer.Instance);

    /// <summary>The stable ordinals of all loaded input modules.</summary>
    private readonly Dictionary<ModuleDefinition, int> _moduleOrdinals = new(ReferenceEqualityComparer.Instance);

    /// <summary>Methods that can be reached as indexed dispatch bodies.</summary>
    private readonly HashSet<MethodDefinition> _bodyMethods = new(ReferenceEqualityComparer.Instance);

    /// <summary>Whether bodies from a loaded module participate in transitive discovery.</summary>
    private readonly Func<ModuleDefinition, bool> _shouldProcessModule;

    /// <summary>The next stable binding ordinal.</summary>
    private int _targetOrdinal;

    /// <summary>The token checked while indexing metadata relationships.</summary>
    private readonly CancellationToken _token;

    /// <summary>Whether construction has finished and cached hierarchies can no longer be added.</summary>
    private readonly bool _isReadOnly;

    /// <summary>
    /// Builds the index after all input modules have been loaded.
    /// </summary>
    /// <param name="modules">The eligible input modules, in stable metadata order.</param>
    /// <param name="runtimeContext">The invocation's resolution context.</param>
    /// <param name="comparer">The invocation's signature comparer.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="shouldProcessModule">The policy for indexed implementation bodies.</param>
    public MethodDispatchIndex(IEnumerable<ModuleDefinition> modules, RuntimeContext runtimeContext, SignatureComparer comparer, CancellationToken token, Func<ModuleDefinition, bool> shouldProcessModule)
    {
        _runtimeContext = runtimeContext;
        _comparer = comparer;
        _token = token;
        _shouldProcessModule = shouldProcessModule;

        foreach (ModuleDefinition module in modules)
        {
            _moduleOrdinals.Add(module, _moduleOrdinals.Count);
            foreach (TypeDefinition type in module.GetAllTypes())
            {
                token.ThrowIfCancellationRequested();
                IndexType(type);
            }
        }

        foreach ((TypeDefinition type, HierarchyType[] hierarchy) in _hierarchies)
        {
            HierarchyType[] receivers = [.. hierarchy.Where(entry => _receivers.Contains(entry.Definition))];
            if (receivers.Length > 0)
            {
                _receiverHierarchies.Add(type, receivers);
            }
        }

        _isReadOnly = true;
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

        GenericContext context = new(signature as GenericInstanceTypeSignature, null);

        foreach (HierarchyType entry in hierarchy)
        {
            yield return entry.Signature.InstantiateGenericTypes(context);
        }
    }

    /// <summary>
    /// Indexes class slots, explicit MethodImpls, and implicit or inherited interface implementations.
    /// </summary>
    /// <param name="type">The receiver definition.</param>
    private void IndexType(TypeDefinition type)
    {
        // Most framework and projection types have no generic method slots. Do not compose their
        // generic hierarchies merely to establish that there are no dispatch bindings to index.
        if (!HasGenericDispatch(type))
        {
            return;
        }

        TypeSignature receiver = GetIdentitySignature(type);

        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsVirtual && method.Signature is { GenericParameterCount: > 0 })
            {
                foreach ((MethodDefinition declaration, TypeSignature declaringType) in GetSlots(method, receiver, new HashSet<MethodDefinition>(ReferenceEqualityComparer.Instance)))
                {
                    AddTarget(declaration, new Target(type, declaringType, method, receiver));
                }

            }
        }

        if (type.IsInterface)
        {
            return;
        }

        HierarchyType[] hierarchy = GetHierarchy(type);

        foreach (HierarchyType iface in hierarchy.Where(static entry => entry.Definition.IsInterface))
        {
            foreach (MethodDefinition declaration in iface.Definition.Methods)
            {
                _token.ThrowIfCancellationRequested();

                if (!declaration.IsVirtual || declaration.Signature is not { GenericParameterCount: > 0 })
                {
                    continue;
                }

                MethodSignature signature = declaration.Signature.InstantiateGenericTypes(new GenericContext(iface.Signature as GenericInstanceTypeSignature, null));

                foreach (HierarchyType owner in hierarchy)
                {
                    // Explicit implementations also cover covariant returns and default-interface overrides.
                    foreach (MethodImplementation implementation in owner.Definition.MethodImplementations)
                    {
                        if (implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? slot) is true &&
                            ReferenceEquals(slot, declaration) &&
                            implementation.Body?.TryResolve(_runtimeContext, out MethodDefinition? body) is true)
                        {
                            TypeSignature slotType = GetDeclaringSignature(implementation.Declaration).InstantiateGenericTypes(
                                new GenericContext(owner.Signature as GenericInstanceTypeSignature, null));

                            if (_comparer.Equals(slotType, iface.Signature))
                            {
                                AddTarget(declaration, new Target(type, iface.Signature, body, owner.Signature));
                            }
                        }
                    }

                    foreach (MethodDefinition body in owner.Definition.Methods)
                    {
                        if (owner.Definition.IsInterface)
                        {
                            if (ReferenceEquals(body, declaration) && !body.IsAbstract)
                            {
                                AddTarget(declaration, new Target(type, iface.Signature, body, owner.Signature));
                            }

                            continue;
                        }

                        if (body.IsPublic && body.IsStatic == declaration.IsStatic &&
                            body.Name == declaration.Name &&
                            body.Signature is { GenericParameterCount: > 0 } bodySignature &&
                            _comparer.Equals(signature, bodySignature.InstantiateGenericTypes(new GenericContext(owner.Signature as GenericInstanceTypeSignature, null))))
                        {
                            AddTarget(declaration, new Target(type, iface.Signature, body, owner.Signature));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks dispatch relevance using definition relationships only, without composing type signatures.
    /// </summary>
    /// <param name="type">The possible receiver definition.</param>
    /// <returns>Whether its base or interface graph contains a generic virtual declaration.</returns>
    private bool HasGenericDispatch(TypeDefinition type)
    {
        if (_hasGenericDispatch.TryGetValue(type, out bool cached))
        {
            return cached;
        }

        Stack<TypeDefinition> pending = new();
        HashSet<TypeDefinition> visited = new(ReferenceEqualityComparer.Instance);
        pending.Push(type);

        while (pending.TryPop(out TypeDefinition? current))
        {
            _token.ThrowIfCancellationRequested();

            if (!visited.Add(current))
            {
                continue;
            }

            if (_hasGenericDispatch.TryGetValue(current, out bool relevant))
            {
                if (relevant)
                {
                    _hasGenericDispatch[type] = true;
                    return true;
                }

                continue;
            }

            if (current.Methods.Any(static method => method.IsVirtual && method.Signature is { GenericParameterCount: > 0 }))
            {
                _hasGenericDispatch[type] = true;
                return true;
            }

            if (current.BaseType?.TryResolve(_runtimeContext, out TypeDefinition? baseType) is true)
            {
                pending.Push(baseType);
            }

            foreach (InterfaceImplementation implementation in current.Interfaces)
            {
                if (implementation.Interface?.TryResolve(_runtimeContext, out TypeDefinition? iface) is true)
                {
                    pending.Push(iface);
                }
            }
        }

        foreach (TypeDefinition definition in visited)
        {
            _hasGenericDispatch[definition] = false;
        }

        return false;
    }

    /// <summary>
    /// Follows explicit slot remaps and nearest matching reuse-slot declarations.
    /// </summary>
    /// <param name="method">The overriding or implementing method.</param>
    /// <param name="declaringType">Its declaring context in the receiver's type parameters.</param>
    /// <param name="visited">The declaration identities already on this path.</param>
    /// <returns>The declarations that dispatch to this method.</returns>
    private IEnumerable<(MethodDefinition, TypeSignature)> GetSlots(MethodDefinition method, TypeSignature declaringType, HashSet<MethodDefinition> visited)
    {
        _token.ThrowIfCancellationRequested();

        if (!visited.Add(method))
        {
            yield break;
        }

        yield return (method, declaringType);
        GenericContext context = new(declaringType as GenericInstanceTypeSignature, null);

        foreach (MethodImplementation implementation in method.DeclaringType!.MethodImplementations)
        {
            if (implementation.Body?.TryResolve(_runtimeContext, out MethodDefinition? body) is true &&
                ReferenceEquals(body, method) &&
                implementation.Declaration?.TryResolve(_runtimeContext, out MethodDefinition? declaration) is true)
            {
                TypeSignature slotType = GetDeclaringSignature(implementation.Declaration).InstantiateGenericTypes(context);

                foreach ((MethodDefinition slot, TypeSignature type) in GetSlots(declaration, slotType, visited))
                {
                    yield return (slot, type);
                }
            }
        }

        if (!method.IsNewSlot && !method.IsStatic)
        {
            MethodSignature signature = method.Signature!.InstantiateGenericTypes(context);

            foreach (HierarchyType ancestor in GetHierarchy(method.DeclaringType).Skip(1).Where(static entry => !entry.Definition.IsInterface))
            {
                TypeSignature ancestorType = ancestor.Signature.InstantiateGenericTypes(context);
                MethodDefinition? declaration = ancestor.Definition.Methods.FirstOrDefault(candidate =>
                    candidate.IsVirtual && !candidate.IsStatic &&
                    candidate.Name == method.Name &&
                    candidate.Signature is { } candidateSignature &&
                    _comparer.Equals(signature, candidateSignature.InstantiateGenericTypes(new GenericContext(ancestorType as GenericInstanceTypeSignature, null))));

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
    /// Retrieves the cached, substitution-aware base and interface hierarchy.
    /// </summary>
    /// <param name="type">The type definition.</param>
    /// <returns>The metadata hierarchy in depth-first order.</returns>
    private HierarchyType[] GetHierarchy(TypeDefinition type)
    {
        if (!_hierarchies.TryGetValue(type, out HierarchyType[]? hierarchy))
        {
            List<HierarchyType> entries = [];
            CollectHierarchy(GetIdentitySignature(type), entries, new HashSet<TypeDefinition>(ReferenceEqualityComparer.Instance));
            hierarchy = [.. entries];
            if (!_isReadOnly)
            {
                _hierarchies.Add(type, hierarchy);
            }
        }

        return hierarchy;
    }

    /// <summary>
    /// Collects metadata relationships, guarding malformed cycles by definition identity.
    /// </summary>
    /// <param name="signature">The current type template.</param>
    /// <param name="entries">The resulting hierarchy.</param>
    /// <param name="path">The definitions currently on the path.</param>
    private void CollectHierarchy(TypeSignature signature, List<HierarchyType> entries, HashSet<TypeDefinition> path)
    {
        _token.ThrowIfCancellationRequested();

        if (!signature.TryResolve(_runtimeContext, out TypeDefinition? definition) || !path.Add(definition))
        {
            return;
        }

        if (!entries.Any(entry => ReferenceEquals(entry.Definition, definition) && _comparer.Equals(entry.Signature, signature)))
        {
            entries.Add(new HierarchyType(definition, signature));
            GenericContext context = new(signature as GenericInstanceTypeSignature, null);

            if (definition.BaseType?.TryGetTypeSignature(_runtimeContext, false, out TypeSignature? baseType) is true)
            {
                CollectHierarchy(baseType.InstantiateGenericTypes(context), entries, path);
            }

            foreach (InterfaceImplementation implementation in definition.Interfaces)
            {
                if (implementation.Interface?.TryGetTypeSignature(_runtimeContext, false, out TypeSignature? iface) is true)
                {
                    CollectHierarchy(iface.InstantiateGenericTypes(context), entries, path);
                }
            }
        }

        _ = path.Remove(definition);
    }

    /// <summary>
    /// Gets a definition's formal declaring context, including nested-type parameters.
    /// </summary>
    /// <param name="type">The type definition.</param>
    /// <returns>The identity type signature.</returns>
    private static TypeSignature GetIdentitySignature(TypeDefinition type)
    {
        return type.GenericParameters.Count == 0
            ? type.ToTypeSignature()
            : new GenericInstanceTypeSignature(type, type.IsValueType,
                Enumerable.Range(0, type.GenericParameters.Count).Select(index => new GenericParameterSignature(GenericParameterType.Type, index)));
    }

    /// <summary>
    /// Gets the type context encoded on a declaration operand.
    /// </summary>
    /// <param name="method">The method operand.</param>
    /// <returns>Its declaring type signature.</returns>
    private TypeSignature GetDeclaringSignature(IMethodDefOrRef method)
    {
        return method.DeclaringType is TypeDefinition type
            ? GetIdentitySignature(type)
            : method.DeclaringType!.ToTypeSignature(_runtimeContext);
    }

    /// <summary>A possible implementation in its receiver's formal type context.</summary>
    /// <param name="Receiver">The receiver whose type arguments parameterize the binding.</param>
    /// <param name="DeclarationType">The slot's declaring type in receiver parameters.</param>
    /// <param name="Method">The body definition.</param>
    /// <param name="BodyType">The body's declaring type in receiver parameters.</param>
    internal sealed record Target(TypeDefinition Receiver, TypeSignature DeclarationType, MethodDefinition Method, TypeSignature BodyType)
    {
        /// <summary>The binding's stable ordinal in the invocation metadata index.</summary>
        public int Ordinal { get; init; }
    }

    /// <summary>A definition with its type signature in the starting type's parameters.</summary>
    /// <param name="Definition">The resolved definition.</param>
    /// <param name="Signature">The substituted type signature.</param>
    private sealed record HierarchyType(TypeDefinition Definition, TypeSignature Signature);
}
