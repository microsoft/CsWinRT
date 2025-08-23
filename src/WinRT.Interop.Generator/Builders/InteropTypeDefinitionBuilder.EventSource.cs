// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
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
            DerivedEventSource(
                delegateType: delegateType,
                baseEventSourceType: interopReferences.EventHandler1EventSource,
                baseEventSource_ctor: interopReferences.EventHandler1EventSource_ctor,
                baseEventSourceConvertToUnmanaged: interopReferences.EventHandler1EventSourceConvertToUnmanaged(delegateType),
                marshallerType: marshallerType,
                interopReferences: interopReferences,
                module: module,
                eventSourceType: out eventSourceType);
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
            DerivedEventSource(
                delegateType: delegateType,
                baseEventSourceType: interopReferences.EventHandler2EventSource,
                baseEventSource_ctor: interopReferences.EventHandler2EventSource_ctor,
                baseEventSourceConvertToUnmanaged: interopReferences.EventHandler2EventSourceConvertToUnmanaged(delegateType),
                marshallerType: marshallerType,
                interopReferences: interopReferences,
                module: module,
                eventSourceType: out eventSourceType);
        }

        /// <summary>
        /// Creates a new type definition for the event source type for some <c>Windows.Foundation.Collections.VectorChangedEventHandler&lt;T&gt;</c> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the delegate type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Delegate.Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="eventSourceType">The resulting event source type.</param>
        public static void VectorChangedEventHandler1(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition eventSourceType)
        {
            DerivedEventSource(
                delegateType: delegateType,
                baseEventSourceType: interopReferences.VectorChangedEventHandler1EventSource,
                baseEventSource_ctor: interopReferences.VectorChangedEventHandler1EventSource_ctor,
                baseEventSourceConvertToUnmanaged: interopReferences.VectorChangedEventHandler1EventSourceConvertToUnmanaged(delegateType),
                marshallerType: marshallerType,
                interopReferences: interopReferences,
                module: module,
                eventSourceType: out eventSourceType);
        }

        /// <summary>
        /// Creates a new type definition for the event source type for some <c>Windows.Foundation.Collections.MapChangedEventHandler&lt;K, V&gt;</c> type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the delegate type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Delegate.Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="eventSourceType">The resulting event source type.</param>
        public static void MapChangedEventHandler2(
            GenericInstanceTypeSignature delegateType,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition eventSourceType)
        {
            DerivedEventSource(
                delegateType: delegateType,
                baseEventSourceType: interopReferences.EventHandler2EventSource,
                baseEventSource_ctor: interopReferences.MapChangedEventHandler2EventSource_ctor,
                baseEventSourceConvertToUnmanaged: interopReferences.MapChangedEventHandler2EventSourceConvertToUnmanaged(delegateType),
                marshallerType: marshallerType,
                interopReferences: interopReferences,
                module: module,
                eventSourceType: out eventSourceType);
        }

        /// <summary>
        /// Creates a new type definition for the event source type for some delegate type.
        /// </summary>
        /// <param name="delegateType">The <see cref="TypeSignature"/> for the delegate type.</param>
        /// <param name="baseEventSourceType">The <see cref="TypeReference"/> for the base event source type.</param>
        /// <param name="baseEventSource_ctor">The <see cref="MemberReference"/> for the constructor of the base event source type.</param>
        /// <param name="baseEventSourceConvertToUnmanaged">The <see cref="MemberReference"/> for the marshalling method of the base event source type.</param>
        /// <param name="marshallerType">The <see cref="TypeDefinition"/> instance returned by <see cref="Delegate.Marshaller"/>.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="eventSourceType">The resulting event source type.</param>
        private static void DerivedEventSource(
            GenericInstanceTypeSignature delegateType,
            TypeReference baseEventSourceType,
            MemberReference baseEventSource_ctor,
            MemberReference baseEventSourceConvertToUnmanaged,
            TypeDefinition marshallerType,
            InteropReferences interopReferences,
            ModuleDefinition module,
            out TypeDefinition eventSourceType)
        {
            TypeSignature baseEventSourceSignature = baseEventSourceType.MakeGenericReferenceType([.. delegateType.TypeArguments]);

            // We're declaring an 'internal sealed class' type
            eventSourceType = new(
                ns: "ABI.WindowsRuntime.InteropServices"u8,
                name: InteropUtf8NameFactory.TypeName(baseEventSourceSignature),
                attributes: TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                baseType: baseEventSourceSignature.Import(module).ToTypeDefOrRef());

            module.TopLevelTypes.Add(eventSourceType);

            // Define the constructor:
            //
            // public <EVENT_SOURCE_TYPE>(WindowsRuntimeObjectReference nativeObjectReference, int index)
            //     : base(nativeObjectReference, index)
            // {
            // }
            //
            // All the actual initialization logic is done in the base 'EventSource<T>' type.
            MethodDefinition ctor = MethodDefinition.CreateConstructor(
                module: module,
                parameterTypes: [
                    interopReferences.WindowsRuntimeObjectReference.Import(module).ToReferenceTypeSignature(),
                    module.CorLibTypeFactory.Int32]);

            eventSourceType.Methods.Add(ctor);

            _ = ctor.CilMethodBody!.Instructions.Insert(0, Ldarg_0);
            _ = ctor.CilMethodBody!.Instructions.Insert(1, Ldarg_1);
            _ = ctor.CilMethodBody!.Instructions.Insert(2, Ldarg_2);
            _ = ctor.CilMethodBody!.Instructions.Insert(3, Call, baseEventSource_ctor.Import(module));

            // Define the 'ConvertToUnmanaged' method as follows:
            //
            // public override WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(<DELEGATE_TYPE> value)
            MethodDefinition convertToUnmanagedMethod = new(
                name: "ConvertToUnmanaged"u8,
                attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.WindowsRuntimeObjectReferenceValue.Import(module).ToValueTypeSignature(),
                    parameterTypes: [delegateType.Import(module)]))
            {
                CilInstructions =
                {
                    { Ldarg_1 },
                    { Call, marshallerType.GetMethod("ConvertToUnmanaged"u8) },
                    { Ret }
                }
            };

            // Add and implement the 'ConvertToUnmanaged' method
            eventSourceType.AddMethodImplementation(
                declaration: baseEventSourceConvertToUnmanaged.Import(module),
                method: convertToUnmanagedMethod);
        }
    }
}
