// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A generator for runtime class names of Windows Runtime types, or user-defined types exposed to native consumers.
/// </summary>
internal static class RuntimeClassNameGenerator
{
    /// <summary>
    /// Generates the Windows Runtime class name for a (potentially generic) type,
    /// applying known type-name mappings and recursively formatting generic arguments.
    /// </summary>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the Windows Runtime class name for.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting Windows Runtime class name for <paramref name="type"/>.</returns>
    public static string GetRuntimeClassName(TypeSignature type, bool useWindowsUIXamlProjections)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        // Helper to format a full type signature into a target interpolated handler
        static void AppendRuntimeClassName(
            ref DefaultInterpolatedStringHandler interpolatedStringHandler,
            TypeSignature type,
            bool useWindowsUIXamlProjections)
        {
            // Handle SZ array types and map them to 'IReferenceArray<T>' type names
            if (type is SzArrayTypeSignature szArrayTypeSignature)
            {
                interpolatedStringHandler.AppendLiteral("IReferenceArray<");

                AppendRuntimeClassName(ref interpolatedStringHandler, szArrayTypeSignature.BaseType, useWindowsUIXamlProjections);

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

                    AppendRuntimeClassName(ref interpolatedStringHandler, genericInstanceTypeSignature.TypeArguments[i], useWindowsUIXamlProjections);
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

        AppendRuntimeClassName(ref handler, type, useWindowsUIXamlProjections);

        return handler.ToStringAndClear();
    }
}
