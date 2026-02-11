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
    /// Helpers for the <see cref="System.ComponentModel.INotifyPropertyChanged"/> type.
    /// </summary>
    public static class INotifyPropertyChanged
    {
        /// <summary>
        /// Creates a new type definition for the interface implementation of the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface.
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
            TypeSignature interfaceType = interopReferences.INotifyPropertyChanged.ToReferenceTypeSignature();

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

            // Create the 'INotifyPropertyChanged.PropertyChanged' add method
            MethodDefinition add_INotifyPropertyChangedPropertyChangedMethod = new(
                name: "System.ComponentModel.INotifyPropertyChanged.add_PropertyChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("PropertyChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'INotifyPropertyChanged.PropertyChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.INotifyPropertyChangedadd_PropertyChanged,
                method: add_INotifyPropertyChangedPropertyChangedMethod);

            // Create the 'INotifyPropertyChanged.PropertyChanged' remove method
            MethodDefinition remove_INotifyPropertyChangedPropertyChangedMethod = new(
                name: "System.ComponentModel.INotifyPropertyChanged.remove_PropertyChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("PropertyChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'INotifyPropertyChanged.PropertyChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.INotifyPropertyChangedremove_PropertyChanged,
                method: remove_INotifyPropertyChangedPropertyChangedMethod);

            // Create the 'INotifyPropertyChanged.PropertyChanged' event
            EventDefinition propertyChangedProperty = new(
                name: "System.ComponentModel.INotifyPropertyChanged.PropertyChanged",
                attributes: default,
                eventType: interopReferences.PropertyChangedEventHandler)
            {
                AddMethod = add_INotifyPropertyChangedPropertyChangedMethod,
                RemoveMethod = remove_INotifyPropertyChangedPropertyChangedMethod
            };

            interfaceImplType.Events.Add(propertyChangedProperty);
        }
    }
}