// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;
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
        // 'IEnumerable'
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections
                ? "Windows.UI.Xaml.Interop.IBindableIterable"
                : "Microsoft.UI.Xaml.Interop.IBindableIterable",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections"u8, "IEnumerable"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.IEnumerable.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module));

        // 'IEnumerator'
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections
                ? "Windows.UI.Xaml.Interop.IBindableIterator"
                : "Microsoft.UI.Xaml.Interop.IBindableIterator",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections"u8, "IEnumerator"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.IEnumerator.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module));

        // 'IList'
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections
                ? "Windows.UI.Xaml.Interop.IBindableVector"
                : "Microsoft.UI.Xaml.Interop.IBindableVector",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections"u8, "IList"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.IList.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module));

        // 'NotifyCollectionChangedEventArgs'
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections
                ? "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs"
                : "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections.Specialized"u8, "NotifyCollectionChangedEventArgs"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.NotifyCollectionChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module));

        // 'NotifyCollectionChangedEventHandler'
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections
                ? "Windows.Foundation.IReference<Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>"
                : "Windows.Foundation.IReference<Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler>",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.Collections.Specialized"u8, "NotifyCollectionChangedEventArgs"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.NotifyCollectionChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module));

        // NotifyCollectionChangedEventHandler // TODO

        // 'PropertyChangedEventArgs'
        module.Assembly!.CustomAttributes.Add(InteropCustomAttributeFactory.TypeMapWindowsRuntimeComWrappersTypeMapGroup(
            value: args.UseWindowsUIXamlProjections
                ? "Windows.UI.Xaml.Data.PropertyChangedEventArgs"
                : "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs",
            target: interopReferences.WindowsRuntimeModule.CreateTypeReference("ABI.System.ComponentModel"u8, "PropertyChangedEventArgs"u8).ToReferenceTypeSignature(),
            trimTarget: interopReferences.PropertyChangedEventArgs.ToReferenceTypeSignature(),
            interopReferences: interopReferences,
            module: module));

        // PropertyChangedEventHandler // TODO

        // BindableIReadOnlyListAdapter // TODO
    }
}
