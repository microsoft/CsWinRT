// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="ITypeDefOrRef"/> type.
/// </summary>
internal static class ITypeDefOrRefExtensions
{
    extension(ITypeDefOrRef type)
    {
        /// <summary>
        /// Gets an <see cref="IMethodDefOrRef"/> instance from the input <see cref="ITypeDefOrRef"/> value.
        /// </summary>
        /// <param name="name">The name of the method to get.</param>
        /// <param name="signature">The signature of the member to reference.</param>
        /// <returns>The resulting <see cref="IMethodDefOrRef"/> instance.</returns>
        public IMethodDefOrRef GetMethodDefOrRef(Utf8String name, MemberSignature? signature)
        {
            return type switch
            {
                TypeDefinition typeDefinition => typeDefinition.GetMethod(name),
                TypeReference typeReference => typeReference.CreateMemberReference(name, signature),
                _ => throw new ArgumentException($"Invalid 'ITypeDefOrRef' implementation of type '{type}'.", nameof(type))
            };
        }

        /// <summary>
        /// Gets an <see cref="IMethodDefOrRef"/> instance from the input <see cref="ITypeDefOrRef"/> value.
        /// </summary>
        /// <param name="name">The name of the method to get.</param>
        /// <param name="signature">The signature of the member to reference.</param>
        /// <returns>The resulting <see cref="IMethodDefOrRef"/> instance.</returns>
        public IMethodDefOrRef GetMethodDefOrRef(ReadOnlySpan<byte> name, MemberSignature? signature)
        {
            return type switch
            {
                TypeDefinition typeDefinition => typeDefinition.GetMethod(name),
                TypeReference typeReference => typeReference.CreateMemberReference(name, signature),
                _ => throw new ArgumentException($"Invalid 'ITypeDefOrRef' implementation of type '{type}'.", nameof(type))
            };
        }
    }
}