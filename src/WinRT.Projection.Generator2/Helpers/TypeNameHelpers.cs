// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Helpers;

/// <summary>
/// Provides helpers for formatting type names in generated code.
/// </summary>
internal static class TypeNameHelpers
{
    /// <summary>
    /// Gets the C# keyword for a fundamental type (e.g., "bool", "int", "string").
    /// Returns <see langword="null"/> if the type does not have a corresponding C# keyword.
    /// </summary>
    public static string? GetCSharpTypeName(string fullTypeName)
    {
        return fullTypeName switch
        {
            "System.Boolean" => "bool",
            "System.Byte" => "byte",
            "System.SByte" => "sbyte",
            "System.Int16" => "short",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.UInt16" => "ushort",
            "System.UInt32" => "uint",
            "System.UInt64" => "ulong",
            "System.Single" => "float",
            "System.Double" => "double",
            "System.Char" => "char",
            "System.String" => "string",
            "System.Object" => "object",
            "System.Guid" => "global::System.Guid",
            "System.Void" => "void",
            _ => null
        };
    }

    /// <summary>
    /// Gets the .NET type name for a fundamental type (e.g., "Boolean", "Int32", "String").
    /// Returns <see langword="null"/> if the type does not have a corresponding .NET type name.
    /// </summary>
    public static string? GetDotNetTypeName(string fullTypeName)
    {
        return fullTypeName switch
        {
            "System.Boolean" => "Boolean",
            "System.Byte" => "Byte",
            "System.SByte" => "SByte",
            "System.Int16" => "Int16",
            "System.Int32" => "Int32",
            "System.Int64" => "Int64",
            "System.UInt16" => "UInt16",
            "System.UInt32" => "UInt32",
            "System.UInt64" => "UInt64",
            "System.Single" => "Single",
            "System.Double" => "Double",
            "System.Char" => "Char",
            "System.String" => "String",
            "System.Object" => "Object",
            "System.Guid" => "Guid",
            "System.Void" => "Void",
            _ => null
        };
    }

    /// <summary>
    /// Escapes special characters in a type name to make it a valid C# identifier.
    /// </summary>
    public static string EscapeTypeNameForIdentifier(string typeName)
    {
        return typeName
            .Replace(' ', '_')
            .Replace(':', '_')
            .Replace('<', '_')
            .Replace('>', '_')
            .Replace('`', '_')
            .Replace(',', '_')
            .Replace('.', '_');
    }

    /// <summary>
    /// Gets the fully qualified projected type name for a type reference.
    /// </summary>
    public static string GetProjectedTypeName(ITypeDefOrRef type)
    {
        if (type is TypeSpecification typeSpec)
        {
            string? fullName = typeSpec.FullName;

            return fullName is not null ? $"global::{fullName}" : "object";
        }

        string? ns = type.Namespace;
        string? name = type.Name;

        if (ns is not null && name is not null)
        {
            string simpleName = name.Contains('`') ? name[..name.IndexOf('`')] : name;

            return $"global::{ns}.{simpleName}";
        }

        return type.FullName ?? "object";
    }

    /// <summary>
    /// Gets the simple name of a type (without namespace or generic arity suffix).
    /// </summary>
    public static string GetSimpleName(TypeDefinition type)
    {
        string? name = type.Name;

        if (name is null)
        {
            return string.Empty;
        }

        // Remove generic arity suffix (e.g., "IList`1" → "IList")
        int backtickIndex = name.IndexOf('`');

        return backtickIndex >= 0 ? name[..backtickIndex] : name;
    }
}
