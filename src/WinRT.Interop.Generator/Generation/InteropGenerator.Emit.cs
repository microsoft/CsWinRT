// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet;
using System.IO;
using System;
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.References;
using System.Linq;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="state">The state for this invocation.</param>
    private static void Emit(InteropGeneratorArgs args, InteropGeneratorState state)
    {
        // Get the loaded module for the application .dll (this should always be available here)
        if (!state.ModuleDefinitions.TryGetValue(args.AssemblyPath, out ModuleDefinition? assemblyModule))
        {
            //throw new WellKnownInteropException("The assembly module was not found.");
        }

        // Get the loaded module for the runtime .dll (this should also always be available here)
        if (state.ModuleDefinitions.FirstOrDefault(static kvp => Path.GetFileName(kvp.Key).Equals("WinRT.Runtime2.dll")).Value is not ModuleDefinition winRTRuntime2Module)
        {
            winRTRuntime2Module = null!;
            //throw new WellKnownInteropException("The WinRT runtime module was not found.");
        }

        AssemblyDefinition winRTInteropAssembly = new(WellKnownInteropNames.InteropDllNameUtf8, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0));
        ModuleDefinition winRTInteropModule = new(WellKnownInteropNames.InteropDllName, assemblyModule.OriginalTargetRuntime.GetDefaultCorLib());

        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(assemblyModule.Assembly?.Name, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(winRTRuntime2Module.Assembly?.Name, winRTRuntime2Module.Assembly?.Version ?? new Version(0, 0, 0, 0)));
        winRTInteropModule.MetadataResolver = new DefaultMetadataResolver(state.AssemblyResolver);

        // Setup the well known items to use when emitting code
        WellKnownInteropDefinitions wellKnownInteropDefinitions = new(winRTInteropModule);
        WellKnownInteropReferences wellKnownInteropReferences = new(winRTInteropModule, winRTRuntime2Module);

        WindowsRuntimeTypeHierarchyBuilder.Lookup(
            state.TypeHierarchyEntries,
            wellKnownInteropDefinitions,
            wellKnownInteropReferences,
            winRTInteropModule,
            out _);

        foreach (GenericInstanceTypeSignature typeSignature in state.GenericDelegateTypes)
        {
            try
            {
                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateImplType,
                    iidRvaField: out _);

                // Define the 'DelegateReferenceImpl' type (with the boxed delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ReferenceImplType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateReferenceImplType,
                    iidRvaField: out _);

                // Define the 'DelegateInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.Delegate.InterfaceEntriesImplType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateInterfaceEntriesImplType);

                // Define the 'NativeDelegate' type (with the extension method implementation)
                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    nativeDelegateType: out TypeDefinition nativeDelegateType);

                // Define the 'ComWrappersCallback' type (with the 'IComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.Delegate.ComWrappersCallbackType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    nativeDelegateType: nativeDelegateType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    out TypeDefinition delegateComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.Delegate.ComWrappersMarshallerAttribute(
                    delegateType: typeSignature,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateInterfaceEntriesImplType: delegateInterfaceEntriesImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    out TypeDefinition delegateComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    marshallerType: out _);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.Delegate.Proxy(
                    delegateType: typeSignature,
                    delegateComWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    out _);
            }
            catch
            {
            }
        }

        foreach (GenericInstanceTypeSignature typeSignature in state.KeyValuePairTypes)
        {
            try
            {
                // Define the 'KeyValuePairImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition keyValuePairTypeImplType,
                    iidRvaField: out _);

                // Define the 'KeyValuePairInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.InterfaceEntriesImplType(
                    keyValuePairType: typeSignature,
                    keyValuePairTypeImplType: keyValuePairTypeImplType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out _);
            }
            catch
            {
            }
        }

        // Add all top level internal types to the interop module
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.RvaFields);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IUnknownVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IInspectableVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateReferenceVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateInterfaceEntries);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IKeyValuePairVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IKeyValuePairInterfaceEntries);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.InteropImplementationDetails);

        string winRTInteropAssemblyPath = Path.Combine(args.OutputDirectory, WellKnownInteropNames.InteropDllName);

        // Emit the interop .dll to disk
        winRTInteropModule.Write(winRTInteropAssemblyPath);
    }
}
