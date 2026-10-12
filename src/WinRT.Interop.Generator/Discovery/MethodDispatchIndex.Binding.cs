// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="MethodDispatchIndex"/>
internal partial class MethodDispatchIndex
{
    /// <summary>
    /// Binds a target's receiver parameters to an observed declaration and, optionally, a real receiver context.
    /// </summary>
    /// <param name="target">The metadata relationship to bind.</param>
    /// <param name="declarationType">The observed slot's declaring context.</param>
    /// <param name="receiverType">An actually discovered receiver context, if available.</param>
    /// <param name="bodyType">The resulting implementation declaring context.</param>
    /// <returns>Whether the relationship is compatible with the observed contexts.</returns>
    public bool TryBind(Target target, TypeSignature declarationType, TypeSignature? receiverType, out TypeSignature bodyType)
    {
        // Inference fills missing receiver arguments and variance checks commit tentative bindings
        // Keep this writable state separate from receiver metadata shared by parallel discovery.
        TypeSignature?[] bindings = new TypeSignature?[target.Receiver.GenericParameters.Count];

        if (receiverType is GenericInstanceTypeSignature receiver)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                bindings[i] = receiver.TypeArguments[i];
            }
        }

        bool matches;

        // Match the same generic declaration and arity before applying per-parameter variance rules
        if (target.DeclarationType is GenericInstanceTypeSignature pattern &&
            declarationType is GenericInstanceTypeSignature actual &&
            _comparer.Equals(pattern.GenericType, actual.GenericType) &&
            pattern.TypeArguments.Count == actual.TypeArguments.Count)
        {
            matches = true;
            _ = pattern.GenericType.TryResolve(_runtimeContext, out TypeDefinition? definition);

            for (int i = 0; i < pattern.TypeArguments.Count; i++)
            {
                // A call through IOuter<object> can dispatch to an implementation of IOuter<string>
                // Preserve observed receiver arguments when variance makes exact unification fail.
                if (definition?.GenericParameters[i].IsVariant == true)
                {
                    TypeSignature?[] candidateBindings = (TypeSignature?[])bindings.Clone();

                    if (TryUnify(pattern.TypeArguments[i], actual.TypeArguments[i], candidateBindings))
                    {
                        candidateBindings.CopyTo(bindings, 0);
                    }

                    continue;
                }

                matches &= TryUnify(pattern.TypeArguments[i], actual.TypeArguments[i], bindings);
            }
        }
        else
        {
            matches = TryUnify(target.DeclarationType, declarationType, bindings);
        }

        // Reconstruct the receiver context to substitute the implementation's declaring type
        // Keep unbound receiver parameters open rather than inventing type arguments.
        GenericInstanceTypeSignature? receiverContext = bindings.Length == 0 ? null : new GenericInstanceTypeSignature(
            genericType: target.Receiver,
            isValueType: target.Receiver.IsValueType,
            typeArguments: bindings.Select((binding, index) => binding ?? new GenericParameterSignature(
                parameterType: GenericParameterType.Type,
                index: index)));

        bodyType = target.BodyType.InstantiateGenericTypes(new GenericContext(
            type: receiverContext,
            method: null));

        return matches;
    }

    /// <summary>
    /// Checks a constrained receiver against a target's receiver hierarchy.
    /// </summary>
    /// <param name="target">The metadata relationship.</param>
    /// <param name="constraint">The substituted constrained receiver.</param>
    /// <returns>The corresponding receiver context, if compatible.</returns>
    public TypeSignature? GetConstrainedReceiver(Target target, TypeSignature constraint)
    {
        if (!constraint.TryResolve(_runtimeContext, out TypeDefinition? definition))
        {
            return null;
        }

        GenericContext context = new(
            type: constraint as GenericInstanceTypeSignature,
            method: null);

        // Project the constraint onto the implementation receiver, including inherited type arguments
        return GetHierarchy(definition)
            .FirstOrDefault(entry => ReferenceEquals(entry.Definition, target.Receiver))
            ?.Signature.InstantiateGenericTypes(context);
    }

    /// <summary>
    /// Unifies receiver type parameters structurally, preserving repeated parameters and custom modifiers.
    /// </summary>
    /// <param name="pattern">The signature in the receiver's formal parameters.</param>
    /// <param name="actual">The observed caller signature.</param>
    /// <param name="bindings">The receiver bindings accumulated so far.</param>
    /// <returns>Whether the signatures can share a receiver context.</returns>
    private bool TryUnify(TypeSignature pattern, TypeSignature actual, TypeSignature?[] bindings)
    {
        // Bind each receiver parameter once and require repeated occurrences to match the same actual type
        if (pattern is GenericParameterSignature { ParameterType: GenericParameterType.Type } parameter &&
            parameter.Index < bindings.Length)
        {
            if (bindings[parameter.Index] is TypeSignature binding)
            {
                return _comparer.Equals(binding, actual);
            }

            bindings[parameter.Index] = actual;

            return true;
        }

        // Unify matching signature shapes recursively, preserving array dimensions and custom modifiers
        return (pattern, actual) switch
        {
            (GenericInstanceTypeSignature left, GenericInstanceTypeSignature right) =>
                _comparer.Equals(left.GenericType, right.GenericType) &&
                left.TypeArguments.Count == right.TypeArguments.Count &&
                left.TypeArguments.Zip(right.TypeArguments).All(pair => TryUnify(pair.First, pair.Second, bindings)),
            (SzArrayTypeSignature left, SzArrayTypeSignature right) => TryUnify(left.BaseType, right.BaseType, bindings),
            (ArrayTypeSignature left, ArrayTypeSignature right) => left.Dimensions.SequenceEqual(right.Dimensions) && TryUnify(left.BaseType, right.BaseType, bindings),
            (ByReferenceTypeSignature left, ByReferenceTypeSignature right) => TryUnify(left.BaseType, right.BaseType, bindings),
            (PointerTypeSignature left, PointerTypeSignature right) => TryUnify(left.BaseType, right.BaseType, bindings),
            (CustomModifierTypeSignature left, CustomModifierTypeSignature right) =>
                left.IsRequired == right.IsRequired && _comparer.Equals(left.ModifierType, right.ModifierType) && TryUnify(left.BaseType, right.BaseType, bindings),
            _ => _comparer.Equals(pattern, actual)
        };
    }
}
