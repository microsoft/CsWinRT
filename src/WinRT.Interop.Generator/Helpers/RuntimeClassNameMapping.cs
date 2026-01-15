// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal class RuntimeClassNameMapping
{

    // TODO: Debug code; Will remove later ---------------------
#pragma warning disable IDE0044 // Add readonly modifier
    private static readonly string printPath = @"C:\Users\kythant\staging\MappedRuntimeClassNames.txt";
    private static HashSet<string> seenStrings = [];
    private static StreamWriter writer = new(printPath, append: false);
#pragma warning restore IDE0044 // Add readonly modifier
    // ---------------------------------------------------------

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
    public static string GetMappedGenericInstanceRuntimeClassName(TypeSignature type, bool useWindowsUIXamlProjections)
    {
        if (type is GenericInstanceTypeSignature genericInstanceType
            && TypeMapping.TryFindMappedTypeName(genericInstanceType.GenericType.FullName, useWindowsUIXamlProjections, out string? mappedTypeName))
        {
            if (genericInstanceType.TypeArguments.Count == 0)
            {
                return mappedTypeName;
            }

            DefaultInterpolatedStringHandler handler = $"{mappedTypeName}<";

            handler.AppendLiteral(GetMappedGenericInstanceRuntimeClassName(genericInstanceType.TypeArguments[0], useWindowsUIXamlProjections));

            for (int i = 1; i < genericInstanceType.TypeArguments.Count; i++)
            {
                handler.AppendLiteral(", ");
                handler.AppendLiteral(GetMappedGenericInstanceRuntimeClassName(genericInstanceType.TypeArguments[i], useWindowsUIXamlProjections));
            }

            handler.AppendLiteral(">");

            // TODO: Debug code; Will remove later ---------------------
            if (!seenStrings.Contains(type.FullName))
            {
                writer.WriteLine(type.FullName);
                writer.WriteLine(handler.ToString());
                writer.WriteLine();
                _ = seenStrings.Add(type.FullName);
            }
            // ---------------------------------------------------------

            return handler.ToStringAndClear();
        }

        return TypeMapping.TryFindMappedTypeName(type.FullName, useWindowsUIXamlProjections, out string? simpleMappedTypeName)
            ? simpleMappedTypeName
            : type.FullName;
    }
}
