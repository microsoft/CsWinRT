// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    /// Helpers for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> types.
    /// </summary>
    public static class IAsyncActionWithProgress1
    {
        /// <summary>
        /// Creates the 'IID' property for some <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> interface.
        /// </summary>
        /// <param name="actionType">The <see cref="GenericInstanceTypeSignature"/> for the async action type.</param>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The interop module being built.</param>
        /// <param name="get_IidMethod">The resulting 'IID' get method for <paramref name="actionType"/>.</param>
        public static void IID(
            GenericInstanceTypeSignature actionType,
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out MethodDefinition get_IidMethod)
        {
            InteropTypeDefinitionBuilder.IID(
                name: InteropUtf8NameFactory.TypeName(actionType),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module,
                iid: Guid.NewGuid(), // TODO
                out get_IidMethod);
        }

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

            // We're declaring an 'internal static class' type
            actionMethodsType = new TypeDefinition(
                ns: InteropUtf8NameFactory.TypeNamespace(actionType),
                name: InteropUtf8NameFactory.TypeName(actionType, "Methods"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef())
            {
                Interfaces = { new InterfaceImplementation(interopReferences.IAsyncActionWithProgressMethodsImpl1.MakeGenericReferenceType(progressType).Import(module).ToTypeDefOrRef()) }
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
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1get_Progress(progressType).Import(module),
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
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1set_Progress(progressType).Import(module),
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
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1get_Completed(progressType).Import(module),
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
                declaration: interopReferences.IAsyncActionWithProgressMethodsImpl1set_Completed(progressType).Import(module),
                method: set_CompletedMethod);
        }
    }
}
