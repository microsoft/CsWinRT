// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable CS1734

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="ITypeDescriptor"/> type.
/// </summary>
internal static class ITypeDescriptorExtensions
{
    extension(ITypeDescriptor descriptor)
    {
        /// <summary>
        /// Gets a value indicating whether a given <see cref="ITypeDescriptor"/> instance can be fully resolved to type definitions.
        /// </summary>
        /// <param name="runtimeContext">The context to assume when resolving the type.</param>
        /// <param name="definition">The resulting <see cref="TypeDefinition"/>, if the type can be resolved.</param>
        /// <returns>Whether the type can be fully resolved.</returns>
        public bool IsFullyResolvable(RuntimeContext? runtimeContext, [NotNullWhen(true)] out TypeDefinition? definition)
        {
            // If this is a type signature, forward to the specialized extension.
            // That will also take care of generic instance type signatures.
            return descriptor is TypeSignature signature
                ? signature.IsFullyResolvable(runtimeContext, out definition)
                : descriptor.TryResolve(runtimeContext, out definition);
        }

        /// <summary>
        /// Atttempts to retrieve a <see cref="TypeSignature"/> from <paramref name="type"/>.
        /// </summary>
        /// <param name="context">The runtime context used to locate the type's assembly.</param>
        /// <param name="throwOnResolutionFailure">Whether to throw if <paramref name="type"/> has to be resolved and the operation fails.</param>
        /// <param name="typeSignature">The retrieved <see cref="TypeSignature"/> when this returns <see langword="true"/>, otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a <see cref="TypeSignature"/> was successfully retrieved, otherwise <see langword="false"/>.</returns>
        public bool TryGetTypeSignature(
            RuntimeContext? context,
            bool throwOnResolutionFailure,
            [NotNullWhen(true)] out TypeSignature? typeSignature)
        {
            // Type specifications encode constructed generic types, and they contain
            // the constructed signature without the need to ever resolve types.
            if (descriptor is TypeSpecification { Signature: { } signature })
            {
                typeSignature = signature;

                return true;
            }

            // If we already have a type definition, just return the signature from it directly.
            // We don't need to further resolve it, so possible unresolved base types don't matter.
            if (descriptor is TypeDefinition definition)
            {
                typeSignature = definition.ToTypeSignature(definition.IsValueType);

                return true;
            }

            // If we should throw on resolution failure, just delegate to the built-in helper.
            // This will throw the most appropriate exception as well on resolution failures.
            if (throwOnResolutionFailure)
            {
                typeSignature = descriptor.ToTypeSignature(context);

                return true;
            }

            // Otherwise, try to resolve and create the signature manually if we succeeded
            if (descriptor.TryResolve(context, out TypeDefinition? resolvedDefinition))
            {
                typeSignature = resolvedDefinition.ToTypeSignature(resolvedDefinition.IsValueType);

                return true;
            }

            typeSignature = null;

            return false;
        }
    }

    extension<T>(IEnumerable<T> descriptors)
    {
        /// <summary>
        /// Sorts the values of a sequence in ascending order, based on the fully qualified type names of the selected key descriptors.
        /// </summary>
        /// <param name="keySelector">The selection function to retrieve <see cref="ITypeDescriptor"/> values to use for sorting.</param>
        /// <param name="runtimeContext">The context for ordering resolved type identities, or <see langword="null"/> for context-free lexical ordering.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptors"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object that stores all the
        /// information that is required to perform the action. The query represented by this method is not executed until the
        /// object is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        /// </remarks>
        public IEnumerable<T> OrderByFullyQualifiedTypeName<TKey>(Func<T, TKey> keySelector, RuntimeContext? runtimeContext)
            where TKey : class, ITypeDescriptor
        {
            return descriptors.OrderBy(keySelector, TypeDescriptorComparer.Create<TKey>(runtimeContext));
        }
    }

    extension<T>(IEnumerable<T> descriptors)
        where T : class, ITypeDescriptor
    {
        /// <summary>
        /// Sorts the <see cref="ITypeDescriptor"/> values of a sequence in ascending order, based on their fully qualified type names.
        /// </summary>
        /// <param name="runtimeContext">The context for ordering resolved type identities, or <see langword="null"/> for context-free lexical ordering.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptors"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object that stores all the
        /// information that is required to perform the action. The query represented by this method is not executed until the
        /// object is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        /// </remarks>
        public IEnumerable<T> OrderByFullyQualifiedTypeName(RuntimeContext? runtimeContext)
        {
            return descriptors.Order(TypeDescriptorComparer.Create<T>(runtimeContext));
        }
    }
}