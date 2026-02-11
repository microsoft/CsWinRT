// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <inheritdoc cref="DynamicCustomMappedTypeMapEntriesBuilder"/>
internal partial class DynamicCustomMappedTypeMapEntriesBuilder
{
    /// <summary>
    /// Helpers for the <see cref="System.Collections.Specialized.INotifyCollectionChanged"/> type.
    /// </summary>
    public static class INotifyCollectionChanged
    {
        /// <summary>
        /// Creates a new type definition for the interface implementation of the <see cref="System.Collections.Specialized.INotifyCollectionChanged"/> interface.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature interfaceType = interopReferences.INotifyCollectionChanged.ToReferenceTypeSignature();

            // We're declaring an 'internal interface class' type
            interfaceImplType = new(
                ns: InteropUtf8NameFactory.TypeNamespace(interfaceType),
                name: InteropUtf8NameFactory.TypeName(interfaceType, "InterfaceImpl"),
                attributes: TypeAttributes.Interface | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.BeforeFieldInit,
                baseType: null)
            {
                CustomAttributes =
                {
                    new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor),
                    InteropCustomAttributeFactory.Guid(interfaceType, interopReferences, useWindowsUIXamlProjections)
                },
                Interfaces = { new InterfaceImplementation(interfaceType.ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'INotifyCollectionChanged.CollectionChanged' add method
            MethodDefinition add_INotifyCollectionChangedCollectionChangedMethod = new(
                name: "System.Collections.Specialized.INotifyCollectionChanged.add_CollectionChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("CollectionChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'INotifyCollectionChanged.CollectionChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.INotifyCollectionChangedadd_CollectionChanged,
                method: add_INotifyCollectionChangedCollectionChangedMethod);

            // Create the 'INotifyCollectionChanged.CollectionChanged' remove method
            MethodDefinition remove_INotifyCollectionChangedCollectionChangedMethod = new(
                name: "System.Collections.Specialized.INotifyCollectionChanged.remove_CollectionChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("CollectionChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'INotifyCollectionChanged.CollectionChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.INotifyCollectionChangedremove_CollectionChanged,
                method: remove_INotifyCollectionChangedCollectionChangedMethod);

            // Create the 'INotifyCollectionChanged.CollectionChanged' event
            EventDefinition collectionChangedProperty = new(
                name: "System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged",
                attributes: default,
                eventType: interopReferences.NotifyCollectionChangedEventHandler)
            {
                AddMethod = add_INotifyCollectionChangedCollectionChangedMethod,
                RemoveMethod = remove_INotifyCollectionChangedCollectionChangedMethod
            };

            interfaceImplType.Events.Add(collectionChangedProperty);
        }
    }
}