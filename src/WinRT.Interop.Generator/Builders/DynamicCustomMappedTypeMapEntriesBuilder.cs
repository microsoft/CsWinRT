// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for the dynamic type map entries for custom-mapped types.
/// </summary>
internal static partial class DynamicCustomMappedTypeMapEntriesBuilder
{
    /// <summary>
    /// Defines all assembly attributes for dynamic custom-mapped type map entries.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    public static void AssemblyAttributes(
        InteropGeneratorArgs args,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IEnumerable.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IEnumerator.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IList.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        ClassType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.NotifyCollectionChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        ClassType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.PropertyChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        DelegateType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        DelegateType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        WindowsRuntimeExposedType(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Interop.IBindableVectorView",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Interop.IBindableVectorView",
            trimTarget: interopReferences.BindableIReadOnlyListAdapter.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped interface type.
    /// </summary>
    /// <param name="windowsUIXamlMetadata">The metadata name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlMetadata">The metadata name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void InterfaceType(
        string windowsUIXamlMetadata,
        string microsoftUIXamlMetadata,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the interface type. Because the metadata type name depends on the XAML configuration
        // that is used, we can't define this proxy type in advance even if the interface type itself is not generic.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedType: trimTarget,
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            referenceMappedType: true,
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(trimTarget, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // For interface types (such as 'IEnumerable'), which don't need CCW support (because they are interfaces), we just
        // need the marshalling type map entry to support anonymous objects, and a metadata proxy type map entry to retrieve
        // the correct metadata info when marshalling 'Type' instances. We don't need to emit entries in the dynamic interface
        // castable type map, because those attributes are in 'WinRT.Runtime.dll' already (as the types are not generic).
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: null,
            externalTypeMapTargetType: proxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: trimTarget,
            marshallingTypeMapSourceType: null,
            marshallingTypeMapProxyType: null,
            metadataTypeMapSourceType: trimTarget,
            metadataTypeMapProxyType: proxyType.ToTypeSignature(),
            interfaceTypeMapSourceType: null,
            interfaceTypeMapProxyType: null,
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped class type.
    /// </summary>
    /// <param name="windowsUIXamlMetadata">The metadata name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlMetadata">The metadata name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void ClassType(
        string windowsUIXamlMetadata,
        string microsoftUIXamlMetadata,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the class type. Same as above, we need to define this proxy type here, as the
        // metadata type name is not fixed. We define a runtime class name so that 'TypeName' marshalling will be
        // able to do lookups correctly (this attribute will be used, since the input type is a normal class type).
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedType: trimTarget,
            mappedMetadata: metadata,
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: null,
            referenceMappedType: true,
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(trimTarget, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // This is similar to interfaces above, with the difference being that because class objects can be instantiated,
        // the proxy type map used will only be with the marshalling type map group, and the metadata type map group is
        // not needed here. Same goes for the external type map: we just have a normal entry for marshalling there.
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: null,
            externalTypeMapTargetType: proxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: trimTarget,
            marshallingTypeMapSourceType: trimTarget,
            marshallingTypeMapProxyType: proxyType.ToTypeSignature(),
            metadataTypeMapSourceType: null,
            metadataTypeMapProxyType: null,
            interfaceTypeMapSourceType: null,
            interfaceTypeMapProxyType: null,
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped delegate type.
    /// </summary>
    /// <param name="windowsUIXamlMetadata">The metadata name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlMetadata">The metadata name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void DelegateType(
        string windowsUIXamlMetadata,
        string microsoftUIXamlMetadata,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the delegate type. Same as above, we need to define this proxy type here, as the
        // metadata type name is not fixed. We don't need a runtime class name because the type is managed-only. That
        // is, when marshalled to native it will be converted into a fully native object, so we don't need name lookups.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedType: trimTarget,
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            referenceMappedType: true,
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(trimTarget, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // Same as above for class types, the only difference here is in the proxy type definition for delegates
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: null,
            externalTypeMapTargetType: proxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: trimTarget,
            marshallingTypeMapSourceType: trimTarget,
            marshallingTypeMapProxyType: proxyType.ToTypeSignature(),
            metadataTypeMapSourceType: null,
            metadataTypeMapProxyType: null,
            interfaceTypeMapSourceType: null,
            interfaceTypeMapProxyType: null,
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped type that can be marshalled to native.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="windowsUIXamlTypeName">The runtime class name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlTypeName">The runtime class name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    private static void WindowsRuntimeExposedType(
        InteropGeneratorArgs args,
        string windowsUIXamlTypeName,
        string microsoftUIXamlTypeName,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        string runtimeClassName = args.UseWindowsUIXamlProjections ? windowsUIXamlTypeName : microsoftUIXamlTypeName;

        // Because these types can be instantiated and marshalled to native, but their runtime class name changes between 'Windows.UI.Xaml' and
        // 'Microsoft.UI.Xaml', we also need to generate a proxy type for them, so that we can annotate it with '[WindowsRuntimeClassName]' with
        // the correct runtime class name for the configuration actually being used at runtime by the current application or published library.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: trimTarget.Name!,
            mappedType: trimTarget,
            mappedMetadata: null,
            runtimeClassName: runtimeClassName,
            metadataTypeName: null,
            referenceMappedType: false,
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(trimTarget, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            out TypeDefinition proxyType);

        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections ? windowsUIXamlTypeName : microsoftUIXamlTypeName,
            target: proxyType.ToTypeSignature(),
            trimTarget: trimTarget,
            interopReferences: interopReferences,
            module: module));
    }

    /// <summary>
    /// Retrieves the marshaller attribute associated with a specified custom-mapped type.
    /// interop.
    /// </summary>
    /// <param name="type">The custom-mapped type to retrieve the marshaller attribute for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The marshaller attribute type for the input type.</returns>
    /// <exception cref="WellKnownInteropException">Thrown if resolving the marshaller attribute type fails.</exception>
    private static TypeDefinition GetMarshallerAttributeType(
        TypeSignature type,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Retrieve the '[ComWrappersMarshaller]' type from 'WinRT.Runtime.dll', which always follows this naming convention
        TypeReference comWrappersMarshallerTypeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference(
            ns: InteropUtf8NameFactory.TypeNamespace(type),
            name: (Utf8String)$"{type.Name}ComWrappersMarshallerAttribute");

        // The '[ComWrappersMarshaller]' type should always exist for all custom-mapped types, throw if it doesn't
        if (comWrappersMarshallerTypeReference.Import(module).Resolve() is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(comWrappersMarshallerTypeReference);
        }

        return comWrappersMarshallerType;
    }
}