// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="AbiTypeShapeKind"/>.
/// </summary>
internal static class AbiTypeShapeKindExtensions
{
    extension(AbiTypeShapeKind kind)
    {
        /// <summary>
        /// Returns whether the shape is a reference-type marshalling kind: WinRT runtime classes/interfaces,
        /// delegates, generic instantiations, the corlib <see cref="object"/> primitive, or
        /// <see cref="System.Nullable{T}"/>/<c>IReference&lt;T&gt;</c> instantiations.
        /// </summary>
        public bool IsReferenceType()
            => kind is AbiTypeShapeKind.RuntimeClassOrInterface
                or AbiTypeShapeKind.Delegate
                or AbiTypeShapeKind.Object
                or AbiTypeShapeKind.GenericInstance
                or AbiTypeShapeKind.NullableT;
    }
}
