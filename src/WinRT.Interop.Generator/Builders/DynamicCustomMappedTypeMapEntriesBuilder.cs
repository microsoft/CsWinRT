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
        ManagedOnlyTypeOrInterface(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Interop.IBindableIterable",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Interop.IBindableIterable",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections"u8, "IEnumerable"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.IEnumerable.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        ManagedOnlyTypeOrInterface(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Interop.IBindableIterator",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Interop.IBindableIterator",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections"u8, "IEnumerator"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.IEnumerator.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        ManagedOnlyTypeOrInterface(
            args: args,
            windowsUIXamlTypeName: "Windows.UI.Xaml.Interop.IBindableVector",
            microsoftUIXamlTypeName: "Microsoft.UI.Xaml.Interop.IBindableVector",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections"u8, "IList"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.IList.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

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
            windowsUIXamlTypeName: "Windows.Foundation.IReference<Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
            microsoftUIXamlTypeName: "Windows.Foundation.IReference<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
            trimTarget: interopReferences.NotifyCollectionChangedEventHandler.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module);

        WindowsRuntimeExposedType(
            args: args,
            windowsUIXamlTypeName: "Windows.Foundation.IReference<Windows.UI.Xaml.Data.PropertyChangedEventHandler>",
            microsoftUIXamlTypeName: "Windows.Foundation.IReference<Microsoft.UI.Xaml.Data.PropertyChangedEventHandler>",
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
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections ? windowsUIXamlTypeName : microsoftUIXamlTypeName,
            target: target,
            trimTarget: trimTarget,
            interopReferences: interopReferences));
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
        if (comWrappersMarshallerTypeReference.Resolve() is not TypeDefinition comWrappersMarshallerType)
        {
            throw WellKnownInteropExceptions.CustomMappedTypeComWrappersMarshallerAttributeTypeResolveError(comWrappersMarshallerTypeReference);
        }

        // Because these types can be instantiated and marshalled to native, but their runtime class name changes between 'Windows.UI.Xaml' and
        // 'Microsoft.UI.Xaml', we also need to generate a proxy type for them, so that we can annotate it with '[WindowsRuntimeClassName]' with
        // the correct runtime class name for the configuration actually being used at runtime by the current application or published library.
        InteropTypeDefinitionBuilder.Proxy(
            ns: InteropUtf8NameFactory.TypeNamespace(trimTarget),
            name: trimTarget.Name!,
            runtimeClassName: runtimeClassName,
            comWrappersMarshallerAttributeType: comWrappersMarshallerType,
            interopReferences: interopReferences,
            module: module,
            out TypeDefinition proxyType);

        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections ? windowsUIXamlTypeName : microsoftUIXamlTypeName,
            target: proxyType.ToTypeSignature(),
            trimTarget: trimTarget,
            interopReferences: interopReferences));
    }
}