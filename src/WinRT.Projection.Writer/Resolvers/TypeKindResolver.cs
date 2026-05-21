// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;

namespace WindowsRuntime.ProjectionWriter.Resolvers;

/// <summary>
/// Classifies a <see cref="TypeDefinition"/> into one of the five fundamental WinRT
/// type kinds (<see cref="TypeKind"/>): class, interface, enum, struct, or delegate.
/// </summary>
internal static class TypeKindResolver
{
    /// <summary>
    /// Returns the <see cref="TypeKind"/> for <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type definition to classify.</param>
    /// <returns>The resolved <see cref="TypeKind"/>.</returns>
    public static TypeKind Resolve(TypeDefinition type)
    {
        return type switch
        {
            { IsInterface: true } => TypeKind.Interface,
            { IsEnum: true } => TypeKind.Enum,
            { IsValueType: true } => TypeKind.Struct,
            { IsDelegate: true } => TypeKind.Delegate,
            _ => TypeKind.Class
        };
    }
}
