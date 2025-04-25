// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Runtime.CompilerServices;
using AsmResolver;
using AsmResolver.DotNet.Signatures;

namespace WinRT.Interop.Generator.Factories;

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
        interpolatedStringHandler.AppendFormatted(typeSignature.Resolve()!.Module!.Assembly!.Name!.Value);
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

                // Append the type argument with the same format as the root type
                interpolatedStringHandler.AppendLiteral("<");
                interpolatedStringHandler.AppendFormatted(typeArgumentSignature.Resolve()!.Module!.Assembly!.Name!.Value);
                interpolatedStringHandler.AppendLiteral(">");
                interpolatedStringHandler.AppendFormatted(typeArgumentSignature.Name!);
            }

            interpolatedStringHandler.AppendLiteral(">");
        }
        else
        {
            // Otherwise just append the type name directly
            interpolatedStringHandler.AppendFormatted(typeSignature.Name!);
        }

        // Append the suffix, if we have one
        interpolatedStringHandler.AppendFormatted(nameSuffix);

        // Create the final value, and also replace '.' characters with '-' instead
        // TODO: make this zero-alloc in .NET 10 using 'WrittenSpan'
        return new(interpolatedStringHandler.ToStringAndClear().Replace('.', '-'));
    }
}
