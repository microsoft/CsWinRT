// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Fixups;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Rewriters;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    private static void Emit(InteropGeneratorArgs args, InteropGeneratorDiscoveryState discoveryState)
    {
        args.Token.ThrowIfCancellationRequested();

        // Initialize the emit state, which tracks all state to use during the emit phase specifically.
        // For instance, it enables fast lookups for type definitions referenced in multiple places.
        InteropGeneratorEmitState emitState = new();

        // Define the module to emit
        ModuleDefinition module = DefineInteropModule(
            args: args,
            discoveryState: discoveryState,
            windowsRuntimeModule: out ModuleDefinition windowsRuntimeModule,
            windowsFoundationModule: out ModuleDefinition windowsFoundationModule);

        args.Token.ThrowIfCancellationRequested();

        // Setup the well known items to use when emitting code
        InteropReferences interopReferences = new(module.CorLibTypeFactory, windowsRuntimeModule, windowsFoundationModule);
        InteropDefinitions interopDefinitions = new(
            interopReferences: interopReferences,
            windowsRuntimeProjectionModule: discoveryState.WinRTProjectionModuleDefinition!,
            windowsRuntimeComponentModule: discoveryState.WinRTComponentModuleDefinition);

        args.Token.ThrowIfCancellationRequested();

        // Emit the type hierarchy lookup
        WindowsRuntimeTypeHierarchyBuilder.Lookup(
            discoveryState.TypeHierarchyEntries,
            interopDefinitions,
            interopReferences,
            module,
            args.Token,
            out _);

        args.Token.ThrowIfCancellationRequested();

        // Add all default top level internal types to the interop module
        DefineDefaultImplementationDetailTypes(interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for generic delegates
        DefineGenericDelegateTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IEnumerator<T>' types
        DefineIEnumeratorTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IEnumerable<T>' types
        DefineIEnumerableTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IReadOnlyList<T>' types
        DefineIReadOnlyListTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IList<T>' types
        DefineIListTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IReadOnlyDictionary<TKey, TValue>' types
        DefineIReadOnlyDictionaryTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IDictionary<TKey, TValue>' types
        DefineIDictionaryTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'KeyValuePair<TKey, TValue>' types
        DefineKeyValuePairTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IMapChangedEventArgs<K, V>' types
        DefineIMapChangedEventArgsTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IObservableVector<T>' types
        DefineIObservableVectorTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IObservableMap<K, V>' types
        DefineIObservableMapTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IAsyncActionWithProgress<TProgress>' types
        DefineIAsyncActionWithProgressTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IAsyncOperation<TResult>' types
        DefineIAsyncOperationTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IAsyncOperationWithProgress<TResult, TProgress>' types
        DefineIAsyncOperationWithProgressTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IReadOnlyCollection<KeyValuePair<TKey, TValue>>' types
        DefineIReadOnlyCollectionKeyValuePair2Types(args, discoveryState, emitState, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'ICollection<KeyValuePair<TKey, TValue>>' types
        DefineICollectionKeyValuePair2Types(args, discoveryState, emitState, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for SZ array types
        DefineSzArrayTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Rewrite the IL methods of marshalling stubs needing two-pass generation
        RewriteMethodDefinitions(args, emitState, interopReferences);

        args.Token.ThrowIfCancellationRequested();

        // Apply fixups to all generated interop methods
        FixupMethodDefinitions(args, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for user-defined array types
        DefineUserDefinedTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all dynamic top level internal types to the interop module
        DefineDynamicImplementationDetailTypes(interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all dynamic type map entries for custom-mapped types
        DefineDynamicCustomMappedTypeMapEntries(args, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all '[IgnoresAccessChecksTo]' attributes
        DefineIgnoresAccessChecksToAttributes(discoveryState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        EmitMetadataAssemblyAttributes(interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit the interop .dll to disk
        WriteInteropModuleToDisk(args, module);
    }

    /// <summary>
    /// Defines the interop module to emit.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="windowsRuntimeModule">The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.</param>
    /// <param name="windowsFoundationModule">The <see cref="ModuleDefinition"/> for the Windows Runtine foundation projection assembly.</param>
    /// <returns>The interop module to populate and emit.</returns>
    private static ModuleDefinition DefineInteropModule(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        out ModuleDefinition windowsRuntimeModule,
        out ModuleDefinition windowsFoundationModule)
    {
        // Get the loaded module for the application .dll (this should always be available here)
        if (!discoveryState.ModuleDefinitions.TryGetValue(args.OutputAssemblyPath, out ModuleDefinition? assemblyModule))
        {
            throw WellKnownInteropExceptions.AssemblyModuleNotFound();
        }

        // Get the loaded module for the runtime .dll (this should also always be available here)
        if ((windowsRuntimeModule = discoveryState.ModuleDefinitions.FirstOrDefault(
            predicate: static kvp => Path.GetFileName(Path.Normalize(kvp.Key)).Equals("WinRT.Runtime.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WinRTRuntimeModuleNotFound();
        }

        // Get the loaded module for the Windows SDK projection .dll (same as above)
        if ((windowsFoundationModule = discoveryState.ModuleDefinitions.FirstOrDefault(
            predicate: static kvp => Path.GetFileName(Path.Normalize(kvp.Key)).Equals("Microsoft.Windows.SDK.NET.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WindowsSdkProjectionModuleNotFound();
        }

        // If assembly version validation is required, ensure that the 'cswinrtinteropgen' version matches that of 'WinRT.Runtime.dll'.
        // We only compare major and minor versions, as it's fine to ship small forward compatible fixes in revision updates.
        if (args.ValidateWinRTRuntimeAssemblyVersion)
        {
            Version? winRTRuntimeAssemblyVersion = windowsRuntimeModule.Assembly?.Version;
            Version? cswinrtinteropgenAssemblyVersion = typeof(InteropGenerator).Assembly.GetName().Version;

            if (winRTRuntimeAssemblyVersion?.EqualsInMajorAndMinorOnly(cswinrtinteropgenAssemblyVersion) is not true)
            {
                throw WellKnownInteropExceptions.WinRTRuntimeAssemblyVersionMismatch(winRTRuntimeAssemblyVersion, cswinrtinteropgenAssemblyVersion);
            }
        }

        try
        {
            // Create the module for the 'WinRT.Interop.dll' assembly, where we'll add all generated types to
            ModuleDefinition winRTInteropModule = new(InteropNames.WindowsRuntimeInteropDllNameUtf8, assemblyModule.OriginalTargetRuntime.GetDefaultCorLib())
            {
                // Create and set a metadata resolver from the assembly resolver that we created during the discovery phase (used for auto-import)
                MetadataResolver = new DefaultMetadataResolver(discoveryState.AssemblyResolver),

                // We need a deterministic MVID for the generated module, so we create one based on the input assemblies.
                // This logic will produce a hash from each .NET assembly that was loaded and analyzed during discovery.
                Mvid = MvidGenerator.CreateMvid(discoveryState.ModuleDefinitions.Keys)
            };

            // Also create a containing assembly for it (needed for the emit phase). We don't actually need the assembly
            // ourselves, but creating it and adding the module will update the declaring assembly for types added to it.
            _ = new AssemblyDefinition(InteropNames.WindowsRuntimeInteropAssemblyNameUtf8, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0))
            {
                Modules = { winRTInteropModule }
            };

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
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineGenericDelegateTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.GenericDelegateTypes.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.Delegate.IIDs(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod,
                    get_ReferenceIidMethod: out MethodDefinition get_ReferenceIidMethod);

                InteropTypeDefinitionBuilder.Delegate.Vftbl(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    nativeDelegateType: out TypeDefinition nativeDelegateType);

                InteropTypeDefinitionBuilder.Delegate.ComWrappersCallbackType(
                    delegateType: typeSignature,
                    nativeDelegateType: nativeDelegateType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersCallbackType);

                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    vftblType: vftblType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out TypeDefinition delegateImplType);

                InteropTypeDefinitionBuilder.Delegate.ReferenceImplType(
                    delegateType: typeSignature,
                    marshallerType: marshallerType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateReferenceImplType);

                InteropTypeDefinitionBuilder.Delegate.InterfaceEntriesImpl(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    get_IidMethod: get_IidMethod,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceEntriesImplType: out TypeDefinition delegateInterfaceEntriesImplType);

                InteropTypeDefinitionBuilder.Delegate.ComWrappersMarshallerAttribute(
                    delegateType: typeSignature,
                    delegateInterfaceEntriesImplType: delegateInterfaceEntriesImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Delegate.Proxy(
                    delegateType: typeSignature,
                    comWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.Delegate.TypeMapAttributes(
                    delegateType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    module: module);

                // Define the 'EventSource' types (for when the delegate types are used for events on projected types)
                if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.EventHandler1))
                {
                    InteropTypeDefinitionBuilder.EventSource.EventHandler1(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        module: module,
                        eventSourceType: out _);
                }
                else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.EventHandler2))
                {
                    InteropTypeDefinitionBuilder.EventSource.EventHandler2(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        module: module,
                        eventSourceType: out _);
                }
                else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.VectorChangedEventHandler1))
                {
                    InteropTypeDefinitionBuilder.EventSource.VectorChangedEventHandler1(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        emitState: emitState,
                        module: module,
                        eventSourceType: out _);
                }
                else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.MapChangedEventHandler2))
                {
                    InteropTypeDefinitionBuilder.EventSource.MapChangedEventHandler2(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        emitState: emitState,
                        module: module,
                        eventSourceType: out _);
                }
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.DelegateTypeCodeGenerationError(typeSignature.Name, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IEnumerator{T}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIEnumeratorTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IEnumerator1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IEnumerator1.IID(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IEnumerator1.ElementMarshaller(
                    enumeratorType: typeSignature,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    elementMarshallerType: out _);

                InteropTypeDefinitionBuilder.IEnumerator1.ImplType(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IEnumerator1.IIteratorMethods(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    iteratorMethodsType: out TypeDefinition iteratorMethodsType);

                InteropTypeDefinitionBuilder.IEnumerator1.Methods(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    enumeratorMethodsType: out _);

                InteropTypeDefinitionBuilder.IEnumerator1.NativeObject(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    nativeObjectType: out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersCallbackType(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    callbackType: out TypeDefinition enumeratorComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersMarshallerAttribute(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition enumeratorComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: enumeratorComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IEnumerator1.InterfaceImpl(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: enumeratorComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IEnumerator1TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IEnumerable{T}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIEnumerableTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IEnumerable1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IEnumerable1.Interface(
                    enumerableType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceType: out _);

                InteropTypeDefinitionBuilder.IEnumerable1.ImplType(
                    enumerableType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IEnumerable1.IIterableMethods(
                    enumerableType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    iterableMethodsType: out TypeDefinition iterableMethodsType);

                InteropTypeDefinitionBuilder.IEnumerable1.Methods(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    enumerableMethodsType: out _);

                InteropTypeDefinitionBuilder.IEnumerable1.NativeObject(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    nativeObjectType: out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersCallbackType(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    callbackType: out TypeDefinition enumerableComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersMarshallerAttribute(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition enumerableComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: enumerableComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IEnumerable1.InterfaceImpl(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: enumerableComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IEnumerable1TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IReadOnlyList{T}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIReadOnlyListTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IReadOnlyList1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IReadOnlyList1.Vftbl(
                    readOnlyListType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.ImplType(
                    readOnlyListType: typeSignature,
                    vftblType: vftblType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IReadOnlyList1.IVectorViewMethods(
                    readOnlyListType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vectorViewMethodsType: out TypeDefinition vectorViewMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.Methods(
                    readOnlyListType: typeSignature,
                    vectorViewMethodsType: vectorViewMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    readOnlyListMethodsType: out TypeDefinition readOnlyListMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.NativeObject(
                    readOnlyListType: typeSignature,
                    vectorViewMethodsType: vectorViewMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersCallbackType(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition readOnlyListComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersMarshallerAttribute(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: readOnlyListComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.InterfaceImpl(
                    readOnlyListType: typeSignature,
                    readOnlyListMethodsType: readOnlyListMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.TypeMapAttributes(
                    readOnlyListType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IReadOnlyList1TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IList{T}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIListTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IList1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IList1.Interface(
                    listType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceType: out _);

                InteropTypeDefinitionBuilder.IList1.Vftbl(
                    listType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                InteropTypeDefinitionBuilder.IList1.ImplType(
                    listType: typeSignature,
                    vftblType: vftblType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IList1.IVectorMethods(
                    listType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vectorMethodsType: out TypeDefinition vectorMethodsType);

                InteropTypeDefinitionBuilder.IList1.Methods(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    listMethodsType: out TypeDefinition listMethodsType);

                InteropTypeDefinitionBuilder.IList1.NativeObject(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IList1.ComWrappersCallbackType(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition listComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IList1.ComWrappersMarshallerAttribute(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition listComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: listComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IList1.InterfaceImpl(
                    listType: typeSignature,
                    listMethodsType: listMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: listComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IList1.TypeMapAttributes(
                    listType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IList1TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIReadOnlyDictionaryTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IReadOnlyDictionary2Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Vftbl(
                    readOnlyDictionaryType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ImplType(
                    readOnlyDictionaryType: typeSignature,
                    vftblType: vftblType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.IMapViewMethods(
                    readOnlyDictionaryType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    mapViewMethodsType: out TypeDefinition mapViewMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Methods(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    readOnlyDictionaryMethodsType: out TypeDefinition readOnlyDictionaryMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.NativeObject(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersCallbackType(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition readOnlyDictionaryComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersMarshallerAttribute(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyDictionaryComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: readOnlyDictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.InterfaceImpl(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryMethodsType: readOnlyDictionaryMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: readOnlyDictionaryComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IReadOnlyDictionary2TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IDictionary{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIDictionaryTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IDictionary2Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IDictionary2.Interface(
                    dictionaryType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceType: out _);

                InteropTypeDefinitionBuilder.IDictionary2.Vftbl(
                    dictionaryType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                InteropTypeDefinitionBuilder.IDictionary2.ImplType(
                    dictionaryType: typeSignature,
                    vftblType: vftblType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IDictionary2.IMapMethods(
                    dictionaryType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    mapMethodsType: out TypeDefinition mapMethodsType);

                InteropTypeDefinitionBuilder.IDictionary2.Methods(
                    dictionaryType: typeSignature,
                    mapMethodsType: mapMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    dictionaryMethodsType: out TypeDefinition dictionaryMethodsType);

                InteropTypeDefinitionBuilder.IDictionary2.NativeObject(
                    dictionaryType: typeSignature,
                    mapMethodsType: mapMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersCallbackType(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition dictionaryComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersMarshallerAttribute(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition dictionaryComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: dictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IDictionary2.InterfaceImpl(
                    dictionaryType: typeSignature,
                    dictionaryMethodsType: dictionaryMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: dictionaryComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IDictionary2TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineKeyValuePairTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        TypeDefinition? methodsType = null;

        // Generate shared code for helpers needed by instantiated 'KeyValuePair<,>' types
        try
        {
            InteropTypeDefinitionBuilder.KeyValuePair.Methods(
                interopReferences: interopReferences,
                module: module,
                methodsType: out methodsType);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.KeyValuePairTypeSharedCodeGenerationError(e).ThrowOrAttach(e);
        }

        // Generate specialized code for all discovered instantiations
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.KeyValuePairTypes.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out TypeDefinition keyValuePairTypeImplType);

                InteropTypeDefinitionBuilder.KeyValuePair.InterfaceEntriesImplType(
                    keyValuePairType: typeSignature,
                    keyValuePairTypeImplType: keyValuePairTypeImplType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition interfaceEntriesImplType);

                InteropTypeDefinitionBuilder.KeyValuePair.Accessors(
                    keyValuePairType: typeSignature,
                    methodsType: methodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    keyAccessorMethod: out MethodDefinition keyAccessorMethod,
                    valueAccessorMethod: out MethodDefinition valueAccessorMethod);

                InteropTypeDefinitionBuilder.KeyValuePair.Marshaller(
                    keyValuePairType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    keyAccessorMethod: keyAccessorMethod,
                    valueAccessorMethod: valueAccessorMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.KeyValuePair.ComWrappersMarshallerAttribute(
                    keyValuePairType: typeSignature,
                    keyValuePairMarshallerType: marshallerType,
                    keyValuePairInterfaceEntriesImplType: interfaceEntriesImplType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerAttributeType: out TypeDefinition marshallerAttributeType);

                InteropTypeDefinitionBuilder.KeyValuePair.Proxy(
                    keyValuePairType: typeSignature,
                    comWrappersMarshallerAttributeType: marshallerAttributeType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.KeyValuePair.TypeMapAttributes(
                    keyValuePairType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.KeyValuePairTypeCodeGenerationError(typeSignature.Name, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIMapChangedEventArgsTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IMapChangedEventArgs1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.ImplType(
                    argsType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.Methods(
                    argsType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    argsMethodsType: out TypeDefinition argsMethodsType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.NativeObject(
                    argsType: typeSignature,
                    argsMethodsType: argsMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.ComWrappersCallbackType(
                    argsType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition argsComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.ComWrappersMarshallerAttribute(
                    argsType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition argsComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: argsComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.InterfaceImpl(
                    argsType: typeSignature,
                    argsMethodsType: argsMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: argsComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IMapChangedEventArgs1TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIObservableVectorTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IObservableVector1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IObservableVector1.ImplType(
                    vectorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IObservableVector1.EventSourceFactory(
                    vectorType: typeSignature,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    factoryType: out TypeDefinition factoryType);

                InteropTypeDefinitionBuilder.IObservableVector1.EventSourceCallback(
                    vectorType: typeSignature,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    callbackType: out TypeDefinition callbackType);

                InteropTypeDefinitionBuilder.IObservableVector1.Methods(
                    vectorType: typeSignature,
                    eventSourceCallbackType: callbackType,
                    interopReferences: interopReferences,
                    module: module,
                    methodsType: out TypeDefinition methodsType);

                InteropTypeDefinitionBuilder.IObservableVector1.NativeObject(
                    vectorType: typeSignature,
                    factoryType: factoryType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IObservableVector1.ComWrappersCallbackType(
                    vectorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition comWrappersCallbackType);

                InteropTypeDefinitionBuilder.IObservableVector1.ComWrappersMarshallerAttribute(
                    vectorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: comWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IObservableVector1.InterfaceImpl(
                    vectorType: typeSignature,
                    vectorMethodsType: methodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: comWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IObservableVectorTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIObservableMapTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IObservableMap2Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IObservableMap2.ImplType(
                    mapType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IObservableMap2.EventSourceCallback(
                    mapType: typeSignature,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    callbackType: out TypeDefinition factoryType);

                InteropTypeDefinitionBuilder.IObservableMap2.Methods(
                    mapType: typeSignature,
                    eventSourceFactoryType: factoryType,
                    interopReferences: interopReferences,
                    module: module,
                    methodsType: out TypeDefinition methodsType);

                InteropTypeDefinitionBuilder.IObservableMap2.NativeObject(
                    mapType: typeSignature,
                    mapMethodsType: methodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IObservableMap2.ComWrappersCallbackType(
                    mapType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition comWrappersCallbackType);

                InteropTypeDefinitionBuilder.IObservableMap2.ComWrappersMarshallerAttribute(
                    mapType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: comWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IObservableMap2.InterfaceImpl(
                    mapType: typeSignature,
                    mapMethodsType: methodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: comWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IObservableMapTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIAsyncActionWithProgressTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IAsyncActionWithProgress1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.ImplType(
                    actionType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.Methods(
                    actionType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    actionMethodsType: out TypeDefinition actionMethodsType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.NativeObject(
                    actionType: typeSignature,
                    actionMethodsType: actionMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.ComWrappersCallbackType(
                    actionType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition actionComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.ComWrappersMarshallerAttribute(
                    actionType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition actionComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: actionComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.InterfaceImpl(
                    actionType: typeSignature,
                    actionMethodsType: actionMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: actionComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IAsyncActionWithProgressTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIAsyncOperationTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IAsyncOperation1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IAsyncOperation1.ImplType(
                    operationType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IAsyncOperation1.Methods(
                    operationType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    operationMethodsType: out TypeDefinition operationMethodsType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.NativeObject(
                    operationType: typeSignature,
                    operationMethodsType: operationMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.ComWrappersCallbackType(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition operationComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.ComWrappersMarshallerAttribute(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition operationComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: operationComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.InterfaceImpl(
                    operationType: typeSignature,
                    operationMethodsType: operationMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: operationComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IAsyncOperationTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIAsyncOperationWithProgressTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IAsyncOperationWithProgress2Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.ImplType(
                    operationType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.Methods(
                    operationType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    operationMethodsType: out TypeDefinition operationMethodsType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.NativeObject(
                    operationType: typeSignature,
                    operationMethodsType: operationMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.ComWrappersCallbackType(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition operationComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.ComWrappersMarshallerAttribute(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition operationComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: operationComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.InterfaceImpl(
                    operationType: typeSignature,
                    operationMethodsType: operationMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: operationComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.TypeMapAttributes(
                    interfaceType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IAsyncOperationWithProgressTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="IReadOnlyCollection{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIReadOnlyCollectionKeyValuePair2Types(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IReadOnlyList1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            // Filter out to 'IReadOnlyList<KeyValuePair<,>>' instantiations
            if (!typeSignature.TypeArguments[0].IsConstructedKeyValuePairType(interopReferences))
            {
                continue;
            }

            // Construct the 'IReadOnlyCollection<KeyValuePair<,>>' type for processing
            GenericInstanceTypeSignature readOnlyCollectionType = interopReferences.IReadOnlyCollection1.MakeGenericReferenceType(typeSignature.TypeArguments[0]);

            try
            {
                InteropTypeDefinitionBuilder.IReadOnlyCollectionKeyValuePair2.ForwarderAttribute(
                    readOnlyCollectionType: readOnlyCollectionType,
                    interopReferences: interopReferences,
                    module: module,
                    forwarderAttributeType: out TypeDefinition forwarderAttributeType);

                InteropTypeDefinitionBuilder.IReadOnlyCollectionKeyValuePair2.InterfaceImpl(
                    readOnlyCollectionType: readOnlyCollectionType,
                    forwarderAttributeType: forwarderAttributeType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IReadOnlyCollectionKeyValuePair2.TypeMapAttributes(
                    readOnlyCollectionType: readOnlyCollectionType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IReadOnlyCollectionKeyValuePairTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="ICollection{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineICollectionKeyValuePair2Types(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IList1Types.OrderByFullyQualifiedTypeName())
        {
            args.Token.ThrowIfCancellationRequested();

            // Filter out to 'IList<KeyValuePair<,>>' instantiations
            if (!typeSignature.TypeArguments[0].IsConstructedKeyValuePairType(interopReferences))
            {
                continue;
            }

            // Construct the 'ICollection<KeyValuePair<,>>' type for processing
            GenericInstanceTypeSignature collectionType = interopReferences.ICollection1.MakeGenericReferenceType(typeSignature.TypeArguments[0]);

            try
            {
                InteropTypeDefinitionBuilder.ICollectionKeyValuePair2.ForwarderAttribute(
                    collectionType: collectionType,
                    interopReferences: interopReferences,
                    module: module,
                    forwarderAttributeType: out TypeDefinition forwarderAttributeType);

                InteropTypeDefinitionBuilder.ICollectionKeyValuePair2.InterfaceImpl(
                    collectionType: collectionType,
                    forwarderAttributeType: forwarderAttributeType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.ICollectionKeyValuePair2.TypeMapAttributes(
                    collectionType: collectionType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.ICollectionKeyValuePairTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for SZ array types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineSzArrayTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach ((SzArrayTypeSignature typeSignature, TypeSignatureEquatableSet vtableTypes) in discoveryState.SzArrayAndVtableTypes.OrderByFullyQualifiedTypeName(static pair => pair.Key))
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.SzArray.Marshaller(
                    arrayType: typeSignature,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.SzArray.ComWrappersCallback(
                    arrayType: typeSignature,
                    marshallerType: marshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition arrayComWrappersCallbackType);

                InteropTypeDefinitionBuilder.SzArray.ArrayImpl(
                    arrayType: typeSignature,
                    marshallerType: marshallerType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition arrayImplType);

                InteropTypeDefinitionBuilder.SzArray.InterfaceEntriesImpl(
                    arrayType: typeSignature,
                    vtableTypes: vtableTypes,
                    implType: arrayImplType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceEntriesType: out TypeDefinition interfaceEntriesType,
                    interfaceEntriesImplType: out TypeDefinition arrayInterfaceEntriesImplType);

                InteropTypeDefinitionBuilder.SzArray.ComWrappersMarshallerAttribute(
                    arrayType: typeSignature,
                    arrayInterfaceEntriesType: interfaceEntriesType,
                    arrayInterfaceEntriesImplType: arrayInterfaceEntriesImplType,
                    arrayComWrappersCallbackType: arrayComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition arrayComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.SzArray.Proxy(
                    arrayType: typeSignature,
                    comWrappersMarshallerAttributeType: arrayComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.SzArray.TypeMapAttributes(
                    arrayType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.SzArrayTypeCodeGenerationError(typeSignature.Name, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Rewrites IL method bodies for marshalling stubs as part of two-pass IL generation.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    private static void RewriteMethodDefinitions(
        InteropGeneratorArgs args,
        InteropGeneratorEmitState emitState,
        InteropReferences interopReferences)
    {
        // We need to sort all items to a temporary list first, as the underlying items
        // might otherwise change as we rewrite methods. This is because e.g. the target
        // instructions will get replaced by marshalling code, meaning trying to compare
        // different objects based on the index of those instructions in the target method
        // would produce different results. This temporary list avoids that issue.
        List<MethodRewriteInfo> rewriteInfos = [.. emitState.EnumerateMethodRewriteInfos().Order()];

        foreach (MethodRewriteInfo rewriteInfo in rewriteInfos)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                switch (rewriteInfo)
                {
                    // Rewrite direct calls to 'ConvertToUnmanaged' (or 'BoxToUnmanaged')
                    case MethodRewriteInfo.RawRetVal rawRetValInfo:
                        InteropMethodRewriter.RawRetVal.RewriteMethod(
                            parameterType: rawRetValInfo.Type,
                            method: rawRetValInfo.Method,
                            marker: rawRetValInfo.Marker,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;

                    // Rewrite return values for managed types
                    case MethodRewriteInfo.ReturnValue returnValueInfo:
                        InteropMethodRewriter.ReturnValue.RewriteMethod(
                            returnType: returnValueInfo.Type,
                            method: returnValueInfo.Method,
                            marker: returnValueInfo.Marker,
                            source: returnValueInfo.Source,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;

                    // Rewrite return values for native types
                    case MethodRewriteInfo.RetVal retValInfo:
                        InteropMethodRewriter.RetVal.RewriteMethod(
                            retValType: retValInfo.Type,
                            method: retValInfo.Method,
                            marker: retValInfo.Marker,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;

                    // Rewrite managed values
                    case MethodRewriteInfo.ManagedValue managedValueInfo:
                        InteropMethodRewriter.ManagedValue.RewriteMethod(
                            parameterType: managedValueInfo.Type,
                            method: managedValueInfo.Method,
                            marker: managedValueInfo.Marker,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;

                    // Rewrite managed parameters
                    case MethodRewriteInfo.ManagedParameter managedParameterInfo:
                        InteropMethodRewriter.ManagedParameter.RewriteMethod(
                            parameterType: managedParameterInfo.Type,
                            method: managedParameterInfo.Method,
                            marker: managedParameterInfo.Marker,
                            parameterIndex: managedParameterInfo.ParameterIndex,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;

                    // Rewrite native parameters
                    case MethodRewriteInfo.NativeParameter nativeParameterInfo:
                        InteropMethodRewriter.NativeParameter.RewriteMethod(
                            parameterType: nativeParameterInfo.Type,
                            method: nativeParameterInfo.Method,
                            tryMarker: nativeParameterInfo.TryMarker,
                            loadMarker: nativeParameterInfo.Marker,
                            finallyMarker: nativeParameterInfo.FinallyMarker,
                            parameterIndex: nativeParameterInfo.ParameterIndex,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;

                    // Rewrite direct calls to 'Dispose' (or the appropriate 'Free' method)
                    case MethodRewriteInfo.Dispose disposeInfo:
                        InteropMethodRewriter.Dispose.RewriteMethod(
                            parameterType: disposeInfo.Type,
                            method: disposeInfo.Method,
                            marker: disposeInfo.Marker,
                            interopReferences: interopReferences,
                            emitState: emitState);
                        break;
                    default: throw new UnreachableException();
                }
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.MethodRewriteError(rewriteInfo.Type, rewriteInfo.Method, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Applies fixups to IL method bodies for marshalling stubs as part of two-pass IL generation.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="module">The interop module being built.</param>
    private static void FixupMethodDefinitions(InteropGeneratorArgs args, ModuleDefinition module)
    {
        ReadOnlySpan<InteropMethodFixup> fixups =
        [
            InteropMethodFixup.RemoveLeftoverNopAfterLeave.Instance,
            InteropMethodFixup.RemoveUnnecessaryTryStartNop.Instance
        ];

        // Applies all available fixups in order, to all methods across all generated types in the module
        foreach (TypeDefinition type in module.GetAllTypes())
        {
            args.Token.ThrowIfCancellationRequested();

            foreach (MethodDefinition method in type.Methods)
            {
                foreach (InteropMethodFixup fixup in fixups)
                {
                    try
                    {
                        fixup.Apply(method);
                    }
                    catch (Exception e)
                    {
                        WellKnownInteropExceptions.MethodFixupError(fixup, method, e).ThrowOrAttach(e);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Defines the interop types for user-defined types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineUserDefinedTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitState emitState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        // Since we're sharing the marshaller attributes across all identical sets of COM interface entries,
        // we need a temporary map so we can look them up when we need to reference them once we get to
        // emitting the proxy types for all user-defined types we want to expose to Windows Runtime.
        Dictionary<TypeSignatureEquatableSet, TypeDefinition> marshallerAttributeMap = [];

        // We first need to emit all the shared COM interface entries types, as we'll aggressively share them
        foreach (TypeSignatureEquatableSet vtableTypes in discoveryState.UserDefinedVtableTypes.Order())
        {
            args.Token.ThrowIfCancellationRequested();

            TypeSignature? typeSignature = null;

            try
            {
                // Get the first user-defined with this vtable set as reference
                typeSignature = discoveryState.UserDefinedAndVtableTypes
                    .Where(kvp => kvp.Value.Equals(vtableTypes))
                    .Select(static kvp => kvp.Key)
                    .OrderByFullyQualifiedTypeName()
                    .First();

                InteropTypeDefinitionBuilder.UserDefinedType.InterfaceEntriesImpl(
                    userDefinedType: typeSignature,
                    vtableTypes: vtableTypes,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceEntriesType: out TypeDefinition interfaceEntriesType,
                    interfaceEntriesImplType: out TypeDefinition interfaceEntriesImplType);

                InteropTypeDefinitionBuilder.UserDefinedType.ComWrappersMarshallerAttribute(
                    userDefinedType: typeSignature,
                    interfaceEntriesType: interfaceEntriesType,
                    interfaceEntriesImplType: interfaceEntriesImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                // Track the marshaller attribute for later
                marshallerAttributeMap.Add(vtableTypes, comWrappersMarshallerType);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.UserDefinedVtableTypeCodeGenerationError(typeSignature?.Name, e).ThrowOrAttach(e);
            }
        }

        // Next, we can emit the actual proxy types for each user-defined type exposed as a CCW
        foreach ((TypeSignature typeSignature, TypeSignatureEquatableSet vtableTypes) in discoveryState.UserDefinedAndVtableTypes.OrderByFullyQualifiedTypeName(static pair => pair.Key))
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.UserDefinedType.Proxy(
                    userDefinedType: typeSignature,
                    comWrappersMarshallerAttributeType: marshallerAttributeMap[vtableTypes],
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.UserDefinedType.TypeMapAttributes(
                    userDefinedType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.UserDefinedTypeCodeGenerationError(typeSignature.Name, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the default implementation detail types.
    /// </summary>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineDefaultImplementationDetailTypes(InteropDefinitions interopDefinitions, ModuleDefinition module)
    {
        try
        {
            // These types are emitted before all other types, so that members in them can more easily
            // be referenced (and imported) without causing issues. The reason is that adding these
            // earlier on ensures that they're already part of the output module.
            module.TopLevelTypes.Add(interopDefinitions.RvaFields);
            module.TopLevelTypes.Add(interopDefinitions.InterfaceIIDs);
            module.TopLevelTypes.Add(interopDefinitions.IUnknownVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IInspectableVftbl);
            module.TopLevelTypes.Add(interopDefinitions.DelegateVftbl);
            module.TopLevelTypes.Add(interopDefinitions.DelegateReferenceVftbl);
            module.TopLevelTypes.Add(interopDefinitions.DelegateInterfaceEntries);
            module.TopLevelTypes.Add(interopDefinitions.IEnumerator1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IEnumerable1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IReadOnlyList1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IList1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IReadOnlyDictionary2Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IDictionary2Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IKeyValuePairVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IKeyValuePairInterfaceEntries);
            module.TopLevelTypes.Add(interopDefinitions.IObservableVectorVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IObservableMapVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IAsyncActionWithProgressVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IAsyncOperationVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IAsyncOperationWithProgressVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IMapChangedEventArgsVftbl);
            module.TopLevelTypes.Add(interopDefinitions.IReferenceArrayVftbl);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DefaultImplementationDetailTypeCodeGenerationError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Defines the dynamic implementation detail types.
    /// </summary>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineDynamicImplementationDetailTypes(InteropDefinitions interopDefinitions, ModuleDefinition module)
    {
        try
        {
            // Emit all shared COM interface entries types that are programmatically generated for user-defined types
            foreach (TypeDefinition typeDefinition in interopDefinitions.EnumerateUserDefinedInterfaceEntriesTypes().OrderByFullyQualifiedTypeName())
            {
                module.TopLevelTypes.Add(typeDefinition);
            }

            // Also emit interface entries types for SZ arrays, same as for user-defined types above
            foreach (TypeDefinition typeDefinition in interopDefinitions.EnumerateSzArrayInterfaceEntriesTypes().OrderByFullyQualifiedTypeName())
            {
                module.TopLevelTypes.Add(typeDefinition);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DynamicImplementationDetailTypeCodeGenerationError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Defines the dynamic type map entries for custom-mapped types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineDynamicCustomMappedTypeMapEntries(
        InteropGeneratorArgs args,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        try
        {
            DynamicCustomMappedTypeMapEntriesBuilder.AssemblyAttributes(
                args: args,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DynamicDynamicCustomMappedTypeMapEntriesCodeGenerationError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Defines the <c>[IgnoresAccessChecksTo]</c> attribute, and applies it to the assembly for each input reference.
    /// </summary>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIgnoresAccessChecksToAttributes(
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        try
        {
            // Emit the '[IgnoresAccessChecksTo]' type first
            module.TopLevelTypes.Add(interopDefinitions.IgnoresAccessChecksToAttribute);

            // Next, emit all the '[IgnoresAccessChecksTo]' attributes for each type
            IgnoresAccessChecksToBuilder.AssemblyAttributes(
                referencePathModules: discoveryState.ModuleDefinitions.Values.OrderByFullyQualifiedName(),
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DefineIgnoresAccessChecksToAttributesError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Emits assembly attributes for the interop assembly being generated.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void EmitMetadataAssemblyAttributes(InteropReferences interopReferences, ModuleDefinition module)
    {
        try
        {
            MetadataAssemblyAttributesBuilder.AssemblyAttributes(interopReferences, module);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.EmitMetadataAssemblyAttributesError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Writes the interop module to disk.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="module">The module to write to disk.</param>
    private static void WriteInteropModuleToDisk(InteropGeneratorArgs args, ModuleDefinition module)
    {
        string winRTInteropAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName);

        try
        {
            module.Write(winRTInteropAssemblyPath);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.EmitDllError(e).ThrowOrAttach(e);
        }
    }
}