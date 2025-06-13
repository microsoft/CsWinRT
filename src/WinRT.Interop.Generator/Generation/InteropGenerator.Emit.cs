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
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    private static void Emit(InteropGeneratorArgs args, InteropGeneratorDiscoveryState discoveryState)
    {
        args.Token.ThrowIfCancellationRequested();

        // Initialize the emit state, which tracks all state to use during the emit phase specifically.
        // For instance, it enables fast lookups for type definitions referenced in multiple places.
        InteropGeneratorEmitState emitState = new();

        // Define the module to emit
        ModuleDefinition module = DefineInteropModule(args, discoveryState, out ModuleDefinition windowsRuntimeModule);

        args.Token.ThrowIfCancellationRequested();

        // Setup the well known items to use when emitting code
        InteropReferences interopReferences = new(module, windowsRuntimeModule);
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

        // Emit interop types for generic delegates
        DefineGenericDelegateTypes(args, discoveryState, interopDefinitions, interopReferences, module);

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

        // Emit interop types for 'IReadOnlyDictionary<,>' types
        DefineIReadOnlyDictionaryTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'IDictionary<,>' types
        DefineIDictionaryTypes(args, discoveryState, emitState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Emit interop types for 'KeyValuePair<,>' types
        DefineKeyValuePairTypes(args, discoveryState, interopDefinitions, interopReferences, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all top level internal types to the interop module
        DefineImplementationDetailTypes(interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

        // Add all '[IgnoreAccessChecksTo]' attributes
        DefineIgnoreAccessChecksToAttributes(discoveryState, interopDefinitions, module);

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
    /// <returns>The interop module to populate and emit.</returns>
    private static ModuleDefinition DefineInteropModule(InteropGeneratorArgs args, InteropGeneratorDiscoveryState discoveryState, out ModuleDefinition windowsRuntimeModule)
    {
        // Get the loaded module for the application .dll (this should always be available here)
        if (!discoveryState.ModuleDefinitions.TryGetValue(args.OutputAssemblyPath, out ModuleDefinition? assemblyModule))
        {
            throw WellKnownInteropExceptions.AssemblyModuleNotFound();
        }

        // Get the loaded module for the runtime .dll (this should also always be available here)
        if ((windowsRuntimeModule = discoveryState.ModuleDefinitions.FirstOrDefault(static kvp => Path.GetFileName(kvp.Key).Equals("WinRT.Runtime2.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WinRTModuleNotFound();
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
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineGenericDelegateTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.GenericDelegateTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                // Define the 'IID' properties
                InteropTypeDefinitionBuilder.Delegate.IIDs(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod,
                    get_ReferenceIidMethod: out MethodDefinition get_ReferenceIidMethod);

                // Define the 'NativeDelegate' type (with the extension method implementation)
                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    nativeDelegateType: out TypeDefinition nativeDelegateType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.Delegate.ComWrappersCallbackType(
                    delegateType: typeSignature,
                    nativeDelegateType: nativeDelegateType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersCallbackType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateImplType);

                // Define the 'DelegateReferenceImpl' type (with the boxed delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ReferenceImplType(
                    delegateType: typeSignature,
                    marshallerType: marshallerType,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateReferenceImplType);

                // Define the 'DelegateInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.Delegate.InterfaceEntriesImplType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition delegateInterfaceEntriesImplType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.Delegate.ComWrappersMarshallerAttribute(
                    delegateType: typeSignature,
                    delegateInterfaceEntriesImplType: delegateInterfaceEntriesImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersMarshallerType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.Delegate.Proxy(
                    delegateType: typeSignature,
                    delegateComWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.Delegate.TypeMapAttributes(
                    delegateType: typeSignature,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    module: module);

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
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.IEnumerator1.IID(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IEnumerator1.ImplType(
                    enumeratorType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                // Define the 'IIteratorMethods' type (with the public thunks for 'IIterator<T>' native calls)
                InteropTypeDefinitionBuilder.IEnumerator1.IIteratorMethods(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    iteratorMethodsType: out TypeDefinition iteratorMethodsType);

                // Define the 'NativeObject' type (with the RCW implementation)
                InteropTypeDefinitionBuilder.IEnumerator1.NativeObject(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeUnsealedObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersCallbackType(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumeratorComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersMarshallerAttribute(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumeratorComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IEnumerator1.Marshaller(
                    enumeratorType: typeSignature,
                    enumeratorComWrappersCallbackType: enumeratorComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IEnumerator1.InterfaceImpl(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IEnumerator1.Proxy(
                    enumeratorType: typeSignature,
                    enumeratorComWrappersMarshallerAttributeType: enumeratorComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.IEnumerator1.TypeMapAttributes(
                    enumeratorType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IEnumerator1TypeCodeGenerationError(typeSignature, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.IEnumerable{T}"/> types.
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
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.IEnumerable1.IID(
                    enumerableType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'Interface' type (with the IID property)
                InteropTypeDefinitionBuilder.IEnumerable1.Interface(
                    enumerableType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceType: out _);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IEnumerable1.ImplType(
                    enumerableType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                // Define the 'IIterableMethods' type (with the public thunks for 'IIterable<T>' native calls)
                InteropTypeDefinitionBuilder.IEnumerable1.IIterableMethods(
                    enumerableType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    iterableMethodsType: out TypeDefinition iterableMethodsType);

                // Define the 'IEnumerableMethods' type (with the public thunks for 'IEnumerable<T>' calls)
                InteropTypeDefinitionBuilder.IEnumerable1.IEnumerableMethods(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    enumerableMethodsType: out TypeDefinition enumerableMethodsType);

                // Define the 'NativeObject' type (with the RCW implementation)
                InteropTypeDefinitionBuilder.IEnumerable1.NativeObject(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeUnsealedObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersCallbackType(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumerableComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersMarshallerAttribute(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumerableComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IEnumerable1.Marshaller(
                    enumerableType: typeSignature,
                    enumerableComWrappersCallbackType: enumerableComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IEnumerable1.InterfaceImpl(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IEnumerable1.Proxy(
                    enumerableType: typeSignature,
                    enumerableComWrappersMarshallerAttributeType: enumerableComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.IEnumerable1.TypeMapAttributes(
                    enumerableType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IEnumerable1TypeCodeGenerationError(typeSignature, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.IReadOnlyList{T}"/> types.
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
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.IReadOnlyList1.IID(
                    readOnlyListType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'Vftbl' type
                InteropTypeDefinitionBuilder.IReadOnlyList1.Vftbl(
                    readOnlyListType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IReadOnlyList1.ImplType(
                    readOnlyListType: typeSignature,
                    vftblType: vftblType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                // Define the 'IVectorViewMethods' type (with the public thunks for 'IVectorView<T>' native calls)
                InteropTypeDefinitionBuilder.IReadOnlyList1.IVectorViewMethods(
                    readOnlyListType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    module: module,
                    vectorViewMethodsType: out TypeDefinition vectorViewMethodsType);

                // Define the 'IReadOnlyListMethods' type
                InteropTypeDefinitionBuilder.IReadOnlyList1.IReadOnlyListMethods(
                    readOnlyListType: typeSignature,
                    vectorViewMethodsType: vectorViewMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    readOnlyListMethodsType: out TypeDefinition readOnlyListMethodsType);

                // Define the 'NativeObject' type (with the RCW implementation)
                InteropTypeDefinitionBuilder.IReadOnlyList1.NativeObject(
                    readOnlyListType: typeSignature,
                    readOnlyListMethodsType: readOnlyListMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeUnsealedObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersCallbackType(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersMarshallerAttribute(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IReadOnlyList1.Marshaller(
                    readOnlyListType: typeSignature,
                    readOnlyListComWrappersCallbackType: readOnlyListComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IReadOnlyList1.InterfaceImpl(
                    readOnlyListType: typeSignature,
                    readOnlyListMethodsType: readOnlyListMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IReadOnlyList1.Proxy(
                    readOnlyListType: typeSignature,
                    readOnlyListComWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.IReadOnlyList1.TypeMapAttributes(
                    readOnlyListType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IReadOnlyList1TypeCodeGenerationError(typeSignature, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.IList{T}"/> types.
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
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.IList1.IID(
                    listType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'Vftbl' type
                InteropTypeDefinitionBuilder.IList1.Vftbl(
                    listType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IList1.ImplType(
                    listType: typeSignature,
                    vftblType: vftblType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                // Define the 'IVectorMethods' type (with the public thunks for 'IVector<T>' native calls)
                InteropTypeDefinitionBuilder.IList1.IVectorMethods(
                    listType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    module: module,
                    vectorMethodsType: out TypeDefinition vectorMethodsType);

                // Define the 'ListMethods' type
                InteropTypeDefinitionBuilder.IList1.IListMethods(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    listMethodsType: out TypeDefinition listMethodsType);

                // Define the 'NativeObject' type (with the RCW implementation)
                InteropTypeDefinitionBuilder.IList1.NativeObject(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeUnsealedObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.IList1.ComWrappersCallbackType(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition listComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IList1.ComWrappersMarshallerAttribute(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition listComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IList1.Marshaller(
                    listType: typeSignature,
                    listComWrappersCallbackType: listComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IList1.InterfaceImpl(
                    listType: typeSignature,
                    listMethodsType: listMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IList1.Proxy(
                    listType: typeSignature,
                    listComWrappersMarshallerAttributeType: listComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.IList1.TypeMapAttributes(
                    listType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IReadOnlyList1TypeCodeGenerationError(typeSignature, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}"/> types.
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
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.IID(
                    readOnlyDictionaryType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'Vftbl' type
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Vftbl(
                    readOnlyDictionaryType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ImplType(
                    readOnlyDictionaryType: typeSignature,
                    vftblType: vftblType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                // Define the 'IMapViewMethods' type (with the public thunks for 'IMapView<K, V>' native calls)
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.IMapViewMethods(
                    readOnlyDictionaryType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    module: module,
                    mapViewMethodsType: out TypeDefinition mapViewMethodsType);

                // Define the 'ReadOnlyDictionaryMethods' type
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.IReadOnlyDictionaryMethods(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    readOnlyDictionaryMethodsType: out TypeDefinition readOnlyDictionaryMethodsType);

                // Define the 'NativeObject' type (with the RCW implementation)
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.NativeObject(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeUnsealedObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersCallbackType(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyDictionaryComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersMarshallerAttribute(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyDictionaryComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Marshaller(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryComWrappersCallbackType: readOnlyDictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.InterfaceImpl(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryMethodsType: readOnlyDictionaryMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Proxy(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryComWrappersMarshallerAttributeType: readOnlyDictionaryComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.TypeMapAttributes(
                    readOnlyDictionaryType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IReadOnlyDictionary2TypeCodeGenerationError(typeSignature, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> types.
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
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.IDictionary2.IID(
                    dictionaryType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'Vftbl' type
                InteropTypeDefinitionBuilder.IDictionary2.Vftbl(
                    dictionaryType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vftblType: out TypeDefinition vftblType);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IDictionary2.ImplType(
                    dictionaryType: typeSignature,
                    vftblType: vftblType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out _);

                // Define the 'IMapMethods' type (with the public thunks for 'IMap<K, V>' native calls)
                InteropTypeDefinitionBuilder.IDictionary2.IMapMethods(
                    dictionaryType: typeSignature,
                    vftblType: vftblType,
                    interopReferences: interopReferences,
                    module: module,
                    mapMethodsType: out TypeDefinition mapMethodsType);

                // Define the 'DictionaryMethods' type
                InteropTypeDefinitionBuilder.IDictionary2.IDictionaryMethods(
                    dictionaryType: typeSignature,
                    mapMethodsType: mapMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    dictionaryMethodsType: out TypeDefinition dictionaryMethodsType);

                // Define the 'NativeObject' type (with the RCW implementation)
                InteropTypeDefinitionBuilder.IDictionary2.NativeObject(
                    dictionaryType: typeSignature,
                    mapMethodsType: mapMethodsType,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeUnsealedObjectComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersCallbackType(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition dictionaryComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersMarshallerAttribute(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition dictionaryComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IDictionary2.Marshaller(
                    dictionaryType: typeSignature,
                    dictionaryComWrappersCallbackType: dictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IDictionary2.InterfaceImpl(
                    dictionaryType: typeSignature,
                    dictionaryMethodsType: dictionaryMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IDictionary2.Proxy(
                    dictionaryType: typeSignature,
                    dictionaryComWrappersMarshallerAttributeType: dictionaryComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition proxyType);

                // Define the type map attributes
                InteropTypeDefinitionBuilder.IDictionary2.TypeMapAttributes(
                    dictionaryType: typeSignature,
                    proxyType: proxyType,
                    interfaceImplType: interfaceImplType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IDictionary2TypeCodeGenerationError(typeSignature, e);
            }
        }
    }

    /// <summary>
    /// Defines the interop types for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineKeyValuePairTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        ModuleDefinition module)
    {
        foreach (GenericInstanceTypeSignature typeSignature in discoveryState.KeyValuePairTypes)
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                // Define the 'IID' property
                InteropTypeDefinitionBuilder.KeyValuePair.IID(
                    keyValuePairType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    get_IidMethod: out MethodDefinition get_IidMethod);

                // Define the 'KeyValuePairImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition keyValuePairTypeImplType);

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
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.ImplementationDetailTypeCodeGenerationError(e);
        }
    }

    /// <summary>
    /// Defines the <c>[IgnoreAccessChecksTo]</c> attribute, and applies it to the assembly for each input reference.
    /// </summary>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    private static void DefineIgnoreAccessChecksToAttributes(
        InteropGeneratorDiscoveryState discoveryState,
        InteropDefinitions interopDefinitions,
        ModuleDefinition module)
    {
        try
        {
            // Emit the '[IgnoreAccessChecksTo]' type first
            module.TopLevelTypes.Add(interopDefinitions.IgnoreAccessChecksToAttribute);

            // Next, emit all the '[IgnoreAccessChecksTo]' attributes for each type
            IgnoreAccessChecksToBuilder.AssemblyAttributes(discoveryState.ModuleDefinitions.Values, interopDefinitions, module);
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
        string winRTInteropAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.InteropDllName);

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
