// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    /// Helpers for <c>Windows.Foundation.IAsyncOperationWithProgress2&lt;TResult, TProgress&gt;</c> types.
    /// </summary>
    public static class IAsyncOperationWithProgress2
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>IAsyncOperationWithProgress2&lt;TResult, TProgress&gt;</c> interface.
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
        /// Creates a new type definition for the methods for some <c>IAsyncOperationWithProgress2&lt;TResult, TProgress&gt;</c> interface.
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
            TypeSignature progressType = operationType.TypeArguments[1];

            // We're declaring an 'internal static class' type
            operationMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(operationType),
                name: InteropUtf8NameFactory.TypeName(operationType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IAsyncOperationWithProgressMethodsImpl2.MakeGenericReferenceType(resultType, progressType).Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(operationMethodsType);

            // Prepare the handler types
            TypeSignature asyncOperationProgressHandlerType = interopReferences.AsyncOperationProgressHandler2.MakeGenericReferenceType(resultType, progressType);
            TypeSignature asyncOperationWithProgressCompletedHandlerType = interopReferences.AsyncOperationWithProgressCompletedHandler2.MakeGenericReferenceType(resultType, progressType);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncOperationProgressHandler<TResult, TProgress>' instance to managed
            MethodDefinition progressConvertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncOperationProgressHandlerType,
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Define the 'Progress' get method:
            MethodDefinition get_ProgressMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.get_Handler(
                methodName: "Progress"u8,
                handlerType: asyncOperationProgressHandlerType,
                vftblField: interopDefinitions.IAsyncOperationWithProgressVftbl.GetField("get_Progress"u8),
                convertToManagedMethod: progressConvertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationWithProgressMethodsImpl2get_Progress(resultType, progressType).Import(module),
                method: get_ProgressMethod);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncOperationProgressHandler<TResult, TProgress>' instance to native
            MethodDefinition progressConvertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncOperationProgressHandlerType,
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            // Define the 'Progress' set method:
            MethodDefinition set_ProgressMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.set_Handler(
                methodName: "Progress"u8,
                handlerType: asyncOperationProgressHandlerType,
                vftblField: interopDefinitions.IAsyncOperationWithProgressVftbl.GetField("set_Progress"u8),
                convertToUnmanagedMethod: progressConvertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationWithProgressMethodsImpl2set_Progress(resultType, progressType).Import(module),
                method: set_ProgressMethod);

            // Get the generated 'ConvertToManaged' method to marshal the 'AsyncOperationWithProgressCompletedHandler<TResult, TProgress>' instance to managed
            MethodDefinition completedConvertToManagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncOperationWithProgressCompletedHandlerType,
                key: "Marshaller").GetMethod("ConvertToManaged"u8);

            // Define the 'Completed' get method:
            MethodDefinition get_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.get_Handler(
                methodName: "Completed"u8,
                handlerType: asyncOperationWithProgressCompletedHandlerType,
                vftblField: interopDefinitions.IAsyncOperationWithProgressVftbl.GetField("get_Completed"u8),
                convertToManagedMethod: completedConvertToManagedMethod,
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationWithProgressMethodsImpl2get_Completed(resultType, progressType).Import(module),
                method: get_CompletedMethod);

            // Get the generated 'ConvertToUnmanaged' method to marshal the 'AsyncOperationWithProgressCompletedHandler<TResult, TProgress>' instance to native
            MethodDefinition completedConvertToUnmanagedMethod = emitState.LookupTypeDefinition(
                typeSignature: asyncOperationWithProgressCompletedHandlerType,
                key: "Marshaller").GetMethod("ConvertToUnmanaged"u8);

            // Define the 'Completed' set method:
            MethodDefinition set_CompletedMethod = InteropMethodDefinitionFactory.IAsyncInfoMethods.set_Handler(
                methodName: "Completed"u8,
                handlerType: asyncOperationWithProgressCompletedHandlerType,
                vftblField: interopDefinitions.IAsyncOperationWithProgressVftbl.GetField("set_Completed"u8),
                convertToUnmanagedMethod: completedConvertToUnmanagedMethod,
                interopReferences: interopReferences,
                module: module);

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationWithProgressMethodsImpl2set_Completed(resultType, progressType).Import(module),
                method: set_CompletedMethod);

            // Define the 'GetResults' method as follows:
            //
            // public static <RESULT_TYPE> GetResults(WindowsRuntimeObjectReference thisReference)
            MethodDefinition getResultsMethod = new(
                name: "GetResults"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
                signature: MethodSignature.CreateStatic(
                    returnType: resultType.Import(module),
                    parameterTypes: [interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature()]))
            {
                CilInstructions =
                {
                    { Ldnull },
                    { Ret } // TODO
                }
            };

            operationMethodsType.AddMethodImplementation(
                declaration: interopReferences.IAsyncOperationWithProgressMethodsImpl2GetResults(resultType, progressType).Import(module),
                method: getResultsMethod);
        }

        /// <summary>
        /// Creates a new type definition for the native object for some <c>IAsyncOperationWithProgress2&lt;TResult, TProgress&gt;</c> interface.
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
            // The 'NativeObject' is deriving from 'WindowsRuntimeAsyncOperationWithProgress<<RESULT_TYPE>, <PROGRESS_TYPE>, <IASYNC_OPERATION_WITH_PROGRESS_METHODS>>'
            TypeSignature windowsRuntimeAsyncOperationWithProgress2Type = interopReferences.WindowsRuntimeAsyncOperationWithProgress3.MakeGenericReferenceType(
                operationType.TypeArguments[0],
                operationType.TypeArguments[1],
                operationMethodsType.ToReferenceTypeSignature());

            InteropTypeDefinitionBuilder.NativeObject(
                typeSignature: operationType,
                nativeObjectBaseType: windowsRuntimeAsyncOperationWithProgress2Type,
                interopReferences: interopReferences,
                module: module,
                out nativeObjectType);
        }

        /// <summary>
        /// Creates a new type definition for the implementation of the <c>IWindowsRuntimeUnsealedObjectComWrappersCallback</c> interface for some <c>IAsyncOperationWithProgress2&lt;TResult, TProgress&gt;</c> interface.
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
    }
}
