// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Resolves the C# type names recorded by reference projections (e.g. <c>System.Collections.Generic.IList&lt;int&gt;</c>) to type symbols.
/// </summary>
/// <remarks>
/// Names are fully qualified, with no <c>global::</c> qualifier, aliases, nested types, arrays, or nullable annotations. Keywords
/// are used for built-in types. Generic arguments are only allowed on the last name, and are resolved recursively.
/// </remarks>
internal static class WindowsRuntimeTypeNameParser
{
    /// <summary>
    /// Tries to resolve a recorded type name to a type symbol.
    /// </summary>
    /// <param name="name">The recorded type name.</param>
    /// <param name="compilation">The consuming compilation.</param>
    /// <param name="assembly">The assembly the name was recorded for, which is searched first.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="type">The resolved type.</param>
    /// <returns>Whether <paramref name="name"/> identifies a type visible to <paramref name="compilation"/>.</returns>
    public static bool TryParse(
        string name,
        Compilation compilation,
        IAssemblySymbol assembly,
        CancellationToken token,
        [NotNullWhen(true)] out INamedTypeSymbol? type)
    {
        TypeSyntax syntax = SyntaxFactory.ParseTypeName(name);

        // Malformed names (e.g. truncated generic argument lists) are rejected instead of being partially resolved
        if (syntax.ContainsDiagnostics)
        {
            type = null;

            return false;
        }

        return TryResolve(syntax, compilation, assembly, token, out type);
    }

    /// <summary>
    /// Resolves a parsed type name to a type symbol.
    /// </summary>
    /// <inheritdoc cref="TryParse"/>
    private static bool TryResolve(
        TypeSyntax syntax,
        Compilation compilation,
        IAssemblySymbol assembly,
        CancellationToken token,
        [NotNullWhen(true)] out INamedTypeSymbol? type)
    {
        token.ThrowIfCancellationRequested();

        type = null;

        // Built-in types are written as keywords (e.g. 'int' or 'string'), which have no metadata name to look up
        if (syntax is PredefinedTypeSyntax predefined)
        {
            SpecialType? specialType = predefined.Keyword.ValueText switch
            {
                "bool" => SpecialType.System_Boolean,
                "char" => SpecialType.System_Char,
                "sbyte" => SpecialType.System_SByte,
                "byte" => SpecialType.System_Byte,
                "short" => SpecialType.System_Int16,
                "ushort" => SpecialType.System_UInt16,
                "int" => SpecialType.System_Int32,
                "uint" => SpecialType.System_UInt32,
                "long" => SpecialType.System_Int64,
                "ulong" => SpecialType.System_UInt64,
                "float" => SpecialType.System_Single,
                "double" => SpecialType.System_Double,
                "string" => SpecialType.System_String,
                "object" => SpecialType.System_Object,
                _ => null
            };

            // Only the keywords that can be Windows Runtime generic arguments are supported (anything else is rejected)
            if (specialType is not SpecialType value)
            {
                return false;
            }

            type = compilation.GetSpecialType(value);

            return true;
        }

        // Everything else is a (possibly qualified and generic) name, which we convert to its metadata name
        if (syntax is not NameSyntax name ||
            !TryGetMetadataName(name, out string? metadataName, out TypeArgumentListSyntax? typeArguments))
        {
            return false;
        }

        // Look in the assembly the class comes from first, as default interfaces are often internal to it
        INamedTypeSymbol? definition = assembly.GetTypeByMetadataName(metadataName) ?? compilation.GetTypeByMetadataName(metadataName);

        // The metadata name includes the arity, but check it anyway to make sure the arguments match the definition
        if (definition is null || definition.Arity != (typeArguments?.Arguments.Count ?? 0))
        {
            return false;
        }

        if (typeArguments is null)
        {
            type = definition;

            return true;
        }

        ITypeSymbol[] arguments = new ITypeSymbol[typeArguments.Arguments.Count];

        // Resolve each generic argument recursively, then close the definition over them
        for (int i = 0; i < arguments.Length; i++)
        {
            if (!TryResolve(typeArguments.Arguments[i], compilation, assembly, token, out INamedTypeSymbol? argument))
            {
                return false;
            }

            arguments[i] = argument;
        }

        type = definition.Construct(arguments);

        return true;
    }

    /// <summary>
    /// Gets the metadata name of a type name, whose generic arguments can only be on its last name.
    /// </summary>
    /// <param name="name">The type name.</param>
    /// <param name="metadataName">The metadata name, including the generic arity.</param>
    /// <param name="typeArguments">The generic arguments of the last name, if any.</param>
    /// <returns>Whether <paramref name="name"/> is a supported type name.</returns>
    private static bool TryGetMetadataName(
        NameSyntax name,
        [NotNullWhen(true)] out string? metadataName,
        out TypeArgumentListSyntax? typeArguments)
    {
        metadataName = null;
        typeArguments = null;

        switch (name)
        {
            // 'Namespace.Type': only the last name can be generic, so the left side must not have type arguments
            case QualifiedNameSyntax qualified:
                if (!TryGetMetadataName(qualified.Left, out string? left, out TypeArgumentListSyntax? leftTypeArguments) ||
                    leftTypeArguments is not null ||
                    !TryGetMetadataName(qualified.Right, out string? right, out typeArguments))
                {
                    return false;
                }

                metadataName = $"{left}.{right}";

                return true;

            // 'Type<A, B>': the metadata name has the arity as a suffix (e.g. 'IDictionary`2')
            case GenericNameSyntax generic:
                metadataName = $"{generic.Identifier.ValueText}`{generic.Arity}";
                typeArguments = generic.TypeArgumentList;

                return true;

            // A simple name, which is either a type in the global namespace or the left side of a qualified name
            case IdentifierNameSyntax identifier:
                metadataName = identifier.Identifier.ValueText;

                return true;
            default:
                return false;
        }
    }
}
