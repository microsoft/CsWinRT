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
    /// Helpers for the <see cref="System.Windows.Input.ICommand"/> type.
    /// </summary>
    public static class ICommand
    {
        /// <summary>
        /// Creates a new type definition for the interface implementation of the <see cref="System.Windows.Input.ICommand"/> interface.
        /// </summary>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="module">The module that will contain the type being created.</param>
        /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
        /// <param name="interfaceImplType">The resulting interface implementation type.</param>
        public static void InterfaceImpl(
            InteropDefinitions interopDefinitions,
            InteropReferences interopReferences,
            ModuleDefinition module,
            bool useWindowsUIXamlProjections,
            out TypeDefinition interfaceImplType)
        {
            TypeSignature interfaceType = interopReferences.ICommand.ToReferenceTypeSignature();

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
                    InteropCustomAttributeFactory.Guid(interfaceType, interopDefinitions, interopReferences, useWindowsUIXamlProjections)
                },
                Interfaces = { new InterfaceImplementation(interfaceType.ToTypeDefOrRef()) }
            };

            module.TopLevelTypes.Add(interfaceImplType);

            // Create the 'ICommand.CanExecuteChanged' add method
            MethodDefinition add_ICommandCanExecuteChangedMethod = new(
                name: "System.Windows.Input.ICommand.add_CanExecuteChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.EventHandler.ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.EventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("CanExecuteChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.AddOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'ICommand.CanExecuteChanged' add accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICommandadd_CanExecuteChanged,
                method: add_ICommandCanExecuteChangedMethod);

            // Create the 'ICommand.CanExecuteChanged' remove method
            MethodDefinition remove_ICommandCanExecuteChangedMethod = new(
                name: "System.Windows.Input.ICommand.remove_CanExecuteChanged",
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceAccessorMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.EventHandler.ToReferenceTypeSignature()]))
            {
                CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                    interfaceType: interfaceType,
                    handlerType: interopReferences.EventHandler.ToReferenceTypeSignature(),
                    eventMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("CanExecuteChanged"u8),
                    eventAccessorAttributes: MethodSemanticsAttributes.RemoveOn,
                    interopReferences: interopReferences)
            };

            // Add and implement the 'ICommand.CanExecuteChanged' remove accessor method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICommandremove_CanExecuteChanged,
                method: remove_ICommandCanExecuteChangedMethod);

            // Create the 'ICommand.CanExecuteChanged' event
            EventDefinition canExecuteChangedProperty = new(
                name: "System.Windows.Input.ICommand.CanExecuteChanged",
                attributes: default,
                eventType: interopReferences.EventHandler)
            {
                AddMethod = add_ICommandCanExecuteChangedMethod,
                RemoveMethod = remove_ICommandCanExecuteChangedMethod
            };

            interfaceImplType.Events.Add(canExecuteChangedProperty);

            // Define the 'ICommand.CanExecute' method
            MethodDefinition canExecuteMethod = new(
                name: "System.Windows.Input.ICommand.CanExecute"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Boolean,
                    parameterTypes: [interopReferences.Object]));

            // Add and implement the 'CanExecute' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICommandCanExecute,
                method: canExecuteMethod);

            // Create a method body for the 'CanExecute' method
            canExecuteMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: interfaceType,
                implementationMethod: canExecuteMethod,
                forwardedMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("CanExecute"u8),
                interopReferences: interopReferences);

            // Define the 'ICommand.Execute' method
            MethodDefinition executeMethod = new(
                name: "System.Windows.Input.ICommand.Execute"u8,
                attributes: WellKnownMethodAttributesFactory.ExplicitInterfaceImplementationInstanceMethod,
                signature: MethodSignature.CreateInstance(
                    returnType: interopReferences.Void,
                    parameterTypes: [interopReferences.Object]));

            // Add and implement the 'Execute' method
            interfaceImplType.AddMethodImplementation(
                declaration: interopReferences.ICommandExecute,
                method: executeMethod);

            // Create a method body for the 'Execute' method
            executeMethod.CilMethodBody = WellKnownCilMethodBodyFactory.DynamicInterfaceCastableImplementation(
                interfaceType: interfaceType,
                implementationMethod: executeMethod,
                forwardedMethod: GetMethodsType(interfaceType, interopReferences, module).GetMethod("Execute"u8),
                interopReferences: interopReferences);
        }
    }
}