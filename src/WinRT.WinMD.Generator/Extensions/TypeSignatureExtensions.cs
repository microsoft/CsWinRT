// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.WinMDGenerator;

/// <summary>
/// Extension methods for <see cref="TypeSignature"/>.
/// </summary>
internal static class TypeSignatureExtensions
{
    extension(TypeSignature signature)
    {
        /// <summary>
        /// Strips any leading custom modifiers (<c>modreq</c>/<c>modopt</c>) from the signature.
        /// </summary>
        /// <remarks>
        /// Custom modifiers carry no Windows Runtime meaning. They are emitted by the C# compiler in a few
        /// cases, most notably the <c>modreq(System.Runtime.InteropServices.InAttribute)</c> applied to the
        /// by-reference type of an <c>in</c> parameter on abstract, virtual, interface, or delegate members.
        /// </remarks>
        /// <returns>The underlying <see cref="TypeSignature"/> with all leading custom modifiers removed.</returns>
        public TypeSignature StripCustomModifiers()
        {
            TypeSignature current = signature;

            while (current is CustomModifierTypeSignature customModifier)
            {
                current = customModifier.BaseType;
            }

            return current;
        }

        /// <summary>
        /// Checks whether the signature is some <see cref="System.Span{T}"/> type.
        /// </summary>
        /// <returns><see langword="true"/> if the signature is <see cref="System.Span{T}"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsTypeOfSpan()
        {
            return signature is GenericInstanceTypeSignature { GenericType.FullName: "System.Span`1" };
        }

        /// <summary>
        /// Checks whether the signature is some <see cref="System.ReadOnlySpan{T}"/> type.
        /// </summary>
        /// <returns><see langword="true"/> if the signature is <see cref="System.ReadOnlySpan{T}"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsTypeOfReadOnlySpan()
        {
            return signature is GenericInstanceTypeSignature { GenericType.FullName: "System.ReadOnlySpan`1" };
        }
    }
}
