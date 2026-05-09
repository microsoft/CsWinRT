// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter.Resolvers;

/// <summary>
/// Classifies WinRT type signatures by their ABI marshalling shape (see <see cref="AbiTypeShapeKind"/>).
/// Mirrors the classification logic that historically lived inline in the ABI emitters.
/// </summary>
/// <remarks>
/// <para>
/// The resolver is constructed with a reference to the <see cref="MetadataCache"/> so it can
/// perform cross-module type resolution (e.g. resolving an enum that lives in a different
/// reference assembly than the one currently being projected).
/// </para>
/// <para>
/// This is the long-term replacement for the cache-dependent inline predicates
/// (<c>IsBlittablePrimitive</c>, <c>IsAnyStruct</c>, <c>IsComplexStruct</c>, etc.) that
/// currently live as private static methods inside <c>AbiTypeHelpers.cs</c>. Migration of
/// callsites happens incrementally in subsequent commits within Pass 18.
/// </para>
/// </remarks>
/// <param name="cache">The metadata cache used for cross-module type resolution.</param>
internal sealed class AbiTypeShapeResolver(MetadataCache cache)
{
    /// <summary>Gets the metadata cache used for cross-module type resolution.</summary>
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

        // The richer cache-aware classification (BlittablePrimitive vs Enum vs BlittableStruct
        // vs ComplexStruct vs MappedAbiValueType vs RuntimeClassOrInterface vs Delegate) will
        // be folded in here as the inline predicates from AbiTypeHelpers.cs migrate over.
        // For now the resolver returns Unknown for those cases so callsites can fall through
        // to the legacy predicates without changing behavior.
        return AbiTypeShapeKind.Unknown;
    }
}
