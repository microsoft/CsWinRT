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
                    new CustomAttribute(interopReferences.DynamicInterfaceCastableImplementationAttribute_ctor.Import(module)),
                    InteropCustomAttributeFactory.Guid(interfaceType, interopReferences, module, useWindowsUIXamlProjections)
                },
                Interfaces = { new InterfaceImplementation(interfaceType.Import(module).ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'INotifyPropertyChanged.PropertyChanged' add method
            MethodDefinition add_ICommandCanExecuteChangedMethod = new(
                name: "System.ComponentModel.INotifyPropertyChanged.add_PropertyChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.PropertyChangedEventHandler.Import(module).ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("PropertyChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences,
                    module: module)
            };

            // Add and implement the 'INotifyPropertyChanged.PropertyChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.INotifyPropertyChangedadd_PropertyChanged.Import(module),
                method: add_ICommandCanExecuteChangedMethod);

            // Create the 'INotifyPropertyChanged.PropertyChanged' remove method
            MethodDefinition remove_ICommandCanExecuteChangedMethod = new(
                name: "System.ComponentModel.INotifyPropertyChanged.remove_PropertyChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: module.CorLibTypeFactory.Void,
                    parameterTypes: [interopReferences.PropertyChangedEventHandler.Import(module).ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("PropertyChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences,
                    module: module)
            };

            // Add and implement the 'INotifyPropertyChanged.PropertyChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.INotifyPropertyChangedremove_PropertyChanged.Import(module),
                method: remove_ICommandCanExecuteChangedMethod);

            // Create the 'INotifyPropertyChanged.PropertyChanged' event
            EventDefinition observableMap2MapChangedProperty = new(
                name: "System.ComponentModel.INotifyPropertyChanged.PropertyChanged",
                attributes: default,
                eventType: interopReferences.PropertyChangedEventHandler.Import(module))
            {
                AddMethod = add_ICommandCanExecuteChangedMethod,
                RemoveMethod = remove_ICommandCanExecuteChangedMethod
            };

            interfaceImplType.Events.Add(observableMap2MapChangedProperty);
        }
    }
}