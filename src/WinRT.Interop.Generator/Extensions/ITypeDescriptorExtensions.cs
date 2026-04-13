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
    }

    extension<T>(IEnumerable<T> descriptors)
    {
        /// <summary>
        /// Sorts the values of a sequence in ascending order, based on the fully qualified type names of the selected key descriptors.
        /// </summary>
        /// <param name="keySelector">The selection function to retrieve <see cref="ITypeDescriptor"/> values to use for sorting.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptors"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object that stores all the
        /// information that is required to perform the action. The query represented by this method is not executed until the
        /// object is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        /// </remarks>
        public IEnumerable<T> OrderByFullyQualifiedTypeName<TKey>(Func<T, TKey> keySelector)
            where TKey : class, ITypeDescriptor
        {
            return descriptors.OrderBy(keySelector, TypeDescriptorComparer.Create<TKey>());
        }
    }

    extension<T>(IEnumerable<T> descriptors)
        where T : class, ITypeDescriptor
    {
        /// <summary>
        /// Sorts the <see cref="ITypeDescriptor"/> values of a sequence in ascending order, based on their fully qualified type names.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="descriptors"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method is implemented by using deferred execution. The immediate return value is an object that stores all the
        /// information that is required to perform the action. The query represented by this method is not executed until the
        /// object is enumerated by calling its <see cref="IEnumerable{T}.GetEnumerator"/> method.
        /// </remarks>
        public IEnumerable<T> OrderByFullyQualifiedTypeName()
        {
            return descriptors.Order(TypeDescriptorComparer.Create<T>());
        }
    }
}