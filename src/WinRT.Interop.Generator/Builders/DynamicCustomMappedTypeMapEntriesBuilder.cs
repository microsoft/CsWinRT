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
        Interface(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IEnumerable.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        Interface(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IEnumerator.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        Interface(
            windowsUIXamlMetadata: "Windows.Foundation.UniversalApiContract",
            microsoftUIXamlMetadata: "Microsoft.UI.Xaml.WinUIContract",
            trimTarget: interopReferences.IList.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module,
            useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);

        ManagedOnlyTypeOrInterface(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections.Specialized"u8, "NotifyCollectionChangedEventArgs"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.NotifyCollectionChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        ManagedOnlyTypeOrInterface(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Data.PropertyChangedEventArgs",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.ComponentModel"u8, "PropertyChangedEventArgs"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.PropertyChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        WindowsRuntimeExposedType(
            args: args,
            windowsUIXamlTypeName: "Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
            microsoftUIXamlTypeName: "Windows.Foundation.IReference`1<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
            trimTarget: interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        WindowsRuntimeExposedType(
            args: args,
            windowsUIXamlTypeName: "Windows.Foundation.IReference`1<Windows.UI.Xaml.Data.PropertyChangedEventHandler>",
            microsoftUIXamlTypeName: "Windows.Foundation.IReference`1<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>",
            trimTarget: interopReferences.PropertyChangedEventHandler.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        WindowsRuntimeExposedType(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Interop.IBindableVectorView",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Interop.IBindableVectorView",
            trimTarget: interopReferences.BindableIReadOnlyListAdapter.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);
    }

    /// <summary>
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped type that is never instantiated.
    /// </summary>
    /// <param name="windowsUIXamlMetadata">The metadata name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlMetadata">The metadata name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    private static void Interface(
        string windowsUIXamlMetadata,
        string microsoftUIXamlMetadata,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module,
        bool useWindowsUIXamlProjections)
    {
        string metadata = useWindowsUIXamlProjections ? windowsUIXamlMetadata : microsoftUIXamlMetadata;

        // Retrieve the '[ComWrappersMarshaller]' type from 'WinRT.Runtime.dll', which always follows this naming convention
        TypeReference comWrappersMarshallerTypeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: (Utf8String)$"{trimTarget.Name}ComWrappersMarshallerAttribute");

        // The '[ComWrappersMarshaller]' type should always exist for all custom-mapped types, throw if it doesn't
        if (comWrappersMarshallerTypeReference.Import(module).Resolve() is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(comWrappersMarshallerTypeReference);
        }

        // Define the proxy type for the interface. Because the metadata type name depends on the XAML configuration
        // being used, we can't define this proxy type in advance even if the interface type itself is not generic.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: InteropUtf8NameFactory.TypeName(trimTarget),
            mappedType: trimTarget,
            mappedMetadata: metadata,
            runtimeClassName: null,
            metadataTypeName: MetadataTypeNameGenerator.GetMetadataTypeName(trimTarget, useWindowsUIXamlProjections),
            referenceMappedType: true,
            comWrappersMarshallerAttributeType: comWrappersMarshallerType,
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
    /// Creates a new custom attribute value for <see cref="TypeMapAttribute{TTypeMapGroup}"/> for a given custom-mapped type that is never marshalled or instantiated.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="windowsUIXamlTypeName">The runtime class name for <c>Windows.UI.Xaml</c>.</param>
    /// <param name="microsoftUIXamlTypeName">The runtime class name for <c>Microsoft.UI.Xaml</c>.</param>
    /// <param name="target"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='target']/node()"/></param>
    /// <param name="trimTarget"><inheritdoc cref="TypeMapAttribute{TTypeMapGroup}.TypeMapAttribute(string, Type, Type)" path="/param[@name='trimTarget']/node()"/></param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The module that the attribute will be used from.</param>
    private static void ManagedOnlyTypeOrInterface(
        InteropGeneratorArgs args,
        string windowsUIXamlTypeName,
        string microsoftUIXamlTypeName,
        TypeSignature target,
        TypeSignature trimTarget,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // This method is used for two kinds of custom-mapped types:
        //   - "Managed-only" types (such as 'PropertyChangedEventArgs'), which don't need CCW support, because they are
        //     marshalled by activating a fully native instance. Because of this, the proxy type for these types doesn't
        //     need the runtime class name annotation on them, which allows it to be defined in 'WinRT.Runtime.dll'. That
        //     in turn also allows the '[TypeMapAssociation<TTypeMapGroup>]' attribute to be defined there. So here we
        //     only need the '[TypeMap<TTypeMapGroup>]' attribute to handle untyped native to managed marshalling.
        //   - Interface types (such as 'IEnumerable'), which also don't need CCW support (because they are interfaces).
        //     For those, the IDIC attributes are in 'WinRT.Runtime.dll', so here we again only need the external type map.
        InteropTypeDefinitionBuilder.TypeMapAttributes(
            runtimeClassName: args.UseWindowsUIXamlProjections ? windowsUIXamlTypeName : microsoftUIXamlTypeName,
            metadataTypeName: null,
            externalTypeMapTargetType: target,
            externalTypeMapTrimTargetType: trimTarget,
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

        // Retrieve the '[ComWrappersMarshaller]' type from 'WinRT.Runtime.dll', which always follows this naming convention
        TypeReference comWrappersMarshallerTypeReference = interopReferences.WindowsRuntimeModule.CreateTypeReference(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: (Utf8String)$"{trimTarget.Name}ComWrappersMarshallerAttribute");

        // The '[ComWrappersMarshaller]' type should always exist for all custom-mapped types, throw if it doesn't
        if (comWrappersMarshallerTypeReference.Import(module).Resolve() is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(comWrappersMarshallerTypeReference);
        }

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
            comWrappersMarshallerAttributeType: comWrappersMarshallerType,
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
}