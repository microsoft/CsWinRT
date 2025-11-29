// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

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
        InteropDefinitions interopDefinitions = new(interopReferences, module);

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

        // Emit interop types for SZ array types
        DefineSzArrayTypes(args, discoveryState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Rewrite the IL methods of marshalling stubs needing two-pass generation
        RewriteMethodDefinitions(args, emitState, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for user-defined array types
        DefineUserDefinedTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all dynamic top level internal types to the interop module
        DefineDynamicImplementationDetailTypes(interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all dynamic type map entries for custom-mapped types
        DefineDynamicCustomMappedTypeMapEntries(args, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all '[IgnoresAccessChecksTo]' attributes
        DefineIgnoresAccessChecksToAttributes(discoveryState, interopDefinitions, module);

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
        if ((windowsRuntimeModule = discoveryState.ModuleDefinitions.FirstOrDefault(static kvp => Path.GetFileName(kvp.Key).Equals("WinRT.Runtime2.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WinRTRuntimeModuleNotFound();
        }

        // Get the loaded module for the Windows SDK projection .dll (same as above)
        if ((windowsFoundationModule = discoveryState.ModuleDefinitions.FirstOrDefault(static kvp => Path.GetFileName(kvp.Key).Equals("Microsoft.Windows.SDK.NET.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WindowsSdkProjectionModuleNotFound();
        }

        // If assembly version validation is required, ensure that the 'cswinrtgen' version matches that of 'WinRT.Runtime.dll'.
        // We only compare major and minor versions, as it's fine to ship small forward compatible fixes in revision updates.
        if (args.ValidateWinRTRuntimeAssemblyVersion)
        {
            Version? winRTRuntimeAssemblyVersion = windowsRuntimeModule.Assembly?.Version;
            Version? cswinrtgenAssemblyVersion = typeof(InteropGenerator).Assembly.GetName().Version;

            if (winRTRuntimeAssemblyVersion?.EqualsInMajorAndMinorOnly(cswinrtgenAssemblyVersion) is not true)
            {
                throw WellKnownInteropExceptions.WinRTRuntimeAssemblyVersionMismatch(winRTRuntimeAssemblyVersion, cswinrtgenAssemblyVersion);
            }
        }

        try
        {
            AssemblyDefinition winRTInteropAssembly = new(InteropNames.InteropAssemblyNameUtf8, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0));
            ModuleDefinition winRTInteropModule = new(InteropNames.InteropDllNameUtf8, assemblyModule.OriginalTargetRuntime.GetDefaultCorLib());

            winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(assemblyModule.Assembly?.Name, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
            winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(windowsRuntimeModule.Assembly?.Name, windowsRuntimeModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
            winRTInteropModule.MetadataResolver = new DefaultMetadataResolver(discoveryState.AssemblyResolver);

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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.GenericDelegateTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.Delegate.IIDs(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod,
                    get_ReferenceIidMethod: out MethodDefinition get_ReferenceIidMethod);

                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
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
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
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
                    delegateComWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.Delegate.TypeMapAttributes(
                    delegateType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
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
                    // We need the marshaller type for the 'IObservableVector<T>' implementation
                    emitState.TrackTypeDefinition(marshallerType, typeSignature, "Marshaller");

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
                    // We need the marshaller type for the 'IObservableMap<K, V>' implementation
                    emitState.TrackTypeDefinition(marshallerType, typeSignature, "Marshaller");

                    InteropTypeDefinitionBuilder.EventSource.MapChangedEventHandler2(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopReferences: interopReferences,
                        emitState: emitState,
                        module: module,
                        eventSourceType: out _);
                }
                else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.AsyncActionProgressHandler1) ||
                         SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.AsyncActionWithProgressCompletedHandler1) ||
                         SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.AsyncOperationCompletedHandler1) ||
                         SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.AsyncOperationProgressHandler2) ||
                         SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.AsyncOperationWithProgressCompletedHandler2))
                {
                    // We need these marshaller types for the various async type implementations
                    emitState.TrackTypeDefinition(marshallerType, typeSignature, "Marshaller");
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IEnumerator1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

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

                InteropTypeDefinitionBuilder.IEnumerator1.NativeObject(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersCallbackType(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumeratorComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersMarshallerAttribute(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumeratorComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IEnumerator1.Marshaller(
                    enumeratorType: typeSignature,
                    enumeratorComWrappersCallbackType: enumeratorComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IEnumerator1.InterfaceImpl(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IEnumerator1.Proxy(
                    enumeratorType: typeSignature,
                    enumeratorComWrappersMarshallerAttributeType: enumeratorComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IEnumerator1.TypeMapAttributes(
                    enumeratorType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IEnumerable1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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

                InteropTypeDefinitionBuilder.IEnumerable1.IEnumerableMethods(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    enumerableMethodsType: out TypeDefinition enumerableMethodsType);

                InteropTypeDefinitionBuilder.IEnumerable1.NativeObject(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersCallbackType(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumerableComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersMarshallerAttribute(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumerableComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IEnumerable1.Marshaller(
                    enumerableType: typeSignature,
                    enumerableComWrappersCallbackType: enumerableComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IEnumerable1.InterfaceImpl(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IEnumerable1.Proxy(
                    enumerableType: typeSignature,
                    enumerableComWrappersMarshallerAttributeType: enumerableComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IEnumerable1.TypeMapAttributes(
                    enumerableType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IReadOnlyList1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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

                InteropTypeDefinitionBuilder.IReadOnlyList1.IReadOnlyListMethods(
                    readOnlyListType: typeSignature,
                    vectorViewMethodsType: vectorViewMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    readOnlyListMethodsType: out TypeDefinition readOnlyListMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.NativeObject(
                    readOnlyListType: typeSignature,
                    readOnlyListMethodsType: readOnlyListMethodsType,
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
                    out TypeDefinition readOnlyListComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersMarshallerAttribute(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.Marshaller(
                    readOnlyListType: typeSignature,
                    readOnlyListComWrappersCallbackType: readOnlyListComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.InterfaceImpl(
                    readOnlyListType: typeSignature,
                    readOnlyListMethodsType: readOnlyListMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.Proxy(
                    readOnlyListType: typeSignature,
                    readOnlyListComWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.TypeMapAttributes(
                    readOnlyListType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IList1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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

                InteropTypeDefinitionBuilder.IList1.IListMethods(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopReferences: interopReferences,
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
                    out TypeDefinition listComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IList1.ComWrappersMarshallerAttribute(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition listComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IList1.Marshaller(
                    listType: typeSignature,
                    listComWrappersCallbackType: listComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IList1.InterfaceImpl(
                    listType: typeSignature,
                    listMethodsType: listMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IList1.Proxy(
                    listType: typeSignature,
                    listComWrappersMarshallerAttributeType: listComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IList1.TypeMapAttributes(
                    listType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IReadOnlyList1TypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IReadOnlyDictionary2Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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
                    module: module,
                    mapViewMethodsType: out TypeDefinition mapViewMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.IReadOnlyDictionaryMethods(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopReferences: interopReferences,
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
                    out TypeDefinition readOnlyDictionaryComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersMarshallerAttribute(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyDictionaryComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Marshaller(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryComWrappersCallbackType: readOnlyDictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.InterfaceImpl(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryMethodsType: readOnlyDictionaryMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Proxy(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryComWrappersMarshallerAttributeType: readOnlyDictionaryComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.TypeMapAttributes(
                    readOnlyDictionaryType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IDictionary2Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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

                InteropTypeDefinitionBuilder.IDictionary2.IDictionaryMethods(
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
                    out TypeDefinition dictionaryComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersMarshallerAttribute(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition dictionaryComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IDictionary2.Marshaller(
                    dictionaryType: typeSignature,
                    dictionaryComWrappersCallbackType: dictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IDictionary2.InterfaceImpl(
                    dictionaryType: typeSignature,
                    dictionaryMethodsType: dictionaryMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IDictionary2.Proxy(
                    dictionaryType: typeSignature,
                    dictionaryComWrappersMarshallerAttributeType: dictionaryComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IDictionary2.TypeMapAttributes(
                    dictionaryType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.KeyValuePairTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition keyValuePairTypeImplType);

                InteropTypeDefinitionBuilder.KeyValuePair.InterfaceEntriesImplType(
                    keyValuePairType: typeSignature,
                    keyValuePairTypeImplType: keyValuePairTypeImplType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.KeyValuePair.Accessors(
                    keyValuePairType: typeSignature,
                    methodsType: methodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    keyAccessorMethod: out MethodDefinition keyAccessorMethod,
                    valueAccessorMethod: out MethodDefinition valueAccessorMethod);

                InteropTypeDefinitionBuilder.KeyValuePair.Marshaller(
                    keyValuePairType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IMapChangedEventArgs1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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
                    out TypeDefinition argsComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.ComWrappersMarshallerAttribute(
                    argsType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition argsComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.Marshaller(
                    argsType: typeSignature,
                    argsComWrappersCallbackType: argsComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.InterfaceImpl(
                    argsType: typeSignature,
                    argsMethodsType: argsMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.Proxy(
                    argsType: typeSignature,
                    argsComWrappersMarshallerAttributeType: argsComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.TypeMapAttributes(
                    argsType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IObservableVector1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    factoryType: out TypeDefinition factoryType);

                InteropTypeDefinitionBuilder.IObservableVector1.Methods(
                    vectorType: typeSignature,
                    eventSourceFactoryType: factoryType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    methodsType: out TypeDefinition methodsType);

                InteropTypeDefinitionBuilder.IObservableVector1.NativeObject(
                    vectorType: typeSignature,
                    vectorMethodsType: methodsType,
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
                    out TypeDefinition comWrappersCallbackType);

                InteropTypeDefinitionBuilder.IObservableVector1.ComWrappersMarshallerAttribute(
                    vectorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IObservableVector1.Marshaller(
                    vectorType: typeSignature,
                    vectorComWrappersCallbackType: comWrappersMarshallerType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IObservableVector1.InterfaceImpl(
                    vectorType: typeSignature,
                    vectorMethodsType: methodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IObservableVector1.Proxy(
                    vectorType: typeSignature,
                    vectorComWrappersMarshallerAttributeType: comWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IObservableVector1.TypeMapAttributes(
                    vectorType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IObservableMap2Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.IObservableMap2.ImplType(
                    mapType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    implType: out _);

                InteropTypeDefinitionBuilder.IObservableMap2.EventSourceFactory(
                    mapType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    factoryType: out TypeDefinition factoryType);

                InteropTypeDefinitionBuilder.IObservableMap2.Methods(
                    mapType: typeSignature,
                    eventSourceFactoryType: factoryType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
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
                    out TypeDefinition comWrappersCallbackType);

                InteropTypeDefinitionBuilder.IObservableMap2.ComWrappersMarshallerAttribute(
                    mapType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IObservableMap2.Marshaller(
                    mapType: typeSignature,
                    mapComWrappersCallbackType: comWrappersMarshallerType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IObservableMap2.InterfaceImpl(
                    mapType: typeSignature,
                    mapMethodsType: methodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IObservableMap2.Proxy(
                    mapType: typeSignature,
                    mapComWrappersMarshallerAttributeType: comWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IObservableMap2.TypeMapAttributes(
                    mapType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IAsyncActionWithProgress1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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
                    out TypeDefinition actionComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.ComWrappersMarshallerAttribute(
                    actionType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition actionComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.Marshaller(
                    actionType: typeSignature,
                    operationComWrappersCallbackType: actionComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.InterfaceImpl(
                    actionType: typeSignature,
                    actionMethodsType: actionMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.Proxy(
                    actionType: typeSignature,
                    actionComWrappersMarshallerAttributeType: actionComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.TypeMapAttributes(
                    actionType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IAsyncOperation1Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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
                    out TypeDefinition operationComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.ComWrappersMarshallerAttribute(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition operationComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.Marshaller(
                    operationType: typeSignature,
                    operationComWrappersCallbackType: operationComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.InterfaceImpl(
                    operationType: typeSignature,
                    operationMethodsType: operationMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.Proxy(
                    operationType: typeSignature,
                    operationComWrappersMarshallerAttributeType: operationComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.TypeMapAttributes(
                    operationType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
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
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.IAsyncOperationWithProgress2Types)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
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
                    out TypeDefinition operationComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.ComWrappersMarshallerAttribute(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition operationComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.Marshaller(
                    operationType: typeSignature,
                    operationComWrappersCallbackType: operationComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.InterfaceImpl(
                    operationType: typeSignature,
                    operationMethodsType: operationMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.Proxy(
                    operationType: typeSignature,
                    operationComWrappersMarshallerAttributeType: operationComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.TypeMapAttributes(
                    operationType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.IAsyncOperationWithProgressTypeCodeGenerationError(typeSignature, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for SZ array types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineSzArrayTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (SzArrayTypeSignature typeSignature in discoveryState.SzArrayTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.IID(
                    interfaceType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                InteropTypeDefinitionBuilder.SzArray.Marshaller(
                    arrayType: typeSignature,
                    interopReferences: interopReferences,
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
                    implType: arrayImplType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceEntriesImplType: out TypeDefinition arrayInterfaceEntriesImplType);

                InteropTypeDefinitionBuilder.SzArray.ComWrappersMarshallerAttribute(
                    arrayType: typeSignature,
                    arrayInterfaceEntriesImplType: arrayInterfaceEntriesImplType,
                    arrayComWrappersCallbackType: arrayComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition arrayComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.SzArray.Proxy(
                    arrayType: typeSignature,
                    arrayComWrappersMarshallerAttributeType: arrayComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.SzArray.TypeMapAttributes(
                    arrayType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.SzArrayTypeCodeGenerationError(typeSignature.Name, e).ThrowOrAttach(e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for SZ array types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void RewriteMethodDefinitions(
        InteropGeneratorArgs args,
        InteropGeneratorEmitState emitState,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (ReturnTypeMethodRewriteInfo rewriteInfo in emitState.EnumerateMethodRewriteInfos())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropMethodRewriteFactory.ReturnValue.RewriteMethod(
                    returnType: rewriteInfo.ReturnType,
                    method: rewriteInfo.Method,
                    marker: rewriteInfo.Marker,
                    source: rewriteInfo.Source,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.MethodRewriteError(rewriteInfo.ReturnType, rewriteInfo.Method, e).ThrowOrAttach(e);
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
        foreach (TypeSignatureEquatableSet vtableTypes in discoveryState.UserDefinedVtableTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            TypeSignature? typeSignature = null;

            try
            {
                // Get the first user-defined with this vtable set as reference
                typeSignature = discoveryState.UserDefinedAndVtableTypes.First(kvp => kvp.Value.Equals(vtableTypes)).Key;

                InteropTypeDefinitionBuilder.UserDefinedType.InterfaceEntriesImpl(
                    userDefinedType: typeSignature,
                    vtableTypes: vtableTypes,
                    args: args,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceEntriesImplType: out TypeDefinition interfaceEntriesImplType);

                InteropTypeDefinitionBuilder.UserDefinedType.ComWrappersMarshallerAttribute(
                    userDefinedType: typeSignature,
                    vtableTypes: vtableTypes,
                    interfaceEntriesImplType: interfaceEntriesImplType,
                    interopDefinitions: interopDefinitions,
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
        foreach ((TypeSignature typeSignature, TypeSignatureEquatableSet vtableTypes) in discoveryState.UserDefinedAndVtableTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.UserDefinedType.Proxy(
                    userDefinedType: typeSignature,
                    comWrappersMarshallerAttributeType: marshallerAttributeMap[vtableTypes],
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

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
            module.TopLevelTypes.Add(interopDefinitions.IReferenceArrayInterfaceEntries);
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
            // Also emit all shared COM interface entries types that are programmatically generated
            foreach (TypeDefinition typeDefinition in interopDefinitions.EnumerateUserDefinedInterfaceEntriesTypes())
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
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineDynamicCustomMappedTypeMapEntries(
        InteropGeneratorArgs args,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        try
        {
            DynamicCustomMappedTypeMapEntriesBuilder.AssemblyAttributes(
                args: args,
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
    /// <param name="module">The interop module being built.</param>
    private static void DefineIgnoresAccessChecksToAttributes(
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        ModuleDefinition module)
    {
        try
        {
            // Emit the '[IgnoresAccessChecksTo]' type first
            module.TopLevelTypes.Add(interopDefinitions.IgnoresAccessChecksToAttribute);

            // Next, emit all the '[IgnoresAccessChecksTo]' attributes for each type
            IgnoresAccessChecksToBuilder.AssemblyAttributes(discoveryState.ModuleDefinitions.Values, interopDefinitions, module);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DefineIgnoresAccessChecksToAttributesError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Writes the interop module to disk.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="module">The module to write to disk.</param>
    private static void WriteInteropModuleToDisk(InteropGeneratorArgs args, ModuleDefinition module)
    {
        string winRTInteropAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.InteropDllName);

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