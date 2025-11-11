// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for COM types.
/// </summary>
internal static class ComExtensions
{
    extension(IHasCustomAttribute type)
    {
        /// <summary>
        /// Checks whether a <see cref="IHasCustomAttribute"/> represents a <see cref="System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute"/> type.
        /// </summary>
        /// <returns>Whether the type represents a <see cref="System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute"/> type.</returns>
        public bool IsGeneratedComInterfaceType => type.HasCustomAttribute("System.Runtime.InteropServices.Marshalling"u8, "GeneratedComInterfaceAttribute"u8);
    }

    extension(TypeDefinition type)
    {
        /// <summary>
        /// Tries to get the generated <c>InterfaceInformation</c> type for a given <see cref="System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute"/> type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="interfaceInformationType">The resulting <c>InterfaceInformation</c> type.</param>
        /// <returns>Whether <paramref name="interfaceInformationType"/> was successfully retrieved.</returns>
        public bool TryGetInterfaceInformationType(InteropReferences interopReferences, [NotNullWhen(true)] out TypeSignature? interfaceInformationType)
        {
            GenericInstanceTypeSignature? unknownDerivedAttributeType = null;

            // First, try to find the constructed '[IUnknownDerived<,>]' attribute
            for (int i = 0; i < type.CustomAttributes.Count; i++)
            {
                CustomAttribute currentAttribute = type.CustomAttributes[i];

                // We expect the attribute to be a constructed generic one, so we need that type signature
                if (currentAttribute.Type?.ToReferenceTypeSignature() is not GenericInstanceTypeSignature typeSignature)
                {
                    continue;
                }

                // Check that the attribute type is a match
                if (!SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IUnknownDerivedAttribute2))
                {
                    continue;
                }

                // We found the '[IUnknownDerived<,>]' attribute, we can stop and inspect it
                unknownDerivedAttributeType = typeSignature;

                break;
            }

            // If we didn't find the '[IUnknownDerived<,>]' attribute, we must stop here. We mostly expect
            // this to happen if the generation failed (e.g. the interface declaration was not valid).
            if (unknownDerivedAttributeType is null)
            {
                interfaceInformationType = null;

                return false;
            }

            // The generated 'InterfaceInformation' type is the first generic argument of the attribute
            interfaceInformationType = unknownDerivedAttributeType.TypeArguments[0];

            return true;
        }
    }
}
