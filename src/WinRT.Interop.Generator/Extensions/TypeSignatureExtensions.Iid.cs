// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Visitors;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator;

/// <inheritdoc cref="TypeSignatureExtensions"/>
internal partial class TypeSignatureExtensions
{
    extension(TypeSignature type)
    {
        /// <summary>
        /// Checks whether a type has an interface representation supported by Windows Runtime IID lookup.
        /// </summary>
        /// <param name="interopReferences">The references for this invocation.</param>
        /// <returns>Whether the existing signature generator can supply an IID for this type.</returns>
        public bool IsWindowsRuntimeIidType(InteropReferences interopReferences)
        {
            if (!type.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance) ||
                !type.IsFullyResolvable(interopReferences.RuntimeContext, out TypeDefinition? definition) ||
                !type.IsWindowsRuntimeType(interopReferences) ||
                interopReferences.SignatureComparer.Equals(type, interopReferences.EventHandler) ||
                (definition.HasGenericParameters && type is not (GenericInstanceTypeSignature or SzArrayTypeSignature)) ||
                definition.IsWindowsRuntimeAttributeType(interopReferences) ||
                (definition.IsAbstract && definition.IsSealed))
            {
                return false;
            }

            if (type is GenericInstanceTypeSignature generic)
            {
                if (generic.IsConstructedNullableValueType(interopReferences) &&
                    generic.TypeArguments[0].IsConstructedKeyValuePairType(interopReferences))
                {
                    return false;
                }

                return generic.TypeArguments.All(argument => argument is not SzArrayTypeSignature && argument.IsWindowsRuntimeIidType(interopReferences));
            }

            if (type is SzArrayTypeSignature array)
            {
                return array.BaseType.IsWindowsRuntimeIidType(interopReferences);
            }

            return !type.IsValueType || definition.IsEnum || type.IsFundamentalWindowsRuntimeType(interopReferences) ||
                type.IsCustomMappedWindowsRuntimeNonGenericStructOrClassType(interopReferences) ||
                definition.Fields.Any(static field => !field.IsStatic);
        }
    }
}
