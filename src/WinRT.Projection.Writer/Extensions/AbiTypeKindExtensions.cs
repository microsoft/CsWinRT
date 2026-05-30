// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="AbiTypeKind"/>.
/// </summary>
internal static class AbiTypeKindExtensions
{
    /// <param name="kind">The input ABI type kind.</param>
    extension(AbiTypeKind kind)
    {
        /// <summary>
        /// Returns whether the shape is a reference-type marshalling kind: Windows Runtime classes/interfaces,
        /// delegates, generic instantiations, the corlib <see cref="object"/> primitive, or
        /// <see cref="System.Nullable{T}"/>/<c>IReference&lt;T&gt;</c> instantiations.
        /// </summary>
        public bool IsReferenceType()
        {
            return kind is AbiTypeKind.RuntimeClassOrInterface
                or AbiTypeKind.Delegate
                or AbiTypeKind.Object
                or AbiTypeKind.GenericInstance
                or AbiTypeKind.NullableT;
        }
    }
}
