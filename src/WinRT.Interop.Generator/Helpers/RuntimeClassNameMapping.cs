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
