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
        if (!discoveryState.ModuleDefinitions.TryGetValue(args.AssemblyPath, out ModuleDefinition? assemblyModule))
        {
            throw WellKnownInteropExceptions.AssemblyModuleNotFound();
        }

        // Get the loaded module for the runtime .dll (this should also always be available here)
        if ((windowsRuntimeModule = discoveryState.ModuleDefinitions.FirstOrDefault(static kvp => Path.GetFileName(kvp.Key).Equals("WinRT.Runtime2.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WinRTModuleNotFound();
        }

        try
        {
            AssemblyDefinition winRTInteropAssembly = new(InteropNames.InteropDllNameUtf8, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0));
            ModuleDefinition winRTInteropModule = new(InteropNames.InteropDllName, assemblyModule.OriginalTargetRuntime.GetDefaultCorLib());

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

                // Define the 'ComWrappersCallback' type (with the 'IWindowsRuntimeObjectComWrappersCallback' implementation)
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
                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IEnumerator1.ImplType(
                    enumeratorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition enumeratorImplType,
                    iidRvaField: out _);

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
                    enumeratorImplType: enumeratorImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumeratorComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersMarshallerAttribute(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    enumeratorImplType: enumeratorImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumeratorComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IEnumerator1.Marshaller(
                    enumeratorType: typeSignature,
                    enumeratorImplType: enumeratorImplType,
                    enumeratorComWrappersCallbackType: enumeratorComWrappersCallbackType,
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
                    interfaceImplType: out _);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IEnumerator1.Proxy(
                    enumeratorType: typeSignature,
                    enumeratorComWrappersMarshallerAttributeType: enumeratorComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out _);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IEnumerator1TypeCodeGenerationError(typeSignature.Name, e);
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
                // Define the 'Interface' type (with the IID property)
                InteropTypeDefinitionBuilder.IEnumerable1.Interface(
                    enumerableType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    interfaceType: out TypeDefinition interfaceType,
                    iidRvaField: out _);

                // Define the 'Impl' type (with the CCW vtable implementation)
                InteropTypeDefinitionBuilder.IEnumerable1.ImplType(
                    enumerableType: typeSignature,
                    interfaceType: interfaceType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition enumerableImplType);

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
                    enumerableImplType: enumerableImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumerableComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersMarshallerAttribute(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    enumerableImplType: enumerableImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition enumerableComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IEnumerable1.Marshaller(
                    enumerableType: typeSignature,
                    enumerableImplType: enumerableImplType,
                    enumerableComWrappersCallbackType: enumerableComWrappersCallbackType,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the 'InterfaceImpl' type (with '[DynamicInterfaceCastableImplementation]')
                InteropTypeDefinitionBuilder.IEnumerable1.InterfaceImpl(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopReferences: interopReferences,
                    module: module,
                    interfaceImplType: out _);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IEnumerable1.Proxy(
                    enumerableType: typeSignature,
                    enumerableComWrappersMarshallerAttributeType: enumerableComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out _);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IEnumerable1TypeCodeGenerationError(typeSignature.Name, e);
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    implType: out TypeDefinition readOnlyListImplType,
                    iidRvaField: out _);

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
                    readOnlyListImplType: readOnlyListImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersMarshallerAttribute(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    readOnlyListImplType: readOnlyListImplType,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.IReadOnlyList1.Marshaller(
                    readOnlyListType: typeSignature,
                    readOnlyListImplType: readOnlyListImplType,
                    readOnlyListComWrappersCallbackType: readOnlyListComWrappersCallbackType,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.IReadOnlyList1.Proxy(
                    readOnlyListType: typeSignature,
                    readOnlyListComWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerType,
                    interopReferences: interopReferences,
                    module: module,
                    out _);
            }
            catch (Exception e) when (!e.IsWellKnown)
            {
                throw WellKnownInteropExceptions.IReadOnlyList1TypeCodeGenerationError(typeSignature.Name, e);
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
            module.TopLevelTypes.Add(interopDefinitions.IEnumerable1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IReadOnlyList1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IList1Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IReadOnlyDictionary2Vftbl);
            module.TopLevelTypes.Add(interopDefinitions.IDictionary2Vftbl);
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
