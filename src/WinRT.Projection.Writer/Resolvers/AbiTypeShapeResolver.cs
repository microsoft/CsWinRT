// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter.Resolvers;

/// <summary>
/// Classifies WinRT type signatures by their ABI marshalling shape (see <see cref="AbiTypeShapeKind"/>).
/// </summary>
/// <remarks>
/// The resolver is constructed with a reference to the <see cref="MetadataCache"/> so it can
/// perform cross-module type resolution (e.g. resolving an enum that lives in a different
/// reference assembly than the one currently being projected).
/// </remarks>
/// <param name="cache">The metadata cache used for cross-module type resolution.</param>
internal sealed class AbiTypeShapeResolver(MetadataCache cache)
{
    /// <summary>
    /// Gets the metadata cache used for cross-module type resolution.
    /// </summary>
    public MetadataCache Cache { get; } = cache;

    /// <summary>
    /// Classifies <paramref name="signature"/> as an <see cref="AbiTypeShape"/>.
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns>The shape classification.</returns>
    public AbiTypeShape Resolve(TypeSignature signature)
    {
        AbiTypeShapeKind kind = ClassifyShape(signature);
        return new AbiTypeShape(kind, signature);
    }

    /// <summary>
    /// Inner classification routine. Returns the resolved <see cref="AbiTypeShapeKind"/> for
    /// <paramref name="signature"/>; returns <see cref="AbiTypeShapeKind.Unknown"/> when the
    /// signature does not match any known WinRT marshalling shape (typically because of an
    /// unresolved cross-module reference).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns>The classification kind.</returns>
    private AbiTypeShapeKind ClassifyShape(TypeSignature signature)
    {
        // Cheap top-level shape checks that don't need the cache.
        if (signature.IsString()) { return AbiTypeShapeKind.String; }
        if (signature.IsObject()) { return AbiTypeShapeKind.Object; }
        if (signature.IsHResultException()) { return AbiTypeShapeKind.HResultException; }
        if (signature.IsSystemType()) { return AbiTypeShapeKind.SystemType; }
        if (signature.IsNullableT()) { return AbiTypeShapeKind.NullableT; }
        if (signature is SzArrayTypeSignature) { return AbiTypeShapeKind.Array; }
        if (signature.IsGenericInstance()) { return AbiTypeShapeKind.GenericInstance; }

        // Cache-aware classifications. These are evaluated in the same order the writer's
        // emission paths historically queried the inline AbiTypeHelpers predicates so the
        // resolver returns the same shape the legacy code path would have inferred.
        if (AbiTypeHelpers.IsMappedAbiValueType(signature)) { return AbiTypeShapeKind.MappedAbiValueType; }
        if (AbiTypeHelpers.IsBlittablePrimitive(Cache, signature))
        {
            return signature is CorLibTypeSignature ? AbiTypeShapeKind.BlittablePrimitive : AbiTypeShapeKind.Enum;
        }
        if (AbiTypeHelpers.IsComplexStruct(Cache, signature)) { return AbiTypeShapeKind.ComplexStruct; }
        if (AbiTypeHelpers.IsAnyStruct(Cache, signature)) { return AbiTypeShapeKind.BlittableStruct; }
        if (AbiTypeHelpers.IsRuntimeClassOrInterface(Cache, signature))
        {
            if (signature is TypeDefOrRefSignature td &&
                td.Type is AsmResolver.DotNet.TypeDefinition def &&
                TypeCategorization.GetCategory(def) == TypeCategory.Delegate)
            {
                return AbiTypeShapeKind.Delegate;
            }
            return AbiTypeShapeKind.RuntimeClassOrInterface;
        }

        return AbiTypeShapeKind.Unknown;
    }
}
