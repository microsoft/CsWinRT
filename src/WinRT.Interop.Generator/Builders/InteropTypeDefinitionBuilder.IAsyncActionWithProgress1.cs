// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
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
    /// Helpers for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> types.
    /// </summary>
    public static class IAsyncActionWithProgress1
    {
        /// <summary>
        /// Creates a new type definition for the methods for some <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="actionMethodsType">The resulting methods type.</param>
        public static void Methods(
            GenericInstanceTypeSignature actionType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition actionMethodsType)
        {
            TypeSignature progressType = actionType.TypeArguments[0];

            // We're declaring an 'internal abstract class' type
            actionMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(actionType),
                name: InteropUtf8NameFactory.TypeName(actionType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IAsyncActionWithProgressMethodsImpl1.MakeGenericReferenceType(progressType).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(actionMethodsType);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncActionProgressHandler<T>' instance to managed
            MethodDefinition progressConvertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType),
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Define the 'Progress' get method:
            MethodDefinition get_ProgressMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.get_Handler(
                methodName: "Progress"u8,
                handlerType: interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType),
                vftblField: interopDefinitions.IAsyncActionWithProgressVftbl.GetField("get_Progress"u8),
                convertToManagedMethod: progressConvertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            actionMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1get_Progress(progressType),
                method: get_ProgressMethod);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncActionProgressHandler<T>' instance to native
            MethodDefinition progressConvertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType),
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            // Define the 'Progress' set method:
            MethodDefinition set_ProgressMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.set_Handler(
                methodName: "Progress"u8,
                handlerType: interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType),
                vftblField: interopDefinitions.IAsyncActionWithProgressVftbl.GetField("set_Progress"u8),
                convertToUnmanagedMethod: progressConvertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            actionMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1set_Progress(progressType),
                method: set_ProgressMethod);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncActionWithProgressCompletedHandler<T>' instance to managed
            MethodDefinition completedConvertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType),
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Define the 'Completed' get method:
            MethodDefinition get_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.get_Handler(
                methodName: "Completed"u8,
                handlerType: interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType),
                vftblField: interopDefinitions.IAsyncActionWithProgressVftbl.GetField("get_Completed"u8),
                convertToManagedMethod: completedConvertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            actionMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1get_Completed(progressType),
                method: get_CompletedMethod);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncActionWithProgressCompletedHandler<T>' instance to native
            MethodDefinition completedConvertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType),
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            // Define the 'Completed' set method:
            MethodDefinition set_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.set_Handler(
                methodName: "Completed"u8,
                handlerType: interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType),
                vftblField: interopDefinitions.IAsyncActionWithProgressVftbl.GetField("set_Completed"u8),
                convertToUnmanagedMethod: completedConvertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            actionMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1set_Completed(progressType),
                method: set_CompletedMethod);

            // Define the 'GetResults' method as follows:
            //
            // public static void GetResults(WindowsRuntimeObjectReference thisReference)
            MethodDefinition getResultsMethod = new(
                name: "GetResults"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldarg_0 },
                    { Call, interopReferences.IAsyncActionWithProgressGetResults },
                    { Ret }
                }
            };

            actionMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1GetResults(progressType),
                method: getResultsMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for some <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="actionMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="nativeObjectType">The resulting native object type.</param>
        public static void NativeObject(
            GenericInstanceTypeSignature actionType,
            TypeDefinition actionMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition nativeObjectType)
        {
            // The 'NativeObject' is deriving from 'WindowsRuntimeAsyncActionWithProgress<<TYPE_ARGUMENT>, <IASYNC_ACTION_WITH_PROGRESS_METHODS>>'
            TypeSignature windowsRuntimeAsyncActionWithProgress1Type = interopReferences.WindowsRuntimeAsyncActionWithProgress2.MakeGenericReferenceType(
                actionType.TypeArguments[0],
                actionMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: actionType,
                nativeObjectBaseType: windowsRuntimeAsyncActionWithProgress1Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="TypeSignature"/> for the async action type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="actionType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="callbackType">The resulting callback type.</param>
        public static void ComWrappersCallbackType(
            TypeSignature actionType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition callbackType)
        {
            ComWrappersCallback(
                runtimeClassName: actionType.FullName, // TODO
                typeSignature: actionType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out callbackType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller attribute of some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="nativeObjectType">The type returned by <see cref="NativeObject"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="actionType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void ComWrappersMarshallerAttribute(
            GenericInstanceTypeSignature actionType,
            TypeDefinition nativeObjectType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.ComWrappersMarshallerAttribute(
                typeSignature: actionType,
                nativeObjectType: nativeObjectType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the marshaller of some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="operationComWrappersCallbackType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersCallbackType"/>.</param>
        /// <param name="get_IidMethod">The 'IID' get method for <paramref name="actionType"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="marshallerType">The resulting marshaller type.</param>
        public static void Marshaller(
            GenericInstanceTypeSignature actionType,
            TypeDefinition operationComWrappersCallbackType,
            MethodDefinition get_IidMethod,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition marshallerType)
        {
            InteropTypeDefinitionBuilder.Marshaller(
                typeSignature: actionType,
                interfaceComWrappersCallbackType: operationComWrappersCallbackType,
                get_IidMethod: get_IidMethod,
                interopReferences: interopReferences,
                module: module,
                out marshallerType);
        }

        /// <summary>
        /// Creates a new type definition for the interface implementation of some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="actionMethodsType">The <see cref="TypeDefinition"/> instance returned by <see cref="Methods"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            GenericInstanceTypeSignature actionType,
            TypeDefinition actionMethodsType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature progressType = actionType.TypeArguments[0];

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(actionType),
                name: InteropUtf8NameFactory.TypeName(actionType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes = { new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor) },
                Interfaces =
                {
                    new InterfaceImplementation(actionType.ToTypeDefOrRef()),
                    new InterfaceImplementation(interopReferences.IAsyncInfo)
                }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Get the getter and setter accessor methods for 'Progress'
            MethodDefinition[] progressMethods = actionMethodsType.GetMethods("Progress"u8);

            // Create the 'get_Progress' getter method
            MethodDefinition get_ProgressMethod = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.get_Progress",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType)));

            // Add and implement the 'get_Progress' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgress1get_Progress(progressType),
                method: get_ProgressMethod);

            // Create a body for the 'get_Progress' method
            get_ProgressMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: actionType,
                implementationMethod: get_ProgressMethod,
                forwardedMethod: progressMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'set_Progress' getter method
            MethodDefinition set_ProgressMethod = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.set_Progress",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType)]));

            // Add and implement the 'set_Progress' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgress1set_Progress(progressType),
                method: set_ProgressMethod);

            // Create a body for the 'set_Progress' method
            set_ProgressMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: actionType,
                implementationMethod: set_ProgressMethod,
                forwardedMethod: progressMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Progress' property
            PropertyDefinition progressProperty = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.Progress",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_ProgressMethod))
            {
                GetMethod = get_ProgressMethod,
                SetMethod = set_ProgressMethod
            };

            interfaceImplType.Properties.Add(progressProperty);

            // Get the getter and setter accessor methods for 'Completed'
            MethodDefinition[] completedMethods = actionMethodsType.GetMethods("Completed"u8);

            // Create the 'get_Completed' getter method
            MethodDefinition get_CompletedMethod = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.get_Completed",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType)));

            // Add and implement the 'get_Completed' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgress1get_Completed(progressType),
                method: get_CompletedMethod);

            // Create a body for the 'get_Completed' method
            get_CompletedMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: actionType,
                implementationMethod: get_CompletedMethod,
                forwardedMethod: completedMethods[0],
                interopReferences: interopReferences,
                module: module);

            // Create the 'set_Completed' setter method
            MethodDefinition set_CompletedMethod = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.set_Completed",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType)]));

            // Add and implement the 'set_Completed' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgress1set_Completed(progressType),
                method: set_CompletedMethod);

            // Create a body for the 'set_Completed' method
            set_CompletedMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: actionType,
                implementationMethod: set_CompletedMethod,
                forwardedMethod: completedMethods[1],
                interopReferences: interopReferences,
                module: module);

            // Create the 'Completed' property
            PropertyDefinition completedProperty = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.Completed",
                attributes: PropertyAttributes.None,
                signature: PropertySignature.FromGetMethod(get_CompletedMethod))
            {
                GetMethod = get_CompletedMethod,
                SetMethod = set_CompletedMethod
            };

            interfaceImplType.Properties.Add(completedProperty);

            // Create the 'GetResults' method
            MethodDefinition getResultsMethod = new(
                name: $"Windows.Foundation.IAsyncActionWithProgress<{progressType.FullName}>.GetResults",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(module.CorLibTypeFactory.Void));

            // Add and implement the 'GetResults' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.IAsyncActionWithProgress1GetResults(progressType),
                method: getResultsMethod);

            // Create a body for the 'GetResults' method
            getResultsMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: actionType,
                implementationMethod: getResultsMethod,
                forwardedMethod: actionMethodsType.GetMethod("GetResults"u8),
                interopReferences: interopReferences,
                module: module);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the vtable for some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="emitState">The emit state for this invocation.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="implType">The resulting implementation type.</param>
        public static void ImplType(
            GenericInstanceTypeSignature actionType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            InteropGeneratorEmitState emitState,
            ModuleDefinition module,
            out TypeDefinition implType)
        {
            TypeSignature progressType = actionType.TypeArguments[0];

            // Prepare the 'AsyncActionProgressHandler<<PROGRESS_TYPE>>' signature
            TypeSignature asyncActionProgressHandlerType = interopReferences.AsyncActionProgressHandler1.MakeGenericReferenceType(progressType);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncActionProgressHandler<T>' instance to native
            MethodDefinition progressConvertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncActionProgressHandlerType,
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            MethodDefinition get_ProgressMethod = InteropMethodDefinitionFactory.IAsyncInfoImpl.get_Handler(
                methodName: "get_Progress"u8,
                asyncInfoType: actionType,
                handlerType: asyncActionProgressHandlerType,
                get_HandlerMethod: interopReferences.IAsyncActionWithProgress1get_Progress(progressType),
                convertToUnmanagedMethod: progressConvertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncOperationProgressHandler<T>' instance to managed
            MethodDefinition progressConvertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncActionProgressHandlerType,
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            MethodDefinition set_ProgressMethod = InteropMethodDefinitionFactory.IAsyncInfoImpl.set_Handler(
                methodName: "set_Progress"u8,
                asyncInfoType: actionType,
                handlerType: asyncActionProgressHandlerType,
                set_HandlerMethod: interopReferences.IAsyncActionWithProgress1set_Progress(progressType),
                convertToManagedMethod: progressConvertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            // Prepare the 'AsyncActionWithProgressCompletedHandler<<PROGRESS_TYPE>>' signature
            TypeSignature asyncActionWithProgressCompletedHandlerType = interopReferences.AsyncActionWithProgressCompletedHandler1.MakeGenericReferenceType(progressType);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncActionProgressHandler<T>' instance to native
            MethodDefinition completedConvertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncActionWithProgressCompletedHandlerType,
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            MethodDefinition get_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoImpl.get_Handler(
                methodName: "get_Completed"u8,
                asyncInfoType: actionType,
                handlerType: asyncActionWithProgressCompletedHandlerType,
                get_HandlerMethod: interopReferences.IAsyncActionWithProgress1get_Completed(progressType),
                convertToUnmanagedMethod: completedConvertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncActionWithProgressCompletedHandler<T>' instance to managed
            MethodDefinition completedConvertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncActionWithProgressCompletedHandlerType,
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            MethodDefinition set_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoImpl.set_Handler(
                methodName: "set_Completed"u8,
                asyncInfoType: actionType,
                handlerType: asyncActionWithProgressCompletedHandlerType,
                set_HandlerMethod: interopReferences.IAsyncActionWithProgress1set_Completed(progressType),
                convertToManagedMethod: completedConvertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            MethodDefinition getResultsMethod = InteropMethodDefinitionFactory.IAsyncActionWithProgress1Impl.GetResults(
                actionType: actionType,
                interopReferences: interopReferences,
                module: module);

            Impl(
                interfaceType: ComInterfaceType.InterfaceIsIInspectable,
                ns: InteropUtf8NameFactory.TypeNamespace(actionType),
                name: InteropUtf8NameFactory.TypeName(actionType, "Impl"),
                vftblType: interopDefinitions.IAsyncActionWithProgressVftbl,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                implType: out implType,
                vtableMethods: [
                    get_ProgressMethod,
                    set_ProgressMethod,
                    get_CompletedMethod,
                    set_CompletedMethod,
                    getResultsMethod]);

            // Track the type (it may be needed by COM interface entries for user-defined types)
            emitState.TrackTypeDefinition(implType, actionType, "Impl");
        }

        /// <summary>
        /// Creates a new type definition for the proxy type of some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="actionComWrappersMarshallerAttributeType">The <see cref="TypeDefinition"/> instance returned by <see cref="ComWrappersMarshallerAttribute"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="proxyType">The resulting proxy type.</param>
        public static void Proxy(
            GenericInstanceTypeSignature actionType,
            TypeDefinition actionComWrappersMarshallerAttributeType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition proxyType)
        {
            string runtimeClassName = $"Windows.Foundation.IAsyncActionWithProgress`1<{actionType.TypeArguments[0]}>"; // TODO

            InteropTypeDefinitionBuilder.Proxy(
                ns: InteropUtf8NameFactory.TypeNamespace(actionType),
                name: InteropUtf8NameFactory.TypeName(actionType),
                runtimeClassName: runtimeClassName,
                comWrappersMarshallerAttributeType: actionComWrappersMarshallerAttributeType,
                interopReferences: interopReferences,
                module: module,
                out proxyType);
        }

        /// <summary>
        /// Creates the type map attributes for some <c>IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="proxyType">The <see cref="TypeDefinition"/> instance returned by <see cref="InteropTypeDefinitionBuilder.Proxy"/>.</param>
        /// <param name="interfaceImplType">The <see cref="TypeDefinition"/> instance returned by <see cref="InterfaceImpl"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        public static void TypeMapAttributes(
            GenericInstanceTypeSignature actionType,
            TypeDefinition proxyType,
            TypeDefinition interfaceImplType,
            InteropReferences interopReferences,
            ModuleDefinition module)
        {
            InteropTypeDefinitionBuilder.TypeMapAttributes(
                runtimeClassName: $"Windows.Foundation.IAsyncActionWithProgress`1<{actionType.TypeArguments[0]}>", // TODO
                externalTypeMapTargetType: proxyType.ToReferenceTypeSignature(),
                externalTypeMapTrimTargetType: actionType,
                proxyTypeMapSourceType: null,
                proxyTypeMapProxyType: null,
                interfaceTypeMapSourceType: actionType,
                interfaceTypeMapProxyType: interfaceImplType.ToReferenceTypeSignature(),
                interopReferences: interopReferences,
                module: module);
        }
    }
}