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

        // TODO: also emit IDIC interface
        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.INotifyPropertyChanged.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        // TODO: also emit IDIC interface
        InterfaceType(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.INotifyCollectionChanged.ToReferenceTypeSignature(),
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
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
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
            mappedMetadata: metadata,
            runtimeClassName: RuntimeClassNameGenerator.GetRuntimeClassName(trimTarget, useWindowsUIXamlProjections),
            metadataTypeName: null,
            mappedType: trimTarget,
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
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            mappedType: trimTarget,
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
            comWrappersMarshallerAttributeType: comWrappersMarshallerType,
            interopReferences: interopReferences,
            module: module,
            out TypeDefinition nativeObjectProxyType);

        // Define the type map entries. We only need one in the marshalling external type map. We can use 'IEnumerable'
        // as the trim target for the specialized RCW type (since the 'IReadOnlyList' interface does not exist in .NET).
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
        if (comWrappersMarshallerTypeReference.Resolve(module) is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(type);
        }

        return comWrappersMarshallerType;
    }
}