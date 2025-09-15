// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
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
    /// Helpers for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> types.
    /// </summary>
    public static class IMapChangedEventArgs1
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="argsType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature argsType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(argsType, "IID"),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the methods for some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="argsMethodsType">The resulting methods type.</param>
        public static void Methods(
            GenericInstanceTypeSignature argsType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition argsMethodsType)
        {
            TypeSignature elementType = argsType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            argsMethodsType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(argsType),
                name: InteropUtf8NameFactory.TypeName(argsType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IMapChangedEventArgsImpl1.MakeGenericReferenceType(elementType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(argsMethodsType);

            // Define the 'CollectionChange' method as follows:
            //
            // public static CollectionChange CollectionChange(WindowsRuntimeObjectReference thisReference)
            MethodDefinition collectionChangeMethod = new(
                name: "CollectionChange"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: interopReferences.CollectionChange.ToValueTypeSignature().Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IMapChangedEventArgsMethodsCollectionChange.Import(module) },
                    { Ret }
                }
            };

            argsMethodsType.Methods.Add(collectionChangeMethod);

            // Define the 'Key' method as follows:
            //
            // public static <KEY_TYPE> Key(WindowsRuntimeObjectReference thisReference)
            MethodDefinition keyMethod = new(
                name: "Key"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: elementType.Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]));

            // Add and implement the 'Key' method
            argsMethodsType.AddMethodImplementation(
                declaration: interopReferences.IMapChangedEventArgsImpl1Key(elementType).Import(module),
                method: keyMethod);

            // Create a method body for the 'Key' method
            keyMethod.CilMethodBody = new CilMethodBody()
            {
                Instructions =
                {
                    { Ldnull },
                    { Throw } // TODO
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the native object for some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="argsMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature argsType,
            TypeDefinition argsMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            // The 'NativeObject' is deriving from 'WindowsRuntimeMapChangedEventArgs<<KEY_TYPE>, <IMAPCHANGEDEVENTARGS_METHODS>>'
            TypeSignature windowsRuntimeMapChangedEventArgs2Type = interopReferences.WindowsRuntimeMapChangedEventArgs2.MakeGenericReferenceType(
                argsType.TypeArguments[0],
                argsMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: argsType,
                nativeObjectBaseType: windowsRuntimeMapChangedEventArgs2Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="TypeSignature"/> for the args type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="argsType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature argsType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: argsType.FullName, // TODO
                typeSignature: argsType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="argsType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature argsType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: argsType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="argsComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="argsType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature argsType,
            TypeDefinition argsComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: argsType,
                interfaceComWrappersCallbackType: argsComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);

            // Track the type (it's needed by 'IEnumerable<T>')
            emitState.TrackTypeDefinition(marshallerType, argsType, "Marshaller");
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="argsMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature argsType,
            TypeDefinition argsMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature elementType = argsType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(argsType),
                name: InteropUtf8NameFactory.TypeName(argsType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces = { new InterfaceImplementation(argsType.Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'CollectionChange' getter method
            MethodDefinition get_CollectionChangeMethod = new(
                name: $"Windows.Foundation.Collections.IMapChangedEventArgs<{elementType.FullName}>.get_CollectionChange",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.CollectionChange.ToValueTypeSignature().Import(module)));

            // Add and implement the 'CollectionChange' get accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IMapChangedEventArgs1get_CollectionChange(elementType).Import(module),
                method: get_CollectionChangeMethod);

            // Create a method body for the 'CollectionChange' property
            get_CollectionChangeMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: argsType,
                implementationMethod: get_CollectionChangeMethod,
                forwardedMethod: argsMethodsType.GetMethod("CollectionChange"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'CollectionChange' property
            PropertyDefinition collectionChangeProperty = new(
                name: $"Windows.Foundation.Collections.IMapChangedEventArgs<{elementType.FullName}>.CollectionChange",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CollectionChangeMethod))
            {
                GetMethod = get_CollectionChangeMethod
            };

            interfaceImplType.Properties.Add(collectionChangeProperty);

            // Create the 'Key' getter method
            MethodDefinition get_KeyMethod = new(
                name: $"Windows.Foundation.Collections.IMapChangedEventArgs<{elementType.FullName}>.get_Key",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(elementType.Import(module)));

            // Add and implement the 'Key' get accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IMapChangedEventArgs1get_Key(elementType).Import(module),
                method: get_KeyMethod);

            // Create a method body for the 'Key' property
            get_KeyMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: argsType,
                implementationMethod: get_KeyMethod,
                forwardedMethod: argsMethodsType.GetMethod("Key"u8),
                interopReferences: interopReferences,
                module: module);

            // Create the 'Key' property
            PropertyDefinition keyProperty = new(
                name: $"Windows.Foundation.Collections.IMapChangedEventArgs<{elementType.FullName}>.Key",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_KeyMethod))
            {
                GetMethod = get_KeyMethod
            };

            interfaceImplType.Properties.Add(keyProperty);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="argsType"/>.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature argsType,
            MethodDefinition get_IidMethod,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            // Define the 'get_CollectionChange' method
            MethodDefinition collectionChangeMethod = InteropMethodDefinitionFactory.IMapChangedEventArgs1Impl.CollectionChanged(
                argsType: argsType,
                interopReferences: interopReferences,
                module: module);

            // Define the 'get_Key' method
            MethodDefinition keyMethod = InteropMethodDefinitionFactory.IMapChangedEventArgs1Impl.Key(
                argsType: argsType,
                interopReferences: interopReferences,
                module: module);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(argsType),
                name: InteropUtf8NameFactory.TypeName(argsType, "Impl"),
                vftblType: interopDefinitions.IMapChangedEventArgsVftbl,
                get_IidMethod: get_IidMethod,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [collectionChangeMethod, keyMethod]);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, argsType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="argsComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature argsType,
            TypeDefinition argsComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.Collections.IMapChangedEventArgs`1<{argsType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(argsType),
                name: InteropUtf8NameFactory.TypeName(argsType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: argsComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IMapChangedEventArgs&lt;K&gt;</c> interface.
        /// </summary>
        /// <param name="argsType">The <see cref="GenericInstanceTypeSignature"/> for the args type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature argsType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.Collections.IMapChangedEventArgs`1<{argsType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: argsType,
                proxyTypeMapSourceType: argsType,
                proxyTypeMapProxyType: proxyType.ToReferenceTypeSignature(),
                interfaceTypeMapSourceType: argsType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}
