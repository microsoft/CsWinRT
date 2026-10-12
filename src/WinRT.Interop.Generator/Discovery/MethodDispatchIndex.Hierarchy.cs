// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

internal sealed partial class MethodDispatchIndex
{
    /// <summary>Metadata hierarchies in each type's formal parameter context.</summary>
    private readonly Dictionary<TypeDefinition, HierarchyType[]> _hierarchies = new(ReferenceEqualityComparer.Instance);

    /// <summary>Whether a definition's metadata hierarchy contains any generic virtual declaration.</summary>
    private readonly Dictionary<TypeDefinition, bool> _hasGenericDispatch = new(ReferenceEqualityComparer.Instance);

    /// <summary>Prevents cache writes once the completed index is shared by parallel enumerations.</summary>
    private readonly bool _isReadOnly;

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
            entries.Add(new HierarchyType(
                Definition: definition,
                Signature: signature));

            GenericContext context = new(
                type: signature as GenericInstanceTypeSignature,
                method: null);

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
            : new GenericInstanceTypeSignature(
                genericType: type,
                isValueType: type.IsValueType,
                typeArguments: Enumerable.Range(0, type.GenericParameters.Count).Select(index => new GenericParameterSignature(
                    parameterType: GenericParameterType.Type,
                    index: index)));
    }

    /// <summary>
    /// Gets the formal type context encoded on a declaration operand, if it can be resolved.
    /// </summary>
    /// <param name="method">The method operand.</param>
    /// <param name="signature">Its declaring type signature.</param>
    /// <returns>Whether the declaring context is available.</returns>
    private bool TryGetDeclaringSignature(IMethodDefOrRef method, [NotNullWhen(true)] out TypeSignature? signature)
    {
        if (method.DeclaringType is TypeDefinition type)
        {
            signature = GetIdentitySignature(type);

            return true;
        }

        signature = null;

        return method.DeclaringType?.TryGetTypeSignature(
            context: _runtimeContext,
            throwOnResolutionFailure: false,
            typeSignature: out signature) is true;
    }

    /// <summary>
    /// A definition with its type signature in the starting type's parameters.
    /// </summary>
    /// <param name="Definition">The resolved definition.</param>
    /// <param name="Signature">The substituted type signature.</param>
    private sealed record HierarchyType(TypeDefinition Definition, TypeSignature Signature);
}
