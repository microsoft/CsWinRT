// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Discovery;

/// <inheritdoc cref="TypeSignatureDiscovery{TResult}"/>
internal partial class TypeSignatureDiscovery<TResult>
{
    /// <summary>
    /// Expands queued member contexts in discovery order.
    /// </summary>
    /// <returns>The signatures visible from instantiated members.</returns>
    private IEnumerable<TResult> EnumeratePendingMembers()
    {
        // A type context can close an ordinary method that has no 'MethodSpec' of its own:
        //
        // class C<T>
        // {
        //     List<T> M() => [];
        // }
        //
        // The open method definition only exposes 'List<T>'; a 'C<int>' 'TypeSpec' lets 'M()' reveal 'List<int>'
        // Closed types discovered through generic factories need the same traversal of ordinary methods
        // and cache initializers to expose nested property/indexer descriptors transitively.
        while (_pendingMembers.TryDequeue(out PendingMember current))
        {
            _token.ThrowIfCancellationRequested();

            if (current.Method is MethodDefinition currentMethod)
            {
                // Only supplemental traversal supersedes queued entries when it observes a shorter path
                if (_isSupplemental &&
                    current.Arguments is { } arguments)
                {
                    // Default bodies have no method arguments and use their own visited map
                    Dictionary<MethodDefinition, Dictionary<IList<TypeSignature>, int>> visitedMethods = current.Context.Method is null
                        ? _visitedDefaultBodies
                        : _visitedMethods;

                    if (visitedMethods[currentMethod][arguments] < current.Depth)
                    {
                        continue;
                    }
                }

                // A method item's context was already counted at its own depth; only its callees advance it
                foreach (TResult result in EnumerateMethodBodyTypes(currentMethod, current.Context, typeDepth: current.Depth, methodDepth: current.Depth + 1))
                {
                    yield return result;
                }

                continue;
            }

            TypeSignature typeSignature = current.Type!;

            if (_isSupplemental && _visitedTypes[typeSignature] < current.Depth)
            {
                continue;
            }

            if (!typeSignature.TryResolve(_module.RuntimeContext, out TypeDefinition? type))
            {
                continue;
            }

            GenericContext genericContext = new(
                type: typeSignature as GenericInstanceTypeSignature,
                method: null);

            _coveredTypes[typeSignature] = current.Depth;

            // The type's arguments can close member signatures that are open in the method table
            foreach (MethodDefinition method in GetMethodsToScan(typeSignature, type))
            {
                foreach (TResult result in EnumerateMethodBodyTypes(method, genericContext, typeDepth: current.Depth + 1, methodDepth: current.Depth + 1))
                {
                    yield return result;
                }
            }
        }
    }

    /// <summary>
    /// Queues generic member contexts and reports the selected signatures within a signature tree.
    /// </summary>
    /// <param name="type">The signature to visit, if available.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit root.</param>
    /// <returns>The signatures not yet reported by this enumeration.</returns>
    private IEnumerable<TResult> EnumerateTypeSignatures(TypeSignature? type, int depth = 0)
    {
        _token.ThrowIfCancellationRequested();

        // 'Node<Pair<T, T>>' can grow exponentially before reaching the depth limit
        // Bound complexity before visitors, hashing, or diagnostic formatting recursively walk the signature.
        if (!IsSignatureWithinComplexityLimit(type, depth: depth))
        {
            yield break;
        }

        EnqueueGenericTypes(type, depth: depth);
        RegisterReceiverContexts(type, depth: depth);

        foreach (TResult result in type?.AcceptVisitor(_visitor) ?? [])
        {
            if (_results.Add(result))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Composes a referenced method's context, reports its visible signatures, and queues eligible bodies.
    /// </summary>
    /// <param name="descriptor">The referenced method or method specification.</param>
    /// <param name="callerContext">The caller's declaring-type and method arguments.</param>
    /// <param name="depth">The expansion depth, or zero for an explicit 'MethodSpec' root.</param>
    /// <returns>The signatures visible after substituting the callee's context.</returns>
    private IEnumerable<TResult> EnumerateMethodTypes(IMethodDescriptor descriptor, GenericContext callerContext, int depth)
    {
        MethodSpecification? specification = descriptor as MethodSpecification;
        IMethodDefOrRef? method = specification is not null ? specification.Method : descriptor as IMethodDefOrRef;

        if (method is null)
        {
            yield break;
        }

        // The operand's generic parameters belong to the callee, not the currently scanned caller
        // Substitute the caller into its declaring type and method arguments first, then use
        // that composed context for the callee's signatures and body.
        TypeSignature? declaringType = (method.DeclaringType as TypeSpecification)?.Signature?.InstantiateGenericTypes(callerContext);
        GenericInstanceMethodSignature? methodArguments = specification?.Signature is { } signature
            ? new GenericInstanceMethodSignature(
                attributes: signature.Attributes,
                typeArguments: [.. signature.TypeArguments.Select(argument => argument.InstantiateGenericTypes(callerContext))])
            : null;

        // Many individually small arguments can collectively exceed the complexity limit
        // Bound the whole context before visiting or hashing any of its signatures.
        if (!IsComplexityWithinLimit(GetContextComplexity(declaringType, methodArguments?.TypeArguments), depth: depth))
        {
            yield break;
        }

        GenericContext genericContext = new(
            type: declaringType as GenericInstanceTypeSignature,
            method: methodArguments);

        // Method arguments can require marshalling even when the callee's signature omits them
        foreach (TypeSignature argument in methodArguments?.TypeArguments ?? [])
        {
            foreach (TResult result in EnumerateTypeSignatures(argument, depth: depth))
            {
                yield return result;
            }
        }

        // Closed declaring types provide contexts for otherwise non-generic member discovery
        foreach (TResult result in EnumerateTypeSignatures(declaringType, depth: depth))
        {
            yield return result;
        }

        // Referenced signatures remain visible even when module policy prevents following the body
        if (method.Signature is MethodSignature methodSignature)
        {
            TypeSignature[] visibleTypes = [methodSignature.ReturnType, .. methodSignature.ParameterTypes];

            // The reference signature remains usable even when the callee cannot be resolved
            foreach (TypeSignature visibleType in visibleTypes)
            {
                foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), depth: depth))
                {
                    yield return result;
                }
            }
        }

