// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
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
/// reference assembly than the one currently being projected). It is the single semantic
/// entry point for "what's the shape of this type at the ABI?" — emission paths consume the
/// resolver's classification rather than reaching for the per-shape predicates directly.
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
    /// Returns whether <paramref name="signature"/> is a blittable WinRT primitive (the C# primitive
    /// types whose layout matches the WinRT ABI directly).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if blittable; otherwise <see langword="false"/>.</returns>
    public bool IsBlittablePrimitive(TypeSignature signature)
        => Resolve(signature).Kind == AbiTypeShapeKind.BlittablePrimitive;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT enum (marshalled as its underlying integer).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if an enum; otherwise <see langword="false"/>.</returns>
    public bool IsEnumType(TypeSignature signature)
        => Resolve(signature).Kind == AbiTypeShapeKind.Enum;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is any WinRT struct that flows across the ABI by value
    /// (either a blittable struct or a complex struct that needs per-field marshalling).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a struct; otherwise <see langword="false"/>.</returns>
    public bool IsAnyStruct(TypeSignature signature)
        => Resolve(signature).Kind is AbiTypeShapeKind.BlittableStruct or AbiTypeShapeKind.ComplexStruct;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT struct that has at least one reference-type
    /// field and therefore requires per-field marshalling via a <c>*Marshaller</c> class.
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a complex struct; otherwise <see langword="false"/>.</returns>
    public bool IsComplexStruct(TypeSignature signature)
        => Resolve(signature).Kind == AbiTypeShapeKind.ComplexStruct;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT runtime class, interface, or delegate
    /// (i.e. flows across the ABI as <c>IInspectable*</c>).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a class / interface / delegate; otherwise <see langword="false"/>.</returns>
    public bool IsRuntimeClassOrInterface(TypeSignature signature)
        => Resolve(signature).Kind is AbiTypeShapeKind.RuntimeClassOrInterface or AbiTypeShapeKind.Delegate;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a mapped value type that needs ABI-specific
    /// marshalling (<c>Windows.Foundation.DateTime</c>, <c>Windows.Foundation.TimeSpan</c>).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if mapped; otherwise <see langword="false"/>.</returns>
    public bool IsMappedAbiValueType(TypeSignature signature)
        => Resolve(signature).Kind == AbiTypeShapeKind.MappedAbiValueType;

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
                td.Type is TypeDefinition def &&
                TypeCategorization.GetCategory(def) == TypeCategory.Delegate)
            {
                return AbiTypeShapeKind.Delegate;
            }
            return AbiTypeShapeKind.RuntimeClassOrInterface;
        }

        return AbiTypeShapeKind.Unknown;
    }
}
