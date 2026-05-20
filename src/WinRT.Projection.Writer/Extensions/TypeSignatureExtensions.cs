// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionWriter.Resolvers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;
using static WindowsRuntime.ProjectionWriter.References.WellKnownTypeNames;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="TypeSignature"/>: shape predicates and signature-tree
/// peeling helpers that do not require the metadata cache.
/// </summary>
/// <remarks>
/// Predicates that need cross-module type resolution (e.g. <c>IsBlittablePrimitive</c>
/// with cross-module enum lookup, <c>IsAnyStruct</c>, <c>IsComplexStruct</c>) live in
/// <see cref="Helpers.AbiTypeHelpers"/> and the <see cref="AbiTypeKindResolver"/>;
/// they are intentionally not included here.
/// </remarks>
internal static class TypeSignatureExtensions
{
    extension(TypeSignature sig)
    {
        /// <summary>
        /// Returns whether the signature is the corlib <see cref="string"/> primitive.
        /// </summary>
        /// <returns><see langword="true"/> if the signature is <see cref="string"/>; otherwise <see langword="false"/>.</returns>
        public bool IsString()
        {
            return sig is CorLibTypeSignature corlib && corlib.ElementType == ElementType.String;
        }

        /// <summary>
        /// Returns whether the signature is the corlib <see cref="object"/> primitive.
        /// </summary>
        /// <returns><see langword="true"/> if the signature is <see cref="object"/>; otherwise <see langword="false"/>.</returns>
        public bool IsObject()
        {
            return sig is CorLibTypeSignature corlib && corlib.ElementType == ElementType.Object;
        }

        /// <summary>
        /// Returns whether the signature is <see cref="System.Type"/> (or a TypeRef/TypeSpec
        /// that resolves to it, including the WinRT <c>Windows.UI.Xaml.Interop.TypeName</c> struct
        /// that is mapped to it).
        /// </summary>
        /// <remarks>
        /// The <c>System.Type</c> branch is live (not dead, despite appearances): per the ECMA-335
        /// custom-attribute blob encoding rules, attribute constructor / field parameters typed as
        /// <c>Type</c> are emitted as TypeRefs to <c>System.Type</c> directly, not to the WinRT
        /// <c>Windows.UI.Xaml.Interop.TypeName</c> struct. The Windows Foundation metadata winmd
        /// (<c>ActivatableAttribute</c>, <c>ComposableAttribute</c>, <c>StyleTypedPropertyAttribute</c>,
        /// etc.) is the primary source of these signatures.
        /// </remarks>
        /// <returns><see langword="true"/> if the signature is the projected <see cref="System.Type"/>; otherwise <see langword="false"/>.</returns>
        public bool IsSystemType()
        {
            (string ns, string name) = sig.Names();

            return (ns == "System" && name == "Type") || (ns == WindowsUIXamlInterop && name == TypeName);
        }

        /// <summary>
        /// Returns whether the signature is the special <see cref="System.Exception"/> /
        /// <c>Windows.Foundation.HResult</c> pair (which uses an HResult struct as its ABI form
        /// and requires custom marshalling via <c>ABI.System.ExceptionMarshaller</c>).
        /// </summary>
        /// <returns><see langword="true"/> if the signature is the projected <c>HResult</c>/<see cref="System.Exception"/>; otherwise <see langword="false"/>.</returns>
        public bool IsHResultException()
        {
            (string ns, string name) = sig.Names();

            return ns == WindowsFoundation && name == HResult;
        }

        /// <summary>
        /// Returns whether the signature is a WinRT <c>IReference&lt;T&gt;</c> or a
        /// <see cref="System.Nullable{T}"/> instantiation (both project to <see cref="System.Nullable{T}"/> in C#).
        /// </summary>
        /// <returns><see langword="true"/> if the signature is a Nullable-shaped generic instantiation; otherwise <see langword="false"/>.</returns>
        public bool IsNullableT()
        {
            if (sig is not GenericInstanceTypeSignature gi)
            {
                return false;
            }

            (string ns, string name) = gi.GenericType.Names();

            return ns == WindowsFoundation && name == IReferenceGeneric;
        }

        /// <summary>
        /// Returns the single type argument of a generic instance type signature, or <see langword="null"/>
        /// if the signature is not a single-arg generic instance. Used to peel the inner <c>T</c> from
        /// <see cref="System.Nullable{T}"/> / <c>IReference&lt;T&gt;</c>.
        /// </summary>
        /// <returns>The inner type argument, or <see langword="null"/>.</returns>
        public TypeSignature? GetNullableInnerType()
        {
            if (sig is GenericInstanceTypeSignature { TypeArguments: [var arg] })
            {
                return arg;
            }

            return null;
        }

