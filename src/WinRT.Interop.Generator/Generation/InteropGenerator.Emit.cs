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
        args.Token.ThrowIfCancellationRequested();

        // Define the module to emit
        ModuleDefinition module = DefineInteropModule(args, state, out ModuleDefinition windowsRuntimeModule);

        args.Token.ThrowIfCancellationRequested();

        // Setup the well known items to use when emitting code
        InteropReferences interopReferences = new(module, windowsRuntimeModule);
        InteropDefinitions interopDefinitions = new(interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit the type hierarchy lookup
        WindowsRuntimeTypeHierarchyBuilder.Lookup(
            state.TypeHierarchyEntries,
            interopDefinitions,
            interopReferences,
            module,
            args.Token,
            out _);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for generic delegates
        DefineGenericDelegateTypes(args, state, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IEnumerator<T>' types
        DefineIEnumeratorTypes(args, state, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'KeyValuePair<,>' types
        DefineKeyValuePairTypes(args, state, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all top level internal types to the interop module
        DefineImplementationDetailTypes(interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all '[IgnoreAccessChecksTo]' attributes
        DefineIgnoreAccessChecksToAttributes(state, interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

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

        try
        {
            AssemblyDefinition winRTInteropAssembly = new(InteropNames.InteropDllNameUtf8, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0));
            ModuleDefinition winRTInteropModule = new(InteropNames.InteropDllName, assemblyModule.OriginalTargetRuntime.GetDefaultCorLib());

            winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(assemblyModule.Assembly?.Name, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
            winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(windowsRuntimeModule.Assembly?.Name, windowsRuntimeModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
            winRTInteropModule.MetadataResolver = new DefaultMetadataResolver(state.AssemblyResolver);

            // Add the module to the parent assembly
            winRTInteropAssembly.Modules.Add(winRTInteropModule);

            return winRTInteropModule;
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DefineInteropAssemblyError(e);
        }
    }

    /// <summary>
    /// Defines the interop types for generic delegates.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineGenericDelegateTypes(
        InteropGeneratorArgs args,
        InteropGeneratorState state,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in state.GenericDelegateTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateImplType,
                    iidRvaField: out _);

                // Define the 'DelegateReferenceImpl' type (with the boxed delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ReferenceImplType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateReferenceImplType,
                    iidRvaField: out _);

                // Define the 'DelegateInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.Delegate.InterfaceEntriesImplType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateInterfaceEntriesImplType);

                // Define the 'NativeDelegate' type (with the extension method implementation)
                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    nativeDelegateType: out TypeDefinition nativeDelegateType);

                // Define the 'ComWrappersCallback' type (with the 'IComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.Delegate.ComWrappersCallbackType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    nativeDelegateType: nativeDelegateType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.Delegate.ComWrappersMarshallerAttribute(
                    delegateType: typeSignature,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateInterfaceEntriesImplType: delegateInterfaceEntriesImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.Delegate.Proxy(
                    delegateType: typeSignature,
                    delegateComWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out _);

                // Define the 'EventSource' types (for when the delegate types are used for events on projected types)
                if (typeSignature.GenericType.Name?.AsSpan().StartsWith("EventHandler`1"u8) is true)
                {
                    InteropTypeDefinitionBuilder.EventSource.EventHandler1(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        module: module,
                        eventSourceType: out _);
                }
                else if (typeSignature.GenericType.Name?.AsSpan().StartsWith("TypedEventHandler`2"u8) is true)
                {
                    InteropTypeDefinitionBuilder.EventSource.EventHandler2(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        module: module,
                        eventSourceType: out _);
                }
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.DelegateTypeCodeGenerationError(typeSignature.Name, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.IEnumerator{T}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIEnumeratorTypes(
        InteropGeneratorArgs args,
        InteropGeneratorState state,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in state.IEnumerator1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.IEnumerator1.IIteratorMethods(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    iteratorMethodsType: out TypeDefinition iteratorMethodsType);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IEnumerator1TypeCodeGenerationError(typeSignature.Name, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineKeyValuePairTypes(
        InteropGeneratorArgs args,
        InteropGeneratorState state,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in state.KeyValuePairTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                // Define the 'KeyValuePairImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition keyValuePairTypeImplType,
                    iidRvaField: out _);

                // Define the 'KeyValuePairInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.InterfaceEntriesImplType(
                    keyValuePairType: typeSignature,
                    keyValuePairTypeImplType: keyValuePairTypeImplType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.KeyValuePairTypeCodeGenerationError(typeSignature.Name, e);
            }
        }
    }

    /// <summary>
    /// Defines the implementation detail types.
    /// </summary>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineImplementationDetailTypes(InteropDefinitions interopDefinitions, ModuleDefinition module)
    {
        try
        {
            module.TopLevelTypes.Add(interopDefinitions.RvaFields);
            module.TopLevelTypes.Add(interopDefinitions.IUnknownVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IInspectableVftbl);
            module.TopLevelTypes.Add(interopDefinitions.DelegateVftbl);
            module.TopLevelTypes.Add(interopDefinitions.DelegateReferenceVftbl);
            module.TopLevelTypes.Add(interopDefinitions.DelegateInterfaceEntries);
            module.TopLevelTypes.Add(interopDefinitions.IEnumerator1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IKeyValuePairVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IKeyValuePairInterfaceEntries);
            module.TopLevelTypes.Add(interopDefinitions.InteropImplementationDetails);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.ImplementationDetailTypeCodeGenerationError(e);
        }
    }

    /// <summary>
    /// Defines the <c>[IgnoreAccessChecksTo]</c> attribute, and applies it to the assembly for each input reference.
    /// </summary>
    /// <param name="state"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIgnoreAccessChecksToAttributes(
        InteropGeneratorState state,
        InteropDefinitions interopDefinitions,
        ModuleDefinition module)
    {
        try
        {
            // Emit the '[IgnoreAccessChecksTo]' type first
            module.TopLevelTypes.Add(interopDefinitions.IgnoreAccessChecksToAttribute);

            // Next, emit all the '[IgnoreAccessChecksTo]' attributes for each type
            IgnoreAccessChecksToBuilder.AssemblyAttributes(state.ModuleDefinitions.Values, interopDefinitions, module);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DefineIgnoreAccessChecksToAttributesError(e);
        }
    }

    /// <summary>
    /// Writes the interop module to disk.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="module">The module to write to disk.</param>
    private static void WriteInteropModuleToDisk(InteropGeneratorArgs args, ModuleDefinition module)
    {
        string winRTInteropAssemblyPath = Path.Combine(args.OutputDirectory, InteropNames.InteropDllName);

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
