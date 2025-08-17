// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Models;

/// <inheritdoc cref="TypeSignatureEquatableSet"/>
internal partial class TypeSignatureEquatableSet
{
    /// <summary>
    /// A builder type for <see cref="TypeSignatureEquatableSet"/> instances.
    /// </summary>
    public sealed class Builder
    {
        /// <summary>
        /// The underlying <see cref="TypeSignature"/> set.
        /// </summary>
        private HashSet<TypeSignature> _set;

        /// <summary>
        /// Creates a new <see cref="Builder"/> instance.
        /// </summary>
        public Builder()
        {
            _set = new HashSet<TypeSignature>(SignatureComparer.IgnoreVersion);
        }

        /// <summary>
        /// Creates a new <see cref="Builder"/> instance.
        /// </summary>
        /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
        public Builder(params ReadOnlySpan<TypeSignature> typeSignatures)
        {
            HashSet<TypeSignature> set = new(typeSignatures.Length, SignatureComparer.IgnoreVersion);

            foreach (TypeSignature typeSignature in typeSignatures)
            {
                _ = set.Add(typeSignature);
            }

            _set = set;
        }

        /// <summary>
        /// Creates a new <see cref="Builder"/> instance.
        /// </summary>
        /// <param name="typeSignatures">The input <see cref="TypeSignature"/>-s to wrap.</param>
        public Builder(params IEnumerable<TypeSignature> typeSignatures)
        {
            _set = new HashSet<TypeSignature>(typeSignatures, SignatureComparer.IgnoreVersion);
        }

        /// <summary>
        /// Adds a new <see cref="TypeSignature"/> value to the builder.
        /// </summary>
        /// <param name="typeSignature">The <see cref="TypeSignature"/> value to add.</param>
        public void Add(TypeSignature typeSignature)
        {
            _ = _set.Add(typeSignature);
        }

        /// <summary>
        /// Creates a new <see cref="TypeSignatureEquatableSet"/> instance from the current builder state.
        /// </summary>
        /// <returns>The resulting <see cref="TypeSignatureEquatableSet"/> instance.</returns>
        public TypeSignatureEquatableSet ToEquatableSet()
        {
            return new((IEnumerable<TypeSignature>)_set);
        }

        /// <summary>
        /// Extracts the internal set as a <see cref="TypeSignatureEquatableSet"/> and replaces it with an empty set.
        /// </summary>
        /// <returns>The resulting <see cref="TypeSignatureEquatableSet"/> instance.</returns>
        public TypeSignatureEquatableSet MoveToEquatableSet()
        {
            HashSet<TypeSignature> set = _set;

            _set = new HashSet<TypeSignature>(SignatureComparer.IgnoreVersion);

            return new(set);
        }
    }
}
