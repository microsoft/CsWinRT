// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver;
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
        interpolatedStringHandler.AppendFormatted(AssemblyNameOrWellKnownIdentifier(typeSignature.Scope!.GetAssembly()!.Name));
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
                    interpolatedStringHandler.AppendFormatted(AssemblyNameOrWellKnownIdentifier(typeArgumentSignature.Scope!.GetAssembly()!.Name));
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

    /// <summary>
    /// Gets the assembly name or a well known identifier for the given assembly name.
    /// </summary>
    /// <param name="assemblyName">The input assembly name to convert.</param>
    /// <returns>The resulting assembly name to use.</returns>
    [return: NotNullIfNotNull(nameof(assemblyName))]
    private static Utf8String? AssemblyNameOrWellKnownIdentifier(Utf8String? assemblyName)
    {
        // Replace some assembly names with well known constants, to make the names more compact
        return assemblyName switch
        {
            { Value: "System.Runtime" } => "#corlib"u8,
            { Value: "Microsoft.Windows.SDK.NET" or "Microsoft.Windows.UI.Xaml" } => "#Windows"u8,
            { Value: "WinRT.Runtime" } => "#CsWinRT"u8,
            { Value: "Microsoft.UI.Xaml.Projection" } => "#WinUI2"u8,
            { Value: "Microsoft.Graphics.Canvas.Interop" } => "#Win2D"u8,
            _ => assemblyName
        };
    }

    /// <summary>
    /// Gets the full name or a well known identifier for the given type signature.
    /// </summary>
    /// <param name="typeSignature">The input type signature to convert.</param>
    /// <returns>The resulting type name to use.</returns>
    private static Utf8String FullNameOrWellKnownIdentifier(TypeSignature typeSignature)
    {
        return TryGetWellKnownIdentifier(typeSignature, out Utf8String? identifierUtf8)
            ? identifierUtf8
            : typeSignature.FullName!;
    }

    /// <summary>
    /// Tries to get the well known identifier for a given type signature.
    /// </summary>
    /// <param name="typeSignature">The input type signature.</param>
    /// <param name="identifier">The resulting well known identifier, if successfully retrieved.</param>
    /// <returns>Whether <paramref name="identifier"/> was successfully retrieved.</returns>
    [UnconditionalSuppressMessage("Style", "IDE0072", Justification = "We only special case some known primitive types.")]
    private static bool TryGetWellKnownIdentifier(TypeSignature typeSignature, [NotNullWhen(true)] out Utf8String? identifier)
    {
        if (typeSignature is CorLibTypeSignature corLibTypeSignature)
        {
            // If the type is a corlib type, we can use the well known identifier
            identifier = corLibTypeSignature.ElementType switch
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

            return identifier is not null;
        }

        identifier = null;

        return false;
    }
}
