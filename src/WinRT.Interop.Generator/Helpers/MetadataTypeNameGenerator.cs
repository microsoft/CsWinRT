// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A generator for metadata type names of Windows Runtime types.
/// </summary>
internal static class MetadataTypeNameGenerator
{
    /// <summary>
    /// Generates the Windows Runtime metadata type name for a (potentially generic) type,
    /// applying known type-name mappings and recursively formatting generic arguments.
    /// </summary>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the Windows Runtime metadata type name for.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting Windows Runtime metadata type name for <paramref name="type"/>.</returns>
    public static string GetMetadataTyoeName(TypeSignature type, bool useWindowsUIXamlProjections)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        AppendMetadataTyoeName(ref handler, type, useWindowsUIXamlProjections);

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Appends the Windows Runtime metadata type name for a (potentially generic) type,
    /// applying known type-name mappings and recursively formatting generic arguments.
    /// </summary>
    /// <param name="interpolatedStringHandler">The <see cref="DefaultInterpolatedStringHandler"/> value to append to.</param>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the Windows Runtime metadata type name for.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting Windows Runtime metadata type name for <paramref name="type"/>.</returns>
    public static void AppendMetadataTyoeName(
        ref DefaultInterpolatedStringHandler interpolatedStringHandler,
        TypeSignature type,
        bool useWindowsUIXamlProjections)
    {
        // Handle SZ array types and map them to 'IReferenceArray<T>' type names
        if (type is SzArrayTypeSignature szArrayTypeSignature)
        {
            interpolatedStringHandler.AppendLiteral("Windows.Foundation.IReferenceArray`1<");

            AppendMetadataTyoeName(ref interpolatedStringHandler, szArrayTypeSignature.BaseType, useWindowsUIXamlProjections);

            interpolatedStringHandler.AppendLiteral(">");
        }
        else if (type is GenericInstanceTypeSignature genericInstanceTypeSignature)
        {
            // For constructed generic types, we first format the generic type (with a mapped
            // name, if applicable), and then recursively process all generic type arguments.
            if (TypeMapping.TryFindMappedTypeName(genericInstanceTypeSignature.GenericType.FullName, useWindowsUIXamlProjections, out string? mappedTypeName))
            {
                interpolatedStringHandler.AppendLiteral(mappedTypeName);
            }
            else
            {
                interpolatedStringHandler.AppendLiteral(genericInstanceTypeSignature.GenericType.FullName);
            }

            interpolatedStringHandler.AppendLiteral("<");

            // Recursively format each type argument
            for (int i = 0; i < genericInstanceTypeSignature.TypeArguments.Count; i++)
            {
                // Add the ', ' separator after the first type argument
                if (i > 0)
                {
                    interpolatedStringHandler.AppendLiteral(", ");
                }

                AppendMetadataTyoeName(ref interpolatedStringHandler, genericInstanceTypeSignature.TypeArguments[i], useWindowsUIXamlProjections);
            }

            interpolatedStringHandler.AppendLiteral(">");
        }
        else if (TypeMapping.TryFindMappedTypeName(type.FullName, useWindowsUIXamlProjections, out string? simpleMappedTypeName))
        {
            // We have a simple, non-generic type, so format its mapped type if available
            interpolatedStringHandler.AppendLiteral(simpleMappedTypeName);
        }
        else
        {
            // Otherwise the type must be a projected type, so just format the full name
            interpolatedStringHandler.AppendLiteral(type.FullName);
        }
    }
}
