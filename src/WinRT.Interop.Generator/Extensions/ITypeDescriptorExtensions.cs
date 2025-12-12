// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        /// <param name="definition">The resulting <see cref="TypeDefinition"/>, if the type can be resolved.</param>
        /// <returns>Whether the type can be fully resolved.</returns>
        public bool IsFullyResolvable([NotNullWhen(true)] out TypeDefinition? definition)
        {
            // If this is a type signature, forward to the specialized extension.
            // That will also take care of generic instance type signatures.
            if (descriptor is TypeSignature signature)
            {
                return signature.IsFullyResolvable(out definition);
            }

            definition = descriptor.Resolve();

            return definition is not null;
        }
    }

    extension<T>(IEnumerable<T> descriptors)
        where T : class, ITypeDescriptor
    {
        /// <summary>
        /// Sorts the <see cref="ITypeDescriptor"/> values of a sequence in ascending order, based on their fully qualified type names.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> whose elements are sorted.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="descriptors"/> is <see langword="null"/>.</exception>
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