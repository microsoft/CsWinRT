// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.References;

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
        // Define the module to emit
        ModuleDefinition module = DefineInteropModule(args, state, out ModuleDefinition windowsRuntimeModule);

        // Setup the well known items to use when emitting code
        WellKnownInteropDefinitions wellKnownInteropDefinitions = new(module);
        WellKnownInteropReferences wellKnownInteropReferences = new(module, windowsRuntimeModule);

        // Emit the type hierarchy lookup
        WindowsRuntimeTypeHierarchyBuilder.Lookup(
            state.TypeHierarchyEntries,
            wellKnownInteropDefinitions,
            wellKnownInteropReferences,
            module,
            out _);

        // Emit interop types for generic delegates
        DefineGenericDelegateTypes(state, wellKnownInteropDefinitions, wellKnownInteropReferences, module);

        // Emit interop types for 'KeyValuePair<,>' types
        DefineKeyValuePairTypes(state, wellKnownInteropDefinitions, wellKnownInteropReferences, module);

        // Add all top level internal types to the interop module
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.RvaFields);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.IUnknownVftbl);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.IInspectableVftbl);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateVftbl);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateReferenceVftbl);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateInterfaceEntries);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.IKeyValuePairVftbl);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.IKeyValuePairInterfaceEntries);
        module.TopLevelTypes.Add(wellKnownInteropDefinitions.InteropImplementationDetails);

        // Emit the interop .dll to disk
        WriteInteropModuleToDisk(args, module);
    }

    /// <summary>
    /// Defines the interop module to emit.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="windowsRuntimeModule">The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.</param>
    /// <returns>The interop module to populate and emit.</returns>
    private static ModuleDefinition DefineInteropModule(InteropGeneratorArgs args, InteropGeneratorState state, out ModuleDefinition windowsRuntimeModule)
    {
        // Get the loaded module for the application .dll (this should always be available here)
        if (!state.ModuleDefinitions.TryGetValue(args.AssemblyPath, out ModuleDefinition? assemblyModule))
        {
            throw WellKnownInteropExceptions.AssemblyModuleNotFound();
        }

        // Get the loaded module for the runtime .dll (this should also always be available here)
        if ((windowsRuntimeModule = state.ModuleDefinitions.FirstOrDefault(static kvp => Path.GetFileName(kvp.Key).Equals("WinRT.Runtime2.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WinRTModuleNotFound();
        }

        ModuleDefinition winRTInteropModule = new(WellKnownInteropNames.InteropDllName, assemblyModule.OriginalTargetRuntime.GetDefaultCorLib());

        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(assemblyModule.Assembly?.Name, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(windowsRuntimeModule.Assembly?.Name, windowsRuntimeModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
        winRTInteropModule.MetadataResolver = new DefaultMetadataResolver(state.AssemblyResolver);

        return winRTInteropModule;
    }

    /// <summary>
    /// Defines the interop types for generic delegates.
    /// </summary>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineGenericDelegateTypes(
        InteropGeneratorState state,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in state.GenericDelegateTypes)
        {
            try
            {
                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    implType: out TypeDefinition delegateImplType,
                    iidRvaField: out _);

                // Define the 'DelegateReferenceImpl' type (with the boxed delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ReferenceImplType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    implType: out TypeDefinition delegateReferenceImplType,
                    iidRvaField: out _);

                // Define the 'DelegateInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.Delegate.InterfaceEntriesImplType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    implType: out TypeDefinition delegateInterfaceEntriesImplType);

                // Define the 'NativeDelegate' type (with the extension method implementation)
                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    nativeDelegateType: out TypeDefinition nativeDelegateType);

                // Define the 'ComWrappersCallback' type (with the 'IComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.Delegate.ComWrappersCallbackType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    nativeDelegateType: nativeDelegateType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.Delegate.ComWrappersMarshallerAttribute(
                    delegateType: typeSignature,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateInterfaceEntriesImplType: delegateInterfaceEntriesImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    marshallerType: out _);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.Delegate.Proxy(
                    delegateType: typeSignature,
                    delegateComWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    out _);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineKeyValuePairTypes(
        InteropGeneratorState state,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in state.KeyValuePairTypes)
        {
            try
            {
                // Define the 'KeyValuePairImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    implType: out TypeDefinition keyValuePairTypeImplType,
                    iidRvaField: out _);

                // Define the 'KeyValuePairInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.InterfaceEntriesImplType(
                    keyValuePairType: typeSignature,
                    keyValuePairTypeImplType: keyValuePairTypeImplType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: module,
                    implType: out _);
            }
            catch
            {
            }
        }
    }

    /// <summary>
    /// Writes the interop module to disk.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="module">The module to write to disk.</param>
    private static void WriteInteropModuleToDisk(InteropGeneratorArgs args, ModuleDefinition module)
    {
        string winRTInteropAssemblyPath = Path.Combine(args.OutputDirectory, WellKnownInteropNames.InteropDllName);

        try
        {
            module.Write(winRTInteropAssemblyPath);
        }
        catch (Exception e)
        {
            throw WellKnownInteropExceptions.EmitDllError(e);
        }
    }
}
