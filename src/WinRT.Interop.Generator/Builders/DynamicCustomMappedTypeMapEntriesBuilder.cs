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
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    public static void AssemblyAttributes(
        InteropGeneratorArgs args,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IEnumerable.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
            useComWrappersMarshallerAttribute: true);

        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IEnumerator.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
            useComWrappersMarshallerAttribute: true);

        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IList.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
            useComWrappersMarshallerAttribute: true);

        ICommandInterfaceType(
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        INotifyCollectionChangedInterfaceType(
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        INotifyPropertyChangedInterfaceType(
            interopDefinitions: interopDefinitions,
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

        ValueType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.NotifyCollectionChangedAction.ToValueTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        IBindableVectorViewType(
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
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
    /// <param name="useComWrappersMarshallerAttribute">Whether to also use the <c>[WindowsRuntimeComWrappersMarshaller]</c> attribute for <paramref name="trimTarget"/>.</param>
    private static void InterfaceType(
        string windowsUIXamlMetadata,
        string microsoftUIXamlMetadata,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections,
        bool useComWrappersMarshallerAttribute)
    {
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Only lookup the '[WindowsRuntimeComWrappersMarshaller]' attribute if requested. For instance, types such as 'IEnumerable'
        // will need one, so they can plug in specialized RCWs to better handle all the various scenarios where the interface is
        // used. Other interfaces, such as 'INotifyPropertyChanged', don't need that, and they will just rely on dynamic casts.
        TypeDefinition? comWrappersMarshallerAttribute = useComWrappersMarshallerAttribute
            ? GetMarshallerAttributeType(trimTarget, interopReferences, module)
            : null;

        // Define the proxy type for the interface type. Because the metadata type name depends on the XAML configuration
        // that is used, we can't define this proxy type in advance even if the interface type itself is not generic.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
            referenceType: null,
            comWrappersMarshallerAttributeType: comWrappersMarshallerAttribute,
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // For interface types (such as 'IEnumerable'), which don't need CCW support (because they are interfaces), we just
        // need the marshalling type map entry to support anonymous objects, and a metadata proxy type map entry to retrieve
        // the correct metadata info when marshalling 'Type' instances. We don't need to emit entries in the dynamic interface
        // castable type map, because those attributes are in 'WinRT.Runtime.dll' already (as the types are not generic).
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
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
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for the <see cref="System.Windows.Input.ICommand"/> interface type.
    /// </summary>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void ICommandInterfaceType(
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        const string windowsUIXamlMetadata = "Windows.Foundation.UniversalApiContract";
        const string microsoftUIXamlMetadata = "Microsoft.UI.Xaml.WinUIContract";

        TypeSignature trimTarget = interopReferences.ICommand.ToReferenceTypeSignature();
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the 'ICommand' interface type (it needs a dynamic one, same as the other interface types above)
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
            referenceType: null,
            comWrappersMarshallerAttributeType: null,
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // Define the 'InterfaceImpl' type for the 'ICommand' interface type
        ICommand.InterfaceImpl(
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: useWindowsUIXamlProjections,
            interfaceImplType: out TypeDefinition interfaceImplType);

        // Same logic as other interface types above, but we also need a '[DynamicInterfaceCastableImplementation]' type and entry
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            externalTypeMapTargetType: proxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: trimTarget,
            marshallingTypeMapSourceType: null,
            marshallingTypeMapProxyType: null,
            metadataTypeMapSourceType: trimTarget,
            metadataTypeMapProxyType: proxyType.ToTypeSignature(),
            interfaceTypeMapSourceType: trimTarget,
            interfaceTypeMapProxyType: interfaceImplType.ToTypeSignature(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for the <see cref="System.Collections.Specialized.INotifyCollectionChanged"/> interface type.
    /// </summary>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void INotifyCollectionChangedInterfaceType(
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        const string windowsUIXamlMetadata = "Windows.Foundation.UniversalApiContract";
        const string microsoftUIXamlMetadata = "Microsoft.UI.Xaml.WinUIContract";

        TypeSignature trimTarget = interopReferences.INotifyCollectionChanged.ToReferenceTypeSignature();
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the 'INotifyCollectionChanged' interface type
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
            referenceType: null,
            comWrappersMarshallerAttributeType: null,
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // Define the 'InterfaceImpl' type for the 'INotifyCollectionChanged' interface type
        INotifyCollectionChanged.InterfaceImpl(
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: useWindowsUIXamlProjections,
            interfaceImplType: out TypeDefinition interfaceImplType);

        // Same logic as other interface types above with '[DynamicInterfaceCastableImplementation]'
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            externalTypeMapTargetType: proxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: trimTarget,
            marshallingTypeMapSourceType: null,
            marshallingTypeMapProxyType: null,
            metadataTypeMapSourceType: trimTarget,
            metadataTypeMapProxyType: proxyType.ToTypeSignature(),
            interfaceTypeMapSourceType: trimTarget,
            interfaceTypeMapProxyType: interfaceImplType.ToTypeSignature(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface type.
    /// </summary>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void INotifyPropertyChangedInterfaceType(
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        const string windowsUIXamlMetadata = "Windows.Foundation.UniversalApiContract";
        const string microsoftUIXamlMetadata = "Microsoft.UI.Xaml.WinUIContract";

        TypeSignature trimTarget = interopReferences.INotifyPropertyChanged.ToReferenceTypeSignature();
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the 'INotifyPropertyChanged' interface type
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
            referenceType: null,
            comWrappersMarshallerAttributeType: null,
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // Define the 'InterfaceImpl' type for the 'INotifyPropertyChanged' interface type
        INotifyPropertyChanged.InterfaceImpl(
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: useWindowsUIXamlProjections,
            interfaceImplType: out TypeDefinition interfaceImplType);

        // Same logic as other interface types above with '[DynamicInterfaceCastableImplementation]'
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            externalTypeMapTargetType: proxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: trimTarget,
            marshallingTypeMapSourceType: null,
            marshallingTypeMapProxyType: null,
            metadataTypeMapSourceType: trimTarget,
            metadataTypeMapProxyType: proxyType.ToTypeSignature(),
            interfaceTypeMapSourceType: trimTarget,
            interfaceTypeMapProxyType: interfaceImplType.ToTypeSignature(),
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
            mappedMetadata: metadata,
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: null,
            mappedType: trimTarget,
            referenceType: null,
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
        // metadata type name is not fixed. This type can be instantiated and boxed, so we need both possible names.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedMetadata: metadata,
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
            referenceType: null,
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(trimTarget, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // Same as above for class types, the only difference here is in the proxy type definition for delegates
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
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
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped value type.
    /// </summary>
    /// <param name="windowsUIXamlMetadata">The metadata name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlMetadata">The metadata name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void ValueType(
        string windowsUIXamlMetadata,
        string microsoftUIXamlMetadata,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Define the proxy type for the value type. In this case we still need both the runtime class name and the
        // metadata type name, as above, but we also need to reference the boxed type instantiation for this type.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedMetadata: metadata,
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
            referenceType: interopReferences.Nullable1.MakeGenericValueType(trimTarget),
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(trimTarget, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            proxyType: out TypeDefinition proxyType);

        // Same as above for delegate types, with both a marshalling and a metadata type map entry
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
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
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for <c>IBindableVectorView</c>.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void IBindableVectorViewType(
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        const string windowsUIXamlTypeName = "Windows.UI.Xaml.Interop.IBindableVectorView";
        const string microsoftUIXamlTypeName = "Microsoft.UI.Xaml.Interop.IBindableVectorView";

        TypeSignature adapterType = interopReferences.BindableIReadOnlyListAdapter.ToReferenceTypeSignature();
        string runtimeClassName = useWindowsUIXamlProjections ? windowsUIXamlTypeName : microsoftUIXamlTypeName;

        // The 'BindableIReadOnlyListAdapter' type is a special type that is only used by the 'IBindableVector.GetView()'
        // implementation (as it returns an 'IBindableVectorView' instance). This type is only used as a CCW. So we need
        // a proxy type for it, with the correct runtime class name, and an entry just in the marshalling proxy type map.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(adapterType),
            name: InteropUtf8NameFactory.TypeName(adapterType),
            mappedMetadata: null,
            runtimeClassName: runtimeClassName,
            metadataTypeName: null,
            mappedType: null,
            referenceType: null,
            comWrappersMarshallerAttributeType: GetMarshallerAttributeType(adapterType, interopReferences, module),
            interopReferences: interopReferences,
            module: module,
            out TypeDefinition adapterProxyType);

        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: null,
            metadataTypeName: null,
            externalTypeMapTargetType: null,
            externalTypeMapTrimTargetType: null,
            marshallingTypeMapSourceType: adapterType,
            marshallingTypeMapProxyType: adapterProxyType.ToTypeSignature(),
            metadataTypeMapSourceType: null,
            metadataTypeMapProxyType: null,
            interfaceTypeMapSourceType: null,
            interfaceTypeMapProxyType: null,
            interopReferences: interopReferences,
            module: module);

        // Retrieve the '[IReadOnlyListComWrappersMarshallerAttribute]' type from 'WinRT.Runtime.dll', which is specialized
        TypeReference comWrappersMarshallerTypeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference(
            ns: "ABI.System.Collections"u8,
            name: "IReadOnlyListComWrappersMarshallerAttribute"u8);

        // Verify that this marshaller attribute does exist correctly and can be resolved
        if (comWrappersMarshallerTypeReference.Resolve(module) is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.NonProjectedTypeComWrappersMarshallerAttributeTypeResolveError(comWrappersMarshallerTypeReference, "IBindableVectorView");
        }

        // Create the proxy type to support RCW marshalling as well
        InteropTypeDefinitionBuilder.Proxy(
            ns: "ABI.System.Collections"u8,
            name: "<#corlib>IReadOnlyList",
            mappedMetadata: null,
            runtimeClassName: null,
            metadataTypeName: null,
            mappedType: null,
            referenceType: null,
            comWrappersMarshallerAttributeType: comWrappersMarshallerType,
            interopReferences: interopReferences,
            module: module,
            out TypeDefinition nativeObjectProxyType);

        // Define the type map entries. We only need one in the marshalling external type map. We can use 'IEnumerable'
        // as the trim target for the specialized RCW type (since the 'IReadOnlyList' interface does not exist in .NET).
        // This is also why we don't need to emit the '[WindowsRuntimeMappedMetadata]' attribute on the proxy type above.
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: runtimeClassName,
            metadataTypeName: null,
            externalTypeMapTargetType: nativeObjectProxyType.ToTypeSignature(),
            externalTypeMapTrimTargetType: interopReferences.IEnumerable.ToReferenceTypeSignature(),
            marshallingTypeMapSourceType: null,
            marshallingTypeMapProxyType: null,
            metadataTypeMapSourceType: null,
            metadataTypeMapProxyType: null,
            interfaceTypeMapSourceType: null,
            interfaceTypeMapProxyType: null,
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Retrieves the marshaller attribute associated with a specified custom-mapped type.
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
        if (comWrappersMarshallerTypeReference.Resolve(module) is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(type);
        }

        return comWrappersMarshallerType;
    }

    /// <summary>
    /// Retrieves the "Methods" type associated with a specified custom-mapped type.
    /// </summary>
    /// <param name="type">The custom-mapped type to retrieve the "Methods" type for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <returns>The "Methods" type for the input type.</returns>
    /// <exception cref="WellKnownInteropException">Thrown if resolving the "Methods" type fails.</exception>
    private static TypeDefinition GetMethodsType(
        TypeSignature type,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Retrieve the "Methods" type from 'WinRT.Runtime.dll', which always follows this naming convention
        TypeReference methodsTypeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference(
            ns: InteropUtf8NameFactory.TypeNamespace(type),
            name: (Utf8String)$"{type.Name}Methods");

        // The "Methods" type should always exist for all custom-mapped types, throw if it doesn't
        if (methodsTypeReference.Resolve(module) is not TypeDefinition methodsType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeMethodsTypeResolveError(type);
        }

        return methodsType;
    }
}