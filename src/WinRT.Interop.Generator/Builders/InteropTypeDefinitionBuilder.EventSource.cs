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
        /// Creates a new type definition for the event source type for some <see cref="Delegate"/> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the <see cref="Delegate"/> type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Delegate.Marshaller"/>.</param>
        /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="eventSourceType">The resulting event source type.</param>
        public static void EventHandler1(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition marshallerType,
            WellKnownInteropReferences wellKnownInteropReferences,
            ModuleDefinition module,
            out TypeDefinition eventSourceType)
        {
            // We're declaring an 'internal sealed class' type
            eventSourceType = new(
                ns: "ABI.WindowsRuntime.InteropServices"u8,
                name: InteropUtf8NameFactory.TypeName(delegateType, "EventSource"),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: wellKnownInteropReferences.EventHandler1EventSource.MakeGenericInstanceType(delegateType.TypeArguments[0]).Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(eventSourceType);

            // Define the constructor
            MethodDefinition ctor = MethodDefinition.CreateConstructor(
                module: module,
                parameterTypes: [
                    wellKnownInteropReferences.WindowsRuntimeObjectReference.Import(module).ToTypeSignature(isValueType: false),
                    module.CorLibTypeFactory.Int32]);

            eventSourceType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Ldarg_2);
            _ = ctor.CilMethodBody!.Instructions.Insert(3, Call, wellKnownInteropReferences.EventHandler1EventSource_ctor.Import(module));

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: wellKnownInteropReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToTypeSignature(isValueType: true),
                    parameterTypes: [delegateType.Import(module)]));

            eventSourceType.Methods.Add(convertToUnmanagedMethod);

            // Mark the 'ConvertToUnmanaged' method as overriding the base method
            eventSourceType.MethodImplementations.Add(new MethodImplementation(
                declaration: wellKnownInteropReferences.EventHandler1EventSourceConvertToUnmanaged(delegateType).Import(module),
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
