// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class RuntimeClassNameGenerator
{
    /// <summary>
    /// Builds the projected WinRT runtime class name for a (potentially generic) type,
    /// applying known type-name mappings and recursively formatting generic arguments.
    /// When <paramref name="type"/> is a generic instance whose generic type has a mapped
    /// name, the result is <c>MappedName&lt;Arg1, Arg2, ...&gt;</c>. Otherwise, this returns
    /// the mapped simple name (if any) or the original <see cref="TypeSignature.FullName"/>.
    /// </summary>
    /// <param name="type">
    /// The type to map. May be a simple type or a <c>GenericInstanceTypeSignature</c>.
    /// Generic arguments are also mapped recursively
    /// </param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>
    /// The mapped runtime class name. For generic instances with a mapped generic type,
    /// returns the mapped name with type arguments (e.g., <c>Namespace.Type&lt;TArg&gt;</c>).
    /// If no mapping exists, returns <paramref name="type"/>.<see cref="TypeSignature.FullName"/>.
    /// </returns>
    public static string GetGenericInstanceRuntimeClassName(TypeSignature type, bool useWindowsUIXamlProjections)
    {
        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[type.FullName.Length * 2]);

        GetGenericInstanceRuntimeClassNameHelper(type, useWindowsUIXamlProjections, ref handler);

        return handler.ToStringAndClear();
    }

    private static void GetGenericInstanceRuntimeClassNameHelper(TypeSignature type, bool useWindowsUIXamlProjections, ref DefaultInterpolatedStringHandler interpolatedStringHandler)
    {
        if (type is GenericInstanceTypeSignature genericInstanceTypeSignature)
        {
            // If the generic type has a mapped name, use it; otherwise, use the full name.
            if (TypeMapping.TryFindMappedTypeName(genericInstanceTypeSignature.GenericType.FullName, useWindowsUIXamlProjections, out string? mappedTypeName))
            {
                interpolatedStringHandler.AppendLiteral(mappedTypeName);
            }
            else
            {
                interpolatedStringHandler.AppendLiteral(genericInstanceTypeSignature.GenericType.FullName);
            }

            interpolatedStringHandler.AppendLiteral("<");

            // Recursively append each type argument.
            GetGenericInstanceRuntimeClassNameHelper(genericInstanceTypeSignature.TypeArguments[0], useWindowsUIXamlProjections, ref interpolatedStringHandler);

            for (int i = 1; i < genericInstanceTypeSignature.TypeArguments.Count; i++)
            {
                interpolatedStringHandler.AppendLiteral(", ");

                GetGenericInstanceRuntimeClassNameHelper(genericInstanceTypeSignature.TypeArguments[i], useWindowsUIXamlProjections, ref interpolatedStringHandler);
            }

            interpolatedStringHandler.AppendLiteral(">");

            return;
        }

        // Non-generic type: apply mapping if available.
        if (TypeMapping.TryFindMappedTypeName(type.FullName, useWindowsUIXamlProjections, out string? simpleMappedTypeName))
        {
            interpolatedStringHandler.AppendLiteral(simpleMappedTypeName);
        }
        else
        {
            interpolatedStringHandler.AppendLiteral(type.FullName);
        }
    }
}
