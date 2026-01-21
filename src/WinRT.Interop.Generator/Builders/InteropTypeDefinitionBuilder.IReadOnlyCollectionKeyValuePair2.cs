// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
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

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <see cref="System.Collections.Generic.IReadOnlyCollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="readOnlyCollectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="forwarderAttributeType">The type returned by <see cref="ForwarderAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature readOnlyCollectionType,
            TypeDefinition forwarderAttributeType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            GenericInstanceTypeSignature keyValuePairType = (GenericInstanceTypeSignature)readOnlyCollectionType.TypeArguments[0];
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];
            TypeSignature readOnlyDictionaryType = interopReferences.IReadOnlyDictionary2.MakeGenericReferenceType(keyType, valueType);
            TypeSignature readOnlyListType = interopReferences.IReadOnlyList1.MakeGenericReferenceType(keyValuePairType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(readOnlyCollectionType),
                name: InteropUtf8NameFactory.TypeName(readOnlyCollectionType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes =
                {
                    new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)),
                    new CustomAttribute(forwarderAttributeType.GetMethod(".ctor"u8))
                },
                Interfaces =
                {
                    new InterfaceImplementation(readOnlyCollectionType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(enumerableType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module).ToTypeDefOrRef())
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.get_Count",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32));

            // Add and implement the 'get_Count' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IReadOnlyCollection1get_Count(keyValuePairType).Import(module),
                method: get_CountMethod);

            // Create a body for the 'get_Count' method
            get_CountMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType1: readOnlyDictionaryType,
                interfaceType2: readOnlyListType,
                implementationMethod: get_CountMethod,
                forwardedMethod1: emitState.LookupTypeDefinition(readOnlyDictionaryType, "IReadOnlyDictionaryMethods").GetMethod("Count"u8),
                forwardedMethod2: emitState.LookupTypeDefinition(readOnlyListType, "IReadOnlyListMethods").GetMethod("Count"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.IReadOnlyCollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CountMethod))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);
        }
    }
}