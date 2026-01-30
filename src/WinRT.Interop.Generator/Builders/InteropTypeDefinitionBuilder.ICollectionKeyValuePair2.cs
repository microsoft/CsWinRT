// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public static class ICollectionKeyValuePair2
    {
        /// <summary>
        /// Creates a new type definition for the forwarder attribute for a <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="collectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="forwarderAttributeType">The resulting marshaller type.</param>
        public static void ForwarderAttribute(
            GenericInstanceTypeSignature collectionType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition forwarderAttributeType)
        {
            GenericInstanceTypeSignature keyValuePairType = (GenericInstanceTypeSignature)collectionType.TypeArguments[0];
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];

            InteropTypeDefinitionFactory.IReadOnlyCollectionKeyValuePair2.ForwarderAttribute(
                readOnlyCollectionType: collectionType,
                readOnlyDictionaryType: interopReferences.IDictionary2.MakeGenericReferenceType(keyType, valueType),
                readOnlyListType: interopReferences.IList1.MakeGenericReferenceType(keyValuePairType),
                interopReferences: interopReferences,
                module: module,
                forwarderAttributeType: out forwarderAttributeType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="collectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="forwarderAttributeType">The type returned by <see cref="ForwarderAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature collectionType,
            TypeDefinition forwarderAttributeType,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            GenericInstanceTypeSignature keyValuePairType = (GenericInstanceTypeSignature)collectionType.TypeArguments[0];
            TypeSignature keyType = keyValuePairType.TypeArguments[0];
            TypeSignature valueType = keyValuePairType.TypeArguments[1];
            TypeSignature dictionaryType = interopReferences.IDictionary2.MakeGenericReferenceType(keyType, valueType);
            TypeSignature listType = interopReferences.IList1.MakeGenericReferenceType(keyValuePairType);
            TypeSignature enumerableType = interopReferences.IEnumerable1.MakeGenericReferenceType(keyValuePairType);

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(collectionType),
                name: InteropUtf8NameFactory.TypeName(collectionType, "InterfaceImpl"),
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
                    new InterfaceImplementation(collectionType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(enumerableType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IEnumerable.Import(module).ToTypeDefOrRef())
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // There's two 'Add' and 'Remove' overloads, one from 'IDictionary<,>' and one from 'ICollection<>'.
            // They are always emitted in this relative order in the "Methods" type, so get them in advance here.
            MethodDefinition[] dictionaryAddMethods = emitState.LookupTypeDefinition(dictionaryType, "Methods").GetMethods("Add"u8);
            MethodDefinition[] dictionaryRemoveMethods = emitState.LookupTypeDefinition(dictionaryType, "Methods").GetMethods("Remove"u8);

            // Create the 'Add' ('KeyValuePair<,>') method
            MethodDefinition addKeyValuePairMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Add",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [keyValuePairType.Import(module)]));

            // Add and implement the 'Add' ('KeyValuePair<,>') method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Add(keyValuePairType).Import(module),
                method: addKeyValuePairMethod);

            // Create a body for the 'Add' ('KeyValuePair<,>') method
            addKeyValuePairMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType1: dictionaryType,
                interfaceType2: listType,
                implementationMethod: addKeyValuePairMethod,
                forwardedMethod1: dictionaryAddMethods[1],
                forwardedMethod2: emitState.LookupTypeDefinition(listType, "Methods").GetMethod("Add"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Remove' ('KeyValuePair<,>') method
            MethodDefinition removeKeyValuePairMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Remove",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyValuePairType.Import(module)]));

            // Add and implement the 'Remove' ('KeyValuePair<,>') method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Remove(keyValuePairType).Import(module),
                method: removeKeyValuePairMethod);

            // Create a body for the 'Remove' ('KeyValuePair<,>') method
            removeKeyValuePairMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType1: dictionaryType,
                interfaceType2: listType,
                implementationMethod: removeKeyValuePairMethod,
                forwardedMethod1: dictionaryRemoveMethods[1],
                forwardedMethod2: emitState.LookupTypeDefinition(listType, "Methods").GetMethod("Remove"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Contains' method
            MethodDefinition containsMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Contains",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Boolean,
                    parameterTypes: [keyValuePairType.Import(module)]));

            // Add and implement the 'Contains' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1Contains(keyValuePairType).Import(module),
                method: containsMethod);

            // Create a body for the 'Contains' method
            containsMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType1: dictionaryType,
                interfaceType2: listType,
                implementationMethod: containsMethod,
                forwardedMethod1: emitState.LookupTypeDefinition(dictionaryType, "Methods").GetMethod("Contains"u8),
                forwardedMethod2: emitState.LookupTypeDefinition(listType, "Methods").GetMethod("Contains"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'CopyTo' method
            MethodDefinition copyToMethod = InteropMethodDefinitionFactory.ICollectionKeyValuePair2InterfaceImpl.CopyTo(
                collectionType: collectionType,
                interopReferences: interopReferences,
                emitState: emitState,
                module: module);

            // Add and implement the 'CopyTo' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1CopyTo(keyValuePairType).Import(module),
                method: copyToMethod);

            // Create the 'get_Count' getter method
            MethodDefinition get_CountMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.get_Count",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Int32));

            // Add and implement the 'get_Count' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1get_Count(keyValuePairType).Import(module),
                method: get_CountMethod);

            // Create a body for the 'get_Count' method
            get_CountMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType1: dictionaryType,
                interfaceType2: listType,
                implementationMethod: get_CountMethod,
                forwardedMethod1: emitState.LookupTypeDefinition(dictionaryType, "Methods").GetMethod("Count"u8),
                forwardedMethod2: emitState.LookupTypeDefinition(listType, "Methods").GetMethod("Count"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Count' property
            PropertyDefinition countProperty = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.Count",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CountMethod))
            { GetMethod = get_CountMethod };

            interfaceImplType.Properties.Add(countProperty);

            // Create the 'get_IsReadOnly' getter method
            MethodDefinition get_IsReadOnlyMethod = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.get_IsReadOnly",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Boolean))
            {
                CilInstructions =
                {
                    { Ldc_I4_0 },
                    { Ret }
                }
            };

            // Add and implement the 'get_IsReadOnly' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICollection1get_IsReadOnly(keyValuePairType).Import(module),
                method: get_IsReadOnlyMethod);

            // Create the 'IsReadOnly' property
            PropertyDefinition isReadOnlyProperty = new(
                name: $"System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<{keyType.FullName},{valueType.FullName}>>.IsReadOnly",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_IsReadOnlyMethod))
            { GetMethod = get_IsReadOnlyMethod };

            interfaceImplType.Properties.Add(isReadOnlyProperty);
        }

        /// <summary>
        /// Creates the type map attributes for some <see cref="System.Collections.Generic.ICollection{T}"/> of <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="collectionType">The <see cref="GenericInstanceTypeSignature"/> for the generic interface type.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature collectionType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: null,
                metadataTypeName: null,
                externalTypeMapTargetType: null,
                externalTypeMapTrimTargetType: null,
                marshallingTypeMapSourceType: null,
                marshallingTypeMapProxyType: null,
                metadataTypeMapSourceType: null,
                metadataTypeMapProxyType: null,
                interfaceTypeMapSourceType: collectionType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}