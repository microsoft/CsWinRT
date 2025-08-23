// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for <see cref="PropertySignature"/>.
/// </summary>
internal static class PropertySignatureExtensions
{
    extension(PropertySignature)
    {
        /// <summary>
        /// Creates a <see cref="PropertySignature"/> from a <see cref="MethodDefinition"/> that represents a property getter.
        /// </summary>
        /// <param name="method">The input get method definition.</param>
        /// <returns>The resulting <see cref="PropertySignature"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="method"/> has no signature.</exception>
        public static PropertySignature FromGetMethod(MethodDefinition method)
        {
            if (method.Signature is not MethodSignature signature)
            {
                throw new ArgumentException("The input method does not have a signature.", nameof(method));
            }

            return FromGetMethodSignature(signature);
        }

        /// <summary>
        /// Creates a <see cref="PropertySignature"/> from a <see cref="MethodSignature"/> that represents a property getter.
        /// </summary>
        /// <param name="signature">The input get method signature.</param>
        /// <returns>The resulting <see cref="PropertySignature"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="signature"/> is generic.</exception>
        public static PropertySignature FromGetMethodSignature(MethodSignature signature)
        {
            if (signature.IsGeneric || signature.IsGenericInstance)
            {
                throw new ArgumentException("Generic signatures are not valid.", nameof(signature));
            }

            return signature.HasThis switch
            {
                true => PropertySignature.CreateInstance(signature.ReturnType, signature.ParameterTypes),
                false => PropertySignature.CreateStatic(signature.ReturnType, signature.ParameterTypes)
            };
        }
    }
}
