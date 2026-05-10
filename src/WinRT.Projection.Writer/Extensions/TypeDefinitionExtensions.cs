// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// Extension methods for <see cref="TypeDefinition"/>.
/// </summary>
internal static class TypeDefinitionExtensions
{
    /// <summary>
    /// Returns the <c>[Default]</c> interface of <paramref name="type"/> (the interface whose
    /// vtable backs the type's <c>IInspectable</c> identity), or <see langword="null"/> if the
    /// type does not declare one.
    /// </summary>
    /// <param name="type">The runtime class type definition.</param>
    /// <returns>The default interface, or <see langword="null"/>.</returns>
    public static ITypeDefOrRef? GetDefaultInterface(this TypeDefinition type)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.IsDefaultInterface() && impl.Interface is not null)
            {
                return impl.Interface;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the <c>Invoke</c> method of a delegate type definition, or <see langword="null"/>
    /// if no such method exists.
    /// </summary>
    /// <param name="type">The delegate type definition.</param>
    /// <returns>The delegate's <c>Invoke</c> method, or <see langword="null"/>.</returns>
    public static MethodDefinition? GetDelegateInvoke(this TypeDefinition type)
    {
        foreach (MethodDefinition m in type.Methods)
        {
            if (m.IsSpecialName && m.Name == "Invoke")
            {
                return m;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns whether <paramref name="type"/> declares a parameterless instance constructor.
    /// </summary>
    /// <param name="type">The type definition to inspect.</param>
    /// <returns><see langword="true"/> if the type has a default constructor; otherwise <see langword="false"/>.</returns>
    public static bool HasDefaultConstructor(this TypeDefinition type)
    {
        foreach (MethodDefinition m in type.Methods)
        {
            if (m.IsRuntimeSpecialName && m.Name == ".ctor" && m.Parameters.Count == 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the second positional argument (a <see cref="uint"/>) of <c>[Windows.Foundation.Metadata.ContractVersionAttribute]</c>
    /// on <paramref name="type"/>, or <see langword="null"/> if the attribute is missing or the
    /// argument cannot be read.
    /// </summary>
    /// <param name="type">The type definition.</param>
    /// <returns>The contract version, or <see langword="null"/>.</returns>
    public static int? GetContractVersion(this TypeDefinition type)
    {
        CustomAttribute? attr = type.GetAttribute("Windows.Foundation.Metadata", "ContractVersionAttribute");
        if (attr is null) { return null; }
        // C++ reads index 1 (second positional arg).
        if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 1)
        {
            object? v = attr.Signature.FixedArguments[1].Element;
            if (v is uint u) { return (int)u; }
            if (v is int i) { return i; }
        }
        return null;
    }

    /// <summary>
    /// Returns the first positional argument (a <see cref="uint"/>) of <c>[Windows.Foundation.Metadata.VersionAttribute]</c>
    /// on <paramref name="type"/>, or <see langword="null"/> if the attribute is missing or the
    /// argument cannot be read.
    /// </summary>
    /// <param name="type">The type definition.</param>
    /// <returns>The version, or <see langword="null"/>.</returns>
    public static int? GetVersion(this TypeDefinition type)
    {
        CustomAttribute? attr = type.GetAttribute("Windows.Foundation.Metadata", "VersionAttribute");
        if (attr is null) { return null; }
        if (attr.Signature is not null && attr.Signature.FixedArguments.Count > 0)
        {
            object? v = attr.Signature.FixedArguments[0].Element;
            if (v is uint u) { return (int)u; }
            if (v is int i) { return i; }
        }
        return null;
    }
}