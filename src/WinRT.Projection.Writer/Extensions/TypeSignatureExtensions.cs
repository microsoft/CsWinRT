// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="TypeSignature"/>: shape predicates and signature-tree
/// peeling helpers that do not require the metadata cache.
/// </summary>
/// <remarks>
/// Predicates that need cross-module type resolution (e.g. <c>IsBlittablePrimitive</c>
/// with cross-module enum lookup, <c>IsAnyStruct</c>, <c>IsComplexStruct</c>) live alongside
/// the ABI emission logic and will be migrated to a dedicated resolver in Pass 18 (ABI
/// marshalling shape analysis), so they are intentionally not included here.
/// </remarks>
internal static class TypeSignatureExtensions
{
    /// <summary>
    /// Returns whether <paramref name="sig"/> is the corlib <see cref="System.String"/> primitive.
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature is <c>System.String</c>; otherwise <see langword="false"/>.</returns>
    public static bool IsString(this TypeSignature sig)
    {
        return sig is CorLibTypeSignature corlib && corlib.ElementType == ElementType.String;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/> is the corlib <see cref="object"/> primitive.
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature is <c>System.Object</c>; otherwise <see langword="false"/>.</returns>
    public static bool IsObject(this TypeSignature sig)
    {
        return sig is CorLibTypeSignature corlib && corlib.ElementType == ElementType.Object;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/> is <see cref="System.Type"/> (or a TypeRef/TypeSpec
    /// that resolves to it, including the WinRT <c>Windows.UI.Xaml.Interop.TypeName</c> struct
    /// that is mapped to it).
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature is the projected <c>System.Type</c>; otherwise <see langword="false"/>.</returns>
    public static bool IsSystemType(this TypeSignature sig)
    {
        if (sig is TypeDefOrRefSignature td && td.Type is { } t)
        {
            (string ns, string name) = t.Names();
            if (ns == "System" && name == "Type") { return true; }
            // The WinMD source type for System.Type is Windows.UI.Xaml.Interop.TypeName.
            if (ns == "Windows.UI.Xaml.Interop" && name == "TypeName") { return true; }
        }
        return false;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/> is a WinRT <c>IReference&lt;T&gt;</c> or a
    /// <see cref="System.Nullable{T}"/> instantiation (both project to <c>Nullable&lt;T&gt;</c> in C#).
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature is a Nullable-shaped generic instantiation; otherwise <see langword="false"/>.</returns>
    public static bool IsNullableT(this TypeSignature sig)
    {
        if (sig is not GenericInstanceTypeSignature gi) { return false; }
        string ns = gi.GenericType?.Namespace?.Value ?? string.Empty;
        string name = gi.GenericType?.Name?.Value ?? string.Empty;
        return (ns == "Windows.Foundation" && name == "IReference`1")
            || (ns == "System" && name == "Nullable`1");
    }

    /// <summary>
    /// Returns the single type argument of a generic instance type signature, or <see langword="null"/>
    /// if the signature is not a single-arg generic instance. Used to peel the inner <c>T</c> from
    /// <c>Nullable&lt;T&gt;</c> / <c>IReference&lt;T&gt;</c>.
    /// </summary>
    /// <param name="sig">The type signature to peel.</param>
    /// <returns>The inner type argument, or <see langword="null"/>.</returns>
    public static TypeSignature? GetNullableInnerType(this TypeSignature sig)
    {
        if (sig is GenericInstanceTypeSignature gi && gi.TypeArguments.Count == 1)
        {
            return gi.TypeArguments[0];
        }
        return null;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/> is a generic instantiation (i.e. requires
    /// <c>WinRT.Interop</c> <c>UnsafeAccessor</c>-based marshalling).
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature is a <see cref="GenericInstanceTypeSignature"/>; otherwise <see langword="false"/>.</returns>
    public static bool IsGenericInstance(this TypeSignature sig)
    {
        return sig is GenericInstanceTypeSignature;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/> is the special <c>System.Exception</c> /
    /// <c>Windows.Foundation.HResult</c> pair (which uses an HResult struct as its ABI form
    /// and requires custom marshalling via <c>ABI.System.ExceptionMarshaller</c>).
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature is the projected <c>HResult</c>/<c>Exception</c>; otherwise <see langword="false"/>.</returns>
    public static bool IsHResultException(this TypeSignature sig)
    {
        if (sig is not TypeDefOrRefSignature td || td.Type is null) { return false; }
        (string ns, string name) = td.Type.Names();
        return (ns == "System" && name == "Exception")
            || (ns == "Windows.Foundation" && name == "HResult");
    }

    /// <summary>
    /// Strips trailing <see cref="ByReferenceTypeSignature"/> and <see cref="CustomModifierTypeSignature"/>
    /// wrappers from <paramref name="sig"/>, returning the underlying signature (or <see langword="null"/>
    /// if the input is <see langword="null"/>).
    /// </summary>
    /// <param name="sig">The type signature to peel.</param>
    /// <returns>The underlying signature with byref + custom-modifier wrappers stripped.</returns>
    public static TypeSignature? StripByRefAndCustomModifiers(this TypeSignature? sig)
    {
        TypeSignature? cur = sig;
        while (true)
        {
            if (cur is CustomModifierTypeSignature cm) { cur = cm.BaseType; continue; }
            if (cur is ByReferenceTypeSignature br) { cur = br.BaseType; continue; }
            break;
        }
        return cur;
    }

    /// <summary>
    /// Returns whether <paramref name="sig"/> represents a by-reference type, peeling any
    /// custom-modifier wrappers (e.g. <c>modreq[InAttribute]</c>) before checking.
    /// </summary>
    /// <param name="sig">The type signature to inspect.</param>
    /// <returns><see langword="true"/> if the signature (after peeling custom modifiers) is a <see cref="ByReferenceTypeSignature"/>; otherwise <see langword="false"/>.</returns>
    public static bool IsByRefType(this TypeSignature? sig)
    {
        TypeSignature? cur = sig;
        while (cur is CustomModifierTypeSignature cm)
        {
            cur = cm.BaseType;
        }
        return cur is ByReferenceTypeSignature;
    }
}
