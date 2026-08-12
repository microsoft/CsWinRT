// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.Generator;

/// <summary>
/// Extensions for <see cref="TypeSignature"/>.
/// </summary>
internal static class TypeSignatureExtensions
{
    extension(TypeSignature signature)
    {
        /// <summary>
        /// Strips trailing <see cref="ByReferenceTypeSignature"/> and <see cref="CustomModifierTypeSignature"/>
        /// wrappers from the signature, returning the underlying signature.
        /// </summary>
        /// <returns>The underlying signature with byref + custom-modifier wrappers stripped.</returns>
        public TypeSignature StripByRefAndCustomModifiers()
        {
            TypeSignature current = signature;

            while (true)
            {
                if (current is ByReferenceTypeSignature byReferenceTypeSignature)
                {
                    current = byReferenceTypeSignature.BaseType;

                    continue;
                }

                if (current is CustomModifierTypeSignature customModifierTypeSignature)
                {
                    current = customModifierTypeSignature.BaseType;

                    continue;
                }

                return current;
            }
        }
    }
}
