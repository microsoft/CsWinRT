// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal class RuntimeClassNameMapping
{
    public static string GetMappedGenericInstanceRuntimeClassName(TypeSignature type, InteropReferences interopReferences)
    {
        if (type is GenericInstanceTypeSignature genericInstanceType && type.Resolve() is TypeDefinition typeDefinition)
        {
            if (TypeMapping.TryFindMappedTypeName(typeDefinition.FullName, false, out string? mappedTypeName))
            {
                if (genericInstanceType.TypeArguments.Count == 0)
                {
                    return mappedTypeName;
                }

                DefaultInterpolatedStringHandler typeArgumentsStringHandler = $"<";

                foreach (TypeSignature typeArgument in genericInstanceType.TypeArguments)
                {
                    string mappedArgumentName = GetMappedGenericInstanceRuntimeClassName(typeArgument, interopReferences);
                    typeArgumentsStringHandler.AppendFormatted(mappedArgumentName);
                    typeArgumentsStringHandler.AppendLiteral(", ");
                }

                DefaultInterpolatedStringHandler handler = $"{mappedTypeName}{typeArgumentsStringHandler.ToStringAndClear().TrimEnd().TrimEnd(',')}>";
                return handler.ToString();
            }
        }
        return type.FullName;
    }
}
