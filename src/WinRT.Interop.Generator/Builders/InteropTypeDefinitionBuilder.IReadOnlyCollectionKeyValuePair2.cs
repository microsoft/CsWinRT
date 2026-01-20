// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class IReadOnlyCollectionKeyValuePair2
    {
        /// <summary>
        /// Creates a new type definition for the forwarder attribute for a <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="readOnlyCollectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="forwarderAttributeType">The resulting marshaller type.</param>
        public static void ForwarderAttribute(
            GenericInstanceTypeSignature readOnlyCollectionType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition forwarderAttributeType)
        {
            GenericInstanceTypeSignature keyValuePairType = (GenericInstanceTypeSignature)readOnlyCollectionType.TypeArguments[0];
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];

            InteropTypeDefinitionFactory.IReadOnlyCollectionKeyValuePair2.ForwarderAttribute(
                readOnlyCollectionType: readOnlyCollectionType,
                readOnlyDictionaryType: interopReferences.IReadOnlyDictionary2.MakeGenericReferenceType(keyType, valueType),
                readOnlyListType: interopReferences.IReadOnlyList1.MakeGenericReferenceType(keyValuePairType),
                interopReferences: interopReferences,
                module: module,
                forwarderAttributeType: out forwarderAttributeType);
        }
    }
}