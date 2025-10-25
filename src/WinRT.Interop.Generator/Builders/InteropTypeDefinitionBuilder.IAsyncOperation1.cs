// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
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
    /// Helpers for <c>Windows.Foundation.IAsyncOperation1&lt;TResult&gt;</c> types.
    /// </summary>
    public static class IAsyncOperation1
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="operationType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature operationType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(operationType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

        /// <summary>
        /// Creates a new type definition for the methods for some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="operationMethodsType">The resulting methods type.</param>
        public static void Methods(
            GenericInstanceTypeSignature operationType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition operationMethodsType)
        {
            TypeSignature resultType = operationType.TypeArguments[0];

            // We're declaring an 'internal static class' type
            operationMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(operationType),
                name: InteropUtf8NameFactory.TypeName(operationType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IAsyncOperationMethodsImpl1.MakeGenericReferenceType(resultType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(operationMethodsType);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncOperationCompletedHandler<T>' instance to managed
            MethodDefinition convertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType),
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Define the 'Completed' get method:
            MethodDefinition get_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.get_Handler(
                methodName: "Completed"u8,
                handlerType: interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType),
                vftblField: interopDefinitions.IAsyncOperationVftbl.GetField("get_Completed"u8),
                convertToManagedMethod: convertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationMethodsImpl1get_Completed(resultType).Import(module),
                method: get_CompletedMethod);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncOperationCompletedHandler<T>' instance to native
            MethodDefinition convertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType),
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            // Define the 'Completed' set method:
            MethodDefinition set_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.set_Handler(
                methodName: "Completed"u8,
                handlerType: interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType),
                vftblField: interopDefinitions.IAsyncOperationVftbl.GetField("set_Completed"u8),
                convertToUnmanagedMethod: convertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationMethodsImpl1set_Completed(resultType).Import(module),
                method: set_CompletedMethod);

            // Define the 'GetResults' set method:
            MethodDefinition getResultsMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.GetResults(
                resultType: resultType,
                vftblField: interopDefinitions.IAsyncOperationVftbl.GetField("GetResults"u8),
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationMethodsImpl1GetResults(resultType).Import(module),
                method: getResultsMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="operationMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature operationType,
            TypeDefinition operationMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            // The 'NativeObject' is deriving from 'WindowsRuntimeAsyncOperation<<TYPE_ARGUMENT>, <IASYNC_OPERATION_METHODS>>'
            TypeSignature windowsRuntimeAsyncOperation1Type = interopReferences.WindowsRuntimeAsyncOperation2.MakeGenericReferenceType(
                operationType.TypeArguments[0],
                operationMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: operationType,
                nativeObjectBaseType: windowsRuntimeAsyncOperation1Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="TypeSignature"/> for the async operation type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="operationType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature operationType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: operationType.FullName, // TODO
                typeSignature: operationType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="operationType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature operationType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: operationType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="operationComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="operationType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature operationType,
            TypeDefinition operationComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: operationType,
                interfaceComWrappersCallbackType: operationComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="operationMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature operationType,
            TypeDefinition operationMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature resultType = operationType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(operationType),
                name: InteropUtf8NameFactory.TypeName(operationType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)) },
                Interfaces =
                {
                    new InterfaceImplementation(operationType.Import(module).ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IAsyncInfo.Import(module))
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Get the getter and setter accessor methods for 'Completed'
            MethodDefinition[] completedMethods = operationMethodsType.GetMethods("Completed"u8);

            // Create the 'get_Completed' getter method
            MethodDefinition get_CompletedMethod = new(
                name: $"Windows.Foundation.IAsyncOperation<{resultType.FullName}>.get_Completed",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType).Import(module)));

            // Add and implement the 'get_Completed' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperation1get_Completed(resultType).Import(module),
                method: get_CompletedMethod);

            // Create a body for the 'get_Completed' method
            get_CompletedMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: operationType,
                implementationMethod: get_CompletedMethod,
                forwardedMethod: completedMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'set_Completed' getter method
            MethodDefinition set_CompletedMethod = new(
                name: $"Windows.Foundation.IAsyncOperation<{resultType.FullName}>.set_Completed",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType).Import(module)]));

            // Add and implement the 'set_Completed' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperation1set_Completed(resultType).Import(module),
                method: set_CompletedMethod);

            // Create a body for the 'set_Completed' method
            set_CompletedMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: operationType,
                implementationMethod: set_CompletedMethod,
                forwardedMethod: completedMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Completed' property
            PropertyDefinition completedProperty = new(
                name: $"Windows.Foundation.IAsyncOperation<{resultType.FullName}>.Completed",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CompletedMethod))
            {
                GetMethod = get_CompletedMethod,
                SetMethod = set_CompletedMethod
            };

            interfaceImplType.Properties.Add(completedProperty);

            // Create the 'GetResults' method
            MethodDefinition getResultsMethod = new(
                name: $"Windows.Foundation.IAsyncOperation<{resultType.FullName}>.GetResults",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(resultType.Import(module)));

            // Add and implement the 'GetResults' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperation1GetResults(resultType).Import(module),
                method: getResultsMethod);

            // Create a body for the 'GetResults' method
            getResultsMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: operationType,
                implementationMethod: getResultsMethod,
                forwardedMethod: operationMethodsType.GetMethod("GetResults"u8),
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature operationType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            TypeSignature resultType = operationType.TypeArguments[0];

            // Prepare the 'AsyncOperationCompletedHandler<<RESULT_TYPE>>' signature
            TypeSignature asyncOperationCompletedHandlerType = interopReferences.AsyncOperationCompletedHandler1.MakeGenericReferenceType(resultType);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncOperationCompletedHandler<T>' instance to native
            MethodDefinition convertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncOperationCompletedHandlerType,
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            MethodDefinition get_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoImpl.get_Handler(
                methodName: "get_Completed"u8,
                asyncInfoType: operationType,
                handlerType: asyncOperationCompletedHandlerType,
                get_HandlerMethod: interopReferences.IAsyncOperation1get_Completed(resultType),
                convertToUnmanagedMethod: convertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncOperationCompletedHandler<T>' instance to managed
            MethodDefinition convertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncOperationCompletedHandlerType,
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            MethodDefinition set_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoImpl.set_Handler(
                methodName: "set_Completed"u8,
                asyncInfoType: operationType,
                handlerType: asyncOperationCompletedHandlerType,
                set_HandlerMethod: interopReferences.IAsyncOperation1set_Completed(resultType),
                convertToManagedMethod: convertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            // TODO

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(operationType),
                name: InteropUtf8NameFactory.TypeName(operationType, "Impl"),
                vftblType: interopDefinitions.IAsyncOperationVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [
                    get_CompletedMethod,
                    set_CompletedMethod]);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, operationType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="operationComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature operationType,
            TypeDefinition operationComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.IAsyncOperation`1<{operationType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(operationType),
                name: InteropUtf8NameFactory.TypeName(operationType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: operationComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IAsyncOperation1&lt;TResult&gt;</c> interface.
        /// </summary>
        /// <param name="operationType">The <see cref="GenericInstanceTypeSignature"/> for the async operation type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature operationType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.IAsyncOperation`1<{operationType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: operationType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: operationType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}
