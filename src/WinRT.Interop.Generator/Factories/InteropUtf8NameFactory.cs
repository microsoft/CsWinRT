// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory for interop UTF8 type names.
/// </summary>
internal static class InteropUtf8NameFactory
{
    /// <summary>
    /// Gets the namespace for a generated interop type.
    /// </summary>
    /// <param name="typeSignature">The source <see cref="TypeSignature"/> for the type to generate.</param>
    /// <returns>The namespace to use.</returns>
    public static Utf8String TypeNamespace(TypeSignature typeSignature)
    {
        // Special case when there's no namespace: just put the generated types under 'ABI'.
        // Otherwise, we just add the "ABI." prefix to the namespace of the input type.
        // TODO: remove temporary allocation in .NET 10 via 'WrittenSpan'
        return typeSignature.Namespace?.Length is not > 0 ? new("ABI"u8) : new($"ABI.{typeSignature.Namespace}");
    }

    public static Utf8String NameOrWellKnownIdentifier(AssemblyDefinition assemblyDefinition)
    {
        Utf8String originalAssemblyNameUtf8 = assemblyDefinition.Name!;

        // Replace some assembly names with well known constants, to make the names more compact
        return originalAssemblyNameUtf8.Value switch
        {
            "System.Runtime" or "System.Private.CoreLib" => "#corlib"u8,
            "Microsoft.Windows.SDK.NET" or "Microsoft.Windows.UI.Xaml" => "#Windows"u8,
            "Microsoft.UI.Xaml.Projection" => "<>WinUI2"u8,
            "Microsoft.Graphics.Canvas.Interop" => "<>Win2D"u8,
            _ => originalAssemblyNameUtf8
        };
    }

    [UnconditionalSuppressMessage("Style", "IDE0072", Justification = "We only special case some known primitive types.")]
    public static Utf8String FullNameOrWellKnownIdentifier(TypeSignature typeSignature)
    {
        return TryGetWellKnownIdentifier(typeSignature, out ReadOnlySpan<byte> identifierUtf8)
            ? identifierUtf8
            : typeSignature.FullName!;
    }

    [UnconditionalSuppressMessage("Style", "IDE0072", Justification = "We only special case some known primitive types.")]
    public static bool TryGetWellKnownIdentifier(TypeSignature typeSignature, out ReadOnlySpan<byte> identifierUtf8)
    {
        if (typeSignature is CorLibTypeSignature corLibTypeSignature)
        {
            // If the type is a corlib type, we can use the well known identifier
            identifierUtf8 = corLibTypeSignature.ElementType switch
            {
                ElementType.Boolean => "bool"u8,
                ElementType.Char => "char"u8,
                ElementType.I1 => "sbyte"u8,
                ElementType.U1 => "byte"u8,
                ElementType.I2 => "short"u8,
                ElementType.U2 => "ushort"u8,
                ElementType.I4 => "int"u8,
                ElementType.U4 => "uint"u8,
                ElementType.I8 => "long"u8,
                ElementType.U8 => "ulong"u8,
                ElementType.R4 => "float"u8,
                ElementType.R8 => "double"u8,
                ElementType.String => "string"u8,
                ElementType.Object => "object"u8,
                _ => null
            };

            return !identifierUtf8.IsEmpty;
        }

        identifierUtf8 = [];

        return false;
    }

    /// <summary>
    /// Gets the name for a generated interop type.
    /// </summary>
    /// <param name="typeSignature">The source <see cref="TypeSignature"/> for the type to generate.</param>
    /// <param name="nameSuffix">The optional name suffix to use.</param>
    /// <returns>The name to use.</returns>
    public static Utf8String TypeName(TypeSignature typeSignature, string? nameSuffix = null)
    {
        DefaultInterpolatedStringHandler interpolatedStringHandler = new(literalLength: 2, formattedCount: 1);

        // Each type name uses this format: '<ASSEMBLY_NAME>TYPE_NAME'
        interpolatedStringHandler.AppendLiteral("<");
        interpolatedStringHandler.AppendFormatted(NameOrWellKnownIdentifier(typeSignature.Resolve()!.Module!.Assembly!));
        interpolatedStringHandler.AppendLiteral(">");

        // If the type is generic, append the definition name and the type arguments
        if (typeSignature is GenericInstanceTypeSignature genericInstanceTypeSignature)
        {
            interpolatedStringHandler.AppendFormatted(genericInstanceTypeSignature.GenericType.Name!.Value);
            interpolatedStringHandler.AppendLiteral("<");

            foreach ((int i, TypeSignature typeArgumentSignature) in genericInstanceTypeSignature.TypeArguments.Index())
            {
                // We use '|' to separate generic type arguments
                if (i > 0)
                {
                    interpolatedStringHandler.AppendLiteral("|");
                }

                // If the type argument has a well known identifier (eg. 'string'), just append it directly
                if (TryGetWellKnownIdentifier(typeArgumentSignature, out _))
                {
                    interpolatedStringHandler.AppendFormatted(FullNameOrWellKnownIdentifier(typeArgumentSignature));
                }
                else
                {
                    // Otherwise, append the type argument with the same format as the root type
                    interpolatedStringHandler.AppendLiteral("<");
                    interpolatedStringHandler.AppendFormatted(NameOrWellKnownIdentifier(typeArgumentSignature.Resolve()!.Module!.Assembly!));
                    interpolatedStringHandler.AppendLiteral(">");
                    interpolatedStringHandler.AppendFormatted(FullNameOrWellKnownIdentifier(typeArgumentSignature));
                }
            }

            interpolatedStringHandler.AppendLiteral(">");
        }
        else
        {
            // Otherwise just append the type name directly
            interpolatedStringHandler.AppendFormatted(FullNameOrWellKnownIdentifier(typeSignature));
        }

        // Append the suffix, if we have one
        interpolatedStringHandler.AppendFormatted(nameSuffix);

        // Create the final value, and also replace '.' characters with '-' instead
        // TODO: make this zero-alloc in .NET 10 using 'WrittenSpan'
        return new(interpolatedStringHandler.ToStringAndClear().Replace('.', '-'));
    }
}
