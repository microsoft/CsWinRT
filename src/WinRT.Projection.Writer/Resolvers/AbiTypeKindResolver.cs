// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter.Resolvers;

/// <summary>
/// Classifies WinRT type signatures by their ABI marshalling shape (see <see cref="AbiTypeKind"/>).
/// </summary>
/// <remarks>
/// The resolver is constructed with a reference to the <see cref="MetadataCache"/> so it can
/// perform cross-module type resolution (e.g. resolving an enum that lives in a different
/// reference assembly than the one currently being projected). It is the single semantic
/// entry point for "what's the shape of this type at the ABI?" — emission paths consume the
/// resolver's classification rather than reaching for the per-shape predicates directly.
/// </remarks>
/// <param name="cache">The metadata cache used for cross-module type resolution.</param>
internal sealed class AbiTypeKindResolver(MetadataCache cache)
{
    /// <summary>
    /// Gets the metadata cache used for cross-module type resolution.
    /// </summary>
    public MetadataCache Cache { get; } = cache;

    /// <summary>
    /// Classifies <paramref name="signature"/> by its WinRT ABI marshalling shape.
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns>The <see cref="AbiTypeKind"/> describing the signature's ABI shape; <see cref="AbiTypeKind.Unknown"/> for signatures that don't match any known WinRT marshalling shape.</returns>
    public AbiTypeKind Resolve(TypeSignature signature)
    {
        // Cheap top-level shape checks that don't need the cache
        if (signature.IsString())
        {
            return AbiTypeKind.String;
        }

        if (signature.IsObject())
        {
            return AbiTypeKind.Object;
        }

        if (signature.IsHResultException())
        {
            return AbiTypeKind.HResultException;
        }

        if (signature.IsSystemType())
        {
            return AbiTypeKind.SystemType;
        }

        if (signature.IsNullableT())
        {
            return AbiTypeKind.NullableT;
        }

        if (signature is SzArrayTypeSignature)
        {
            return AbiTypeKind.Array;
        }

        if (signature.IsGenericInstance())
        {
            return AbiTypeKind.GenericInstance;
        }

        // Cache-aware classifications. These are evaluated in the same order the writer's
        // emission paths historically queried the inline AbiTypeHelpers predicates so the
        // resolver returns the same shape the legacy code path would have inferred.
        if (AbiTypeHelpers.IsMappedAbiValueType(signature))
        {
            return AbiTypeKind.MappedAbiValueType;
        }

        if (AbiTypeHelpers.IsBlittablePrimitive(Cache, signature))
        {
            return signature is CorLibTypeSignature
                ? AbiTypeKind.BlittablePrimitive
                : AbiTypeKind.Enum;
        }

        if (AbiTypeHelpers.IsNonBlittableStruct(Cache, signature))
        {
            return AbiTypeKind.NonBlittableStruct;
        }

        if (AbiTypeHelpers.IsBlittableStruct(Cache, signature))
        {
            return AbiTypeKind.BlittableStruct;
        }

        if (AbiTypeHelpers.IsRuntimeClassOrInterface(Cache, signature))
        {
            if (signature is TypeDefOrRefSignature td &&
                td.Type is TypeDefinition def &&
                TypeCategorization.GetCategory(def) == TypeKind.Delegate)
            {
                return AbiTypeKind.Delegate;
            }

            return AbiTypeKind.RuntimeClassOrInterface;
        }

        return AbiTypeKind.Unknown;
    }

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a blittable WinRT primitive (the C# primitive
    /// types whose layout matches the WinRT ABI directly), OR a WinRT enum (whose ABI shape is its
    /// underlying integer primitive).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if blittable primitive or enum; otherwise <see langword="false"/>.</returns>
    public bool IsBlittablePrimitive(TypeSignature signature)
        => Resolve(signature) is AbiTypeKind.BlittablePrimitive or AbiTypeKind.Enum;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT enum (marshalled as its underlying integer).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if an enum; otherwise <see langword="false"/>.</returns>
    public bool IsEnumType(TypeSignature signature)
        => Resolve(signature) == AbiTypeKind.Enum;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT struct that flows across the ABI by value
    /// without per-field marshalling (i.e. blittable struct, including bool/char fields).
    /// Complex structs (with reference fields requiring per-field marshalling) are <b>not</b> included.
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a blittable struct; otherwise <see langword="false"/>.</returns>
    public bool IsBlittableStruct(TypeSignature signature)
        => Resolve(signature) == AbiTypeKind.BlittableStruct;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT struct that has at least one reference-type
    /// field and therefore requires per-field marshalling via a <c>*Marshaller</c> class.
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a complex struct; otherwise <see langword="false"/>.</returns>
    public bool IsNonBlittableStruct(TypeSignature signature)
        => Resolve(signature) == AbiTypeKind.NonBlittableStruct;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a WinRT runtime class, interface, or delegate
    /// (i.e. flows across the ABI as <c>IInspectable*</c>).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a class / interface / delegate; otherwise <see langword="false"/>.</returns>
    public bool IsRuntimeClassOrInterface(TypeSignature signature)
        => Resolve(signature) is AbiTypeKind.RuntimeClassOrInterface or AbiTypeKind.Delegate;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a reference type that crosses the ABI as
    /// an opaque pointer — either a runtime class / interface / delegate, the base <see cref="object"/>
    /// type, or any closed generic instance.
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if a reference type or generic instance; otherwise <see langword="false"/>.</returns>
    public bool IsReferenceTypeOrGenericInstance(TypeSignature signature)
        => IsRuntimeClassOrInterface(signature) || signature.IsObject() || signature.IsGenericInstance();

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a mapped value type that needs ABI-specific
    /// marshalling (<c>Windows.Foundation.DateTime</c>, <c>Windows.Foundation.TimeSpan</c>).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> if mapped; otherwise <see langword="false"/>.</returns>
    public bool IsMappedAbiValueType(TypeSignature signature)
        => Resolve(signature) == AbiTypeKind.MappedAbiValueType;

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a blittable element shape suitable for
    /// direct pinning when carried as an SZ-array element (a blittable primitive or a blittable struct).
    /// </summary>
    /// <param name="signature">The type signature to classify.</param>
    /// <returns><see langword="true"/> when the type is blittable in the array-element sense; otherwise <see langword="false"/>.</returns>
    public bool IsBlittableAbiElement(TypeSignature signature)
        => IsBlittablePrimitive(signature) || IsBlittableStruct(signature);

    /// <summary>
    /// Returns whether <paramref name="signature"/> is an SZ-array element shape that flows across the
    /// ABI without per-element marshalling — either a blittable element (blittable primitive or
    /// blittable struct, see <see cref="IsBlittableAbiElement"/>) or a mapped value type (e.g.
    /// WinRT <c>DateTime</c> -&gt; <see cref="System.DateTimeOffset"/>, see <see cref="IsMappedAbiValueType"/>).
    /// Used to gate "do we need to walk each element on input?" decisions in the array-pass paths.
    /// </summary>
    /// <param name="signature">The element type to classify.</param>
    /// <returns><see langword="true"/> when the element can pass directly; otherwise <see langword="false"/>.</returns>
    public bool IsDirectPassArrayElement(TypeSignature signature)
        => IsBlittableAbiElement(signature) || IsMappedAbiValueType(signature);

    /// <summary>
    /// Returns whether <paramref name="signature"/> is a recognised SZ-array element shape for which
    /// the projection has an ABI marshalling story. This is the union of all five concrete element
    /// shapes that the receive-array paths know how to handle:
    /// <list type="bullet">
    ///   <item>Blittable element (primitive or blittable struct) — see <see cref="IsBlittableAbiElement"/></item>
    ///   <item>Ref-like (string / runtime class / interface / object) — see <see cref="TypeSignatureExtensions.IsAbiArrayElementRefLike"/></item>
    ///   <item>Complex struct — see <see cref="IsNonBlittableStruct"/></item>
    ///   <item><see cref="System.Exception"/> (mapped from WinRT <c>HResult</c>) — see <see cref="TypeSignatureExtensions.IsHResultException"/></item>
    ///   <item>Mapped value type (e.g. WinRT <c>DateTime</c> / <c>TimeSpan</c>) — see <see cref="IsMappedAbiValueType"/></item>
    /// </list>
    /// Element types outside this set (e.g. generic instances, <see cref="System.Type"/>, unresolved
    /// references) cannot be returned as a receive-array.
    /// </summary>
    /// <param name="signature">The element type to classify.</param>
    /// <returns><see langword="true"/> when the element is one of the recognised receive-array shapes; otherwise <see langword="false"/>.</returns>
    public bool IsRecognizedReceiveArrayElement(TypeSignature signature)
        => IsBlittableAbiElement(signature)
            || signature.IsAbiArrayElementRefLike(this)
            || IsNonBlittableStruct(signature)
            || signature.IsHResultException()
            || IsMappedAbiValueType(signature);

    /// <summary>
    /// Returns whether <paramref name="signature"/> identifies a parameter type that, when received
    /// via an <c>Out</c> parameter, holds a resource which the caller must release in a <c>finally</c>
    /// block. This includes:
    /// <list type="bullet">
    ///   <item>SZ-array element ref-like types (HSTRING / IInspectable* slots) — see <see cref="TypeSignatureExtensions.IsAbiArrayElementRefLike"/></item>
    ///   <item><see cref="System.Type"/> (whose ABI form is an HSTRING that must be freed) — see <see cref="TypeSignatureExtensions.IsSystemType"/></item>
    ///   <item>Complex structs (per-field cleanup via the <c>*Marshaller</c>) — see <see cref="IsNonBlittableStruct"/></item>
    ///   <item>Generic instances (need <c>WindowsRuntimeObjectReferenceValue</c> dispose) — see <see cref="TypeSignatureExtensions.IsGenericInstance"/></item>
    /// </list>
    /// Callers should pass the already-stripped parameter type (via <see cref="TypeSignatureExtensions.StripByRefAndCustomModifiers"/>).
    /// </summary>
    /// <param name="signature">The stripped (non-byref) parameter type to classify.</param>
    /// <returns><see langword="true"/> when the type requires finally-block cleanup on receive; otherwise <see langword="false"/>.</returns>
    public bool RequiresOutParameterCleanup(TypeSignature signature)
        => signature.IsAbiArrayElementRefLike(this)
            || signature.IsSystemType()
            || IsNonBlittableStruct(signature)
            || signature.IsGenericInstance();

    /// <summary>
    /// Classifies <paramref name="elementType"/> into one of the six
    /// <see cref="AbiArrayElementKind"/> values used by the per-element pointer-type ladders.
    /// This is the discriminator-only form -- callers translate the kind into the per-site
    /// emission output.
    /// </summary>
    /// <param name="elementType">The SZ-array element type to classify.</param>
    /// <returns>The classification kind. <see cref="AbiArrayElementKind.BlittablePrimitive"/>
    /// is the default for shapes that don't match any of the other branches.</returns>
    public AbiArrayElementKind ClassifyArrayElement(TypeSignature elementType)
    {
        if (elementType.IsAbiArrayElementRefLike(this))
        {
            return AbiArrayElementKind.RefLikeVoidStar;
        }

        if (elementType.IsHResultException())
        {
            return AbiArrayElementKind.HResultException;
        }

        if (IsMappedAbiValueType(elementType))
        {
            return AbiArrayElementKind.MappedValueType;
        }

        if (IsNonBlittableStruct(elementType))
        {
            return AbiArrayElementKind.NonBlittableStruct;
        }

        if (IsBlittableStruct(elementType))
        {
            return AbiArrayElementKind.BlittableStruct;
        }

        return AbiArrayElementKind.BlittablePrimitive;
    }
}