        // Queue the body after reporting its signatures, so their type contexts keep their place in the FIFO
        EnqueueMethodBody(method, declaringType, methodArguments, genericContext, depth: depth);
    }

    /// <summary>
    /// Visits a method's visible types and generic operands using an instantiated context.
    /// </summary>
    /// <param name="method">The method whose signature and body should be scanned.</param>
    /// <param name="genericContext">The context to substitute into visible types and referenced members.</param>
    /// <param name="typeDepth">The depth assigned to types found directly in the method.</param>
    /// <param name="methodDepth">The depth assigned to referenced method contexts.</param>
    /// <returns>The signatures visible from the instantiated method.</returns>
    private IEnumerable<TResult> EnumerateMethodBodyTypes(MethodDefinition method, GenericContext genericContext, int typeDepth, int methodDepth)
    {
        RecordMethodScan(method, genericContext, typeDepth);

        // Substitute the active context into every signature source shared with root discovery
        foreach (TypeSignature visibleType in method.EnumerateAllVisibleTypes(_module.RuntimeContext))
        {
            foreach (TResult result in EnumerateTypeSignatures(visibleType.InstantiateGenericTypes(genericContext), depth: typeDepth))
            {
                yield return result;
            }
        }

        // Propagate that context through generic call and dispatch operands
        foreach (MethodOperand operand in method.EnumerateMethodOperands())
        {
            IMethodDescriptor descriptor = operand.Descriptor;

            // Follow only calls carrying generic substitutions to avoid crawling ordinary dependency bodies
            if (descriptor is MethodSpecification || descriptor.DeclaringType is TypeSpecification)
            {
                foreach (TResult result in EnumerateMethodTypes(descriptor, genericContext, depth: methodDepth))
                {
                    yield return result;
                }

                RegisterDispatch(operand, genericContext, methodDepth);
            }
            else
            {
                // A non-generic static slot has no 'MethodSpec' or generic declaring type to trigger member propagation
                RecordStaticDispatch(operand, genericContext, depth: methodDepth);
            }
        }
    }

    /// <summary>
    /// Selects the same eligible members for both budget accounting and queued type expansion.
    /// </summary>
    /// <param name="typeSignature">The type context being expanded.</param>
    /// <param name="type">The resolved type definition.</param>
    /// <returns>The methods eligible for discovery in this context.</returns>
    private IEnumerable<MethodDefinition> GetMethodsToScan(TypeSignature typeSignature, TypeDefinition type)
    {
        // An explicit 'TypeSpec' supplies the caller's type arguments for a one-hop member scan,
        // even when its dependency module is skipped by policy; further traversal still
        // follows the configured assembly marshalling rules.
        if (_typeSpecifications.Contains(typeSignature) ||
            (type.DeclaringModule is ModuleDefinition declaringModule && _shouldProcessModule(declaringModule)))
        {
            return type.Methods;
        }

        // Mode-based skips still scan static initializers, but explicit exclusions do not
        if (type.DeclaringModule is ModuleDefinition excludedModule && _isMarshallingDisabledModule(excludedModule))
        {
            return [];
        }

        // Fields declared as 'object' or an interface hide the concrete type from their signatures
        // Static initializers can reveal the constructed instances and arrays stored in those caches.
        return type.TryGetStaticConstructor(out MethodDefinition? initializer) ? [initializer] : [];
    }
}
