// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="TypeSignature"/> type.
/// </summary>
internal static class TypeSignatureExtensions
{
    extension(TypeSignature signature)
    {
        /// <summary>
        /// Gets a value indicating whether a given <see cref="TypeSignature"/> instance can be fully resolved to type definitions.
        /// </summary>
        public bool IsFullyResolvable
        {
            get
            {
                // Ensure that we can resolve the type (if we can't, we're likely missing a .dll)
                if (signature.Resolve() is null)
                {
                    return false;
                }

                // Recurse on all type arguments as well
                if (signature is GenericInstanceTypeSignature genericInstanceTypeSignature)
                {
                    foreach (TypeSignature typeArgument in genericInstanceTypeSignature.TypeArguments)
                    {
                        if (!typeArgument.IsFullyResolvable)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public bool IsDelegate
        {
            get
            {
                if (signature is null)
                {
                    return false;
                }

                TypeDefinition? typeDef = signature.Resolve();
                if (typeDef is null || !typeDef.IsClass)
                {
                    return false;
                }

                // Walk base types
                TypeDefinition? baseType = typeDef.BaseType?.Resolve();
                while (baseType != null)
                {
                    if (baseType.Namespace == "System" &&
                        (baseType.Name == "MulticastDelegate" || baseType.Name == "Delegate"))
                    {
                        return true;
                    }
                    baseType = baseType.BaseType?.Resolve();
                }

                return false;
            }
        }
    }
}
