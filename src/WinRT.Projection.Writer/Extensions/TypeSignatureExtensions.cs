// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
/// <see cref="Helpers.AbiTypeHelpers"/> and the <see cref="AbiTypeShapeResolver"/>;
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
        /// <returns><see langword="true"/> if the signature is the projected <see cref="System.Type"/>; otherwise <see langword="false"/>.</returns>
        public bool IsSystemType()
        {
            if (sig is TypeDefOrRefSignature td && td.Type is { } t)
            {
                (string ns, string name) = t.Names();

                if (ns == "System" && name == "Type")
                {
                    return true;
                }

                if (ns == WindowsUIXamlInterop && name == TypeName)
                {
                    return true;
                }
            }

            return false;
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

            string ns = gi.GenericType?.Namespace?.Value ?? string.Empty;
            string name = gi.GenericType?.Name?.Value ?? string.Empty;
            return (ns == WindowsFoundation && name == IReferenceGeneric)
                || (ns == "System" && name == NullableGeneric);
        }

        /// <summary>
        /// Returns the single type argument of a generic instance type signature, or <see langword="null"/>
        /// if the signature is not a single-arg generic instance. Used to peel the inner <c>T</c> from
        /// <see cref="System.Nullable{T}"/> / <c>IReference&lt;T&gt;</c>.
        /// </summary>
        /// <returns>The inner type argument, or <see langword="null"/>.</returns>
        public TypeSignature? GetNullableInnerType()
        {
            if (sig is GenericInstanceTypeSignature gi && gi.TypeArguments.Count == 1)
            {
                return gi.TypeArguments[0];
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
        /// Returns whether the signature is the special <see cref="System.Exception"/> /
        /// <c>Windows.Foundation.HResult</c> pair (which uses an HResult struct as its ABI form
        /// and requires custom marshalling via <c>ABI.System.ExceptionMarshaller</c>).
        /// </summary>
        /// <returns><see langword="true"/> if the signature is the projected <c>HResult</c>/<see cref="System.Exception"/>; otherwise <see langword="false"/>.</returns>
        public bool IsHResultException()
        {
            if (sig is not TypeDefOrRefSignature td || td.Type is null)
            {
                return false;
            }

            (string ns, string name) = td.Type.Names();
            return (ns == "System" && name == "Exception")
                || (ns == WindowsFoundation && name == HResult);
        }
    }

    extension(TypeSignature? sig)
    {
        /// <summary>
        /// Strips trailing <see cref="ByReferenceTypeSignature"/> and <see cref="CustomModifierTypeSignature"/>
        /// wrappers from the signature, returning the underlying signature (or <see langword="null"/>
        /// if the input is <see langword="null"/>).
        /// </summary>
        /// <returns>The underlying signature with byref + custom-modifier wrappers stripped.</returns>
        public TypeSignature? StripByRefAndCustomModifiers()
        {
            TypeSignature? cur = sig;
            while (true)
            {
                if (cur is CustomModifierTypeSignature cm)
                {
                    cur = cm.BaseType;
                    continue;
                }

                if (cur is ByReferenceTypeSignature br)
                {
                    cur = br.BaseType;
                    continue;
                }

                break;
            }
            return cur;
        }

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

        /// <summary>
        /// Returns whether the signature has the "ABI reference-pointer" shape: it crosses the
        /// ABI as a <c>void*</c> / <c>IInspectable*</c>. That covers <see cref="string"/> (HSTRING),
        /// any WinRT runtime class or interface (resolved via <paramref name="resolver"/>),
        /// <see cref="object"/>, and any generic instance (e.g. <c>IList&lt;T&gt;</c>).
        /// </summary>
        /// <param name="resolver">The active <see cref="AbiTypeShapeResolver"/> (needed for runtime-class/interface resolution).</param>
        /// <returns><see langword="true"/> if the signature flows as a <c>void*</c> across the ABI; otherwise <see langword="false"/>.</returns>
        /// <remarks>
        /// Use this variant for parameter/return types. For SZ-array element types (where generic
        /// instances cannot appear as elements), use <see cref="IsAbiArrayElementRefLike"/> instead.
        /// </remarks>
        public bool IsAbiRefLike(AbiTypeShapeResolver resolver)
        {
            return (sig is CorLibTypeSignature corlibStr && corlibStr.ElementType == ElementType.String)
                || resolver.IsRuntimeClassOrInterface(sig!)
                || (sig is CorLibTypeSignature corlibObj && corlibObj.ElementType == ElementType.Object)
                || sig is GenericInstanceTypeSignature;
        }

        /// <summary>
        /// Returns whether the signature has the "ABI array-element reference-pointer" shape:
        /// it crosses the ABI as a <c>void*</c> when carried as an element of an SZ-array.
        /// That covers <see cref="string"/>, WinRT runtime classes/interfaces, and
        /// <see cref="object"/> — the 3-way variant of <see cref="IsAbiRefLike"/>
        /// without the <c>IsGenericInstance</c> case, because generic instances cannot
        /// appear as array elements in metadata.
        /// </summary>
        /// <param name="resolver">The active <see cref="AbiTypeShapeResolver"/> (needed for runtime-class/interface resolution).</param>
        /// <returns><see langword="true"/> if the signature flows as a <c>void*</c> when used as an SZ-array element; otherwise <see langword="false"/>.</returns>
        public bool IsAbiArrayElementRefLike(AbiTypeShapeResolver resolver)
        {
            return (sig is CorLibTypeSignature corlibStr && corlibStr.ElementType == ElementType.String)
                || resolver.IsRuntimeClassOrInterface(sig!)
                || (sig is CorLibTypeSignature corlibObj && corlibObj.ElementType == ElementType.Object);
        }

        /// <summary>
        /// Strips byref + custom modifiers from the input signature and returns the result
        /// as an <see cref="SzArrayTypeSignature"/> if the underlying type is one; otherwise
        /// returns <see langword="null"/>.
        /// </summary>
        public SzArrayTypeSignature? AsSzArray()
        {
            return Helpers.AbiTypeHelpers.StripByRefAndCustomModifiers(sig!) as SzArrayTypeSignature;
        }

        /// <summary>
        /// Returns the element type of the underlying SZ-array (after stripping byref +
        /// custom modifiers), or <see langword="null"/> if the underlying type is not an
        /// <see cref="SzArrayTypeSignature"/>.
        /// </summary>
        public TypeSignature? SzArrayElement()
        {
            return (Helpers.AbiTypeHelpers.StripByRefAndCustomModifiers(sig!) as SzArrayTypeSignature)?.BaseType;
        }
    }
}