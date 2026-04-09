// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="IMethodDefOrRef"/> type.
/// </summary>
internal static class IMethodDefOrRefExtensions
{
    extension(IMethodDefOrRef method)
    {
        /// <summary>
        /// Enumerates all types that are visible from a given method (e.g. return type, parameter types, etc.).
        /// </summary>
        /// <param name="runtimeContext">The context to assume when resolving types.</param>
        /// <returns>The visible types for the given method.</returns>
        public IEnumerable<TypeSignature> EnumerateAllVisibleTypes(RuntimeContext? runtimeContext)
        {
            // Visit the method signature, if available
            if (method.Signature is MethodSignature signature)
            {
                yield return signature.ReturnType;

                // Gather all parameter types as well
                foreach (TypeSignature parameterType in signature.ParameterTypes)
                {
                    yield return parameterType;
                }
            }

            // Also visit the method definition, if we can resolve it
            if (method.TryResolve(runtimeContext, out MethodDefinition? definition))
            {
                // Process all declared locals first
                foreach (TypeSignature localType in definition.EnumerateLocalVariableTypes())
                {
                    yield return localType;
                }

                // Look for all 'newobj' instructions and gather the object types
                foreach (ITypeDefOrRef objectType in definition.EnumerateNewobjTypes())
                {
                    yield return objectType.ToTypeSignature(runtimeContext);
                }

                // Look for all 'newarr' instructions and gather the array types
                foreach (ITypeDefOrRef elementType in definition.EnumerateNewarrElementTypes())
                {
                    yield return elementType.ToTypeSignature(runtimeContext).MakeSzArrayType();
                }
            }
        }
    }
}