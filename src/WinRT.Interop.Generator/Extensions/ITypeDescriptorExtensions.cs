// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

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
}