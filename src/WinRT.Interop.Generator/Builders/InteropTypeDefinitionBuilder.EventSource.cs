// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="InteropTypeDefinitionBuilder"/>
internal partial class InteropTypeDefinitionBuilder
{
    /// <summary>
    /// Helpers for event source types for <see cref="Delegate"/>-s.
    /// </summary>
    public static class EventSource
    {
        /// <summary>
        /// Creates a new type definition for the event source type for some <see cref="System.EventHandler{TEventArgs}"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="System.EventHandler{TEventArgs}"/> type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Delegate.Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="eventSourceType">The resulting event source type.</param>
        public static void EventHandler1(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition eventSourceType)
        {
            TypeSignature baseEventSourceType = interopReferences.EventHandler1EventSource.MakeGenericInstanceType(delegateType.TypeArguments[0]);

            // We're declaring an 'internal sealed class' type
            eventSourceType = new(
                ns: "ABI.WindowsRuntime.InteropServices"u8,
                name: InteropUtf8NameFactory.TypeName(baseEventSourceType),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: baseEventSourceType.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(eventSourceType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(
                module: module,
                parameterTypes: [
                    interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                    module.CorLibTypeFactory.Int32]);

            eventSourceType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Ldarg_2);
            _ = ctor.CilMethodBody!.Instructions.Insert(3, Call, interopReferences.EventHandler1EventSource_ctor.Import(module));

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: true),
                    parameterTypes: [delegateType.Import(module)]));

            eventSourceType.Methods.Add(convertToUnmanagedMethod);

            // Mark the 'ConvertToUnmanaged' method as overriding the base method
            eventSourceType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.EventHandler1EventSourceConvertToUnmanaged(delegateType).Import(module),
                body: convertToUnmanagedMethod));

            // Create a method body for the 'CreateObject' method
            convertToUnmanagedMethod.CilMethodBody = new CilMethodBody(convertToUnmanagedMethod)
            {
                Instructions =
                {
                    { Ldarg_1 },
                    { Call, marshallerType.GetMethod("ConvertToUnmanaged"u8) },
                    { Ret }
                }
            };
        }

        /// <summary>
        /// Creates a new type definition for the event source type for some <see cref="System.EventHandler{TSender, TEventArgs}"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="System.EventHandler{TSender, TEventArgs}"/> type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Delegate.Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="eventSourceType">The resulting event source type.</param>
        public static void EventHandler2(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition eventSourceType)
        {
            TypeSignature baseEventSourceType = interopReferences.EventHandler2EventSource.MakeGenericInstanceType([.. delegateType.TypeArguments]);

            // We're declaring an 'internal sealed class' type
            eventSourceType = new(
                ns: "ABI.WindowsRuntime.InteropServices"u8,
                name: InteropUtf8NameFactory.TypeName(baseEventSourceType),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: baseEventSourceType.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(eventSourceType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(
                module: module,
                parameterTypes: [
                    interopReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                    module.CorLibTypeFactory.Int32]);

            eventSourceType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Ldarg_2);
            _ = ctor.CilMethodBody!.Instructions.Insert(3, Call, interopReferences.EventHandler2EventSource_ctor.Import(module));

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: true),
                    parameterTypes: [delegateType.Import(module)]));

            eventSourceType.Methods.Add(convertToUnmanagedMethod);

            // Mark the 'ConvertToUnmanaged' method as overriding the base method
            eventSourceType.MethodImplementations.Add(new MethodImplementation(
                declaration: interopReferences.EventHandler2EventSourceConvertToUnmanaged(delegateType).Import(module),
                body: convertToUnmanagedMethod));

            // Create a method body for the 'CreateObject' method
            convertToUnmanagedMethod.CilMethodBody = new CilMethodBody(convertToUnmanagedMethod)
            {
                Instructions =
                {
                    { Ldarg_1 },
                    { Call, marshallerType.GetMethod("ConvertToUnmanaged"u8) },
                    { Ret }
                }
            };
        }
    }
}