        /// <summary>
        /// Returns whether the signature is a generic instantiation (i.e. requires
        /// <c>WinRT.Interop</c> <c>UnsafeAccessor</c>-based marshalling).
        /// </summary>
        /// <returns><see langword="true"/> if the signature is a <see cref="GenericInstanceTypeSignature"/>; otherwise <see langword="false"/>.</returns>
        public bool IsGenericInstance()
        {
            return sig is GenericInstanceTypeSignature;
        }

        /// <summary>
        /// Returns whether the signature has the "ABI reference-pointer" shape: it crosses the
        /// ABI as a <c>void*</c> / <c>IInspectable*</c>. That covers <see cref="string"/> (HSTRING),
        /// any WinRT runtime class or interface (resolved via <paramref name="resolver"/>),
        /// <see cref="object"/>, and any generic instance (e.g. <c>IList&lt;T&gt;</c>).
        /// </summary>
        /// <param name="resolver">The active <see cref="AbiTypeKindResolver"/> (needed for runtime-class/interface resolution).</param>
        /// <returns><see langword="true"/> if the signature flows as a <c>void*</c> across the ABI; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// Use this variant for parameter/return types. For SZ-array element types (where generic
        /// instances cannot appear as elements), use <see cref="IsAbiArrayElementRefLike"/> instead.
        /// </remarks>
        public bool IsAbiRefLike(AbiTypeKindResolver resolver)
        {
            return
                sig.IsString() ||
                sig.IsObject() ||
                sig.IsGenericInstance() ||
                resolver.IsRuntimeClassOrInterface(sig);
        }

        /// <summary>
        /// Returns whether the signature has the "ABI array-element reference-pointer" shape:
        /// it crosses the ABI as a <c>void*</c> when carried as an element of an SZ-array.
        /// That covers <see cref="string"/>, WinRT runtime classes/interfaces, and
        /// <see cref="object"/> — the 3-way variant of <see cref="IsAbiRefLike"/>
        /// without the <c>IsGenericInstance</c> case, because generic instances cannot
        /// appear as array elements in metadata.
        /// </summary>
        /// <param name="resolver">The active <see cref="AbiTypeKindResolver"/> (needed for runtime-class/interface resolution).</param>
        /// <returns><see langword="true"/> if the signature flows as a <c>void*</c> when used as an SZ-array element; otherwise <see langword="false"/>.</returns>
        public bool IsAbiArrayElementRefLike(AbiTypeKindResolver resolver)
        {
            return
                sig.IsString() ||
                sig.IsObject() ||
                resolver.IsRuntimeClassOrInterface(sig);
        }

        /// <summary>
        /// Strips trailing <see cref="ByReferenceTypeSignature"/> and <see cref="CustomModifierTypeSignature"/>
        /// wrappers from the signature, returning the underlying signature.
        /// </summary>
        /// <returns>The underlying signature with byref + custom-modifier wrappers stripped.</returns>
        public TypeSignature StripByRefAndCustomModifiers()
        {
            TypeSignature current = sig;

            while (true)
            {
                if (current is ByReferenceTypeSignature br)
                {
                    current = br.BaseType;

                    continue;
                }

                if (current is CustomModifierTypeSignature cm)
                {
                    current = cm.BaseType;

                    continue;
                }

                return current;
            }
        }

        /// <summary>
        /// Strips byref + custom modifiers from the input signature and returns the result
        /// as an <see cref="SzArrayTypeSignature"/> if the underlying type is one; otherwise
        /// returns <see langword="null"/>.
        /// </summary>
        public SzArrayTypeSignature? AsSzArray()
        {
            return sig.StripByRefAndCustomModifiers() as SzArrayTypeSignature;
        }
    }

    extension([NotNullWhen(true)] TypeSignature? sig)
    {
        /// <summary>
        /// Returns whether the signature represents a by-reference type, peeling any
        /// custom-modifier wrappers (e.g. <c>modreq[InAttribute]</c>) before checking.
        /// </summary>
        /// <returns><see langword="true"/> if the signature (after peeling custom modifiers) is a <see cref="ByReferenceTypeSignature"/>; otherwise <see langword="false"/>.</returns>
        public bool IsByRefType()
        {
            TypeSignature? cur = sig;

            while (cur is CustomModifierTypeSignature cm)
            {
                cur = cm.BaseType;
            }

            return cur is ByReferenceTypeSignature;
        }
    }
}