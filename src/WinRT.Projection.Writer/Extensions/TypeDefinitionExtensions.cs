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
}
