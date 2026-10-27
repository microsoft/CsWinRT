// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.Helpers;
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Fixups;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Rewriters;
using WindowsRuntime.InteropGenerator.Writers;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Normalizes the discovered references and initializes the output module before fingerprinting and emission.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <returns>The output module and the Windows Runtime implementation module.</returns>
    private static (ModuleDefinition Module, ModuleDefinition WindowsRuntimeModule) PrepareEmission(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState)
    {
        NormalizeAssemblyReferences(discoveryState);

        ModuleDefinition module = DefineInteropModule(args, discoveryState, out ModuleDefinition runtime, out _);

        return (module, runtime);
    }

    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="module">The initialized output module.</param>
    /// <param name="windowsRuntimeModule">The Windows Runtime implementation module.</param>
    /// <param name="fingerprint">The emission fingerprint to cache with the output, or an empty span to disable caching.</param>
    private static void Emit(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        ModuleDefinition module,
        ModuleDefinition windowsRuntimeModule,
        ReadOnlySpan<byte> fingerprint)
    {
        args.Token.ThrowIfCancellationRequested();

        // Initialize the emit state, which tracks all state to use during the emit phase specifically.
        // For instance, it enables fast lookups for type definitions referenced in multiple places.
        InteropGeneratorEmitState emitState = new(discoveryState.SignatureComparer);

        // Setup the well known items to use when emitting code
        InteropReferences interopReferences = new(
            runtimeContext: module.RuntimeContext!,
            corLibTypeFactory: module.CorLibTypeFactory,
            windowsRuntimeModule: windowsRuntimeModule,
            windowsRuntimeComponentModule: discoveryState.WindowsRuntimeComponentModule,
            signatureComparer: discoveryState.SignatureComparer);
        InteropDefinitions interopDefinitions = new(
            interopReferences: interopReferences,
            windowsRuntimeSdkProjectionModule: discoveryState.WindowsRuntimeSdkProjectionModule!,
            windowsRuntimeSdkXamlProjectionModule: discoveryState.WindowsRuntimeSdkXamlProjectionModule,
            windowsRuntimeProjectionModule: discoveryState.WindowsRuntimeProjectionModule,
            windowsRuntimeComponentModule: discoveryState.WindowsRuntimeComponentModule,
            enableDefaultCustomPropertyProviderSupport: args.EnableDefaultCustomPropertyProviderSupport);

        // Prepare the canonicalized discovery inputs for emission
        InteropGeneratorEmitInputs inputs = new(discoveryState, interopReferences, module, args.Token);

        args.Token.ThrowIfCancellationRequested();

        // Add all default top level internal types to the interop module
        DefineDefaultImplementationDetailTypes(interopDefinitions, module);

        args.Token.ThrowIfCancellationRequested();

        InteropGeneratorEmissionContext context = new(args, discoveryState, inputs, emitState, interopDefinitions, interopReferences, module);

        // Emit interop code as dependencies complete, then join all workers before applying IL fixups
        RunEmission(context);

        args.Token.ThrowIfCancellationRequested();

        // Share serialized assembly attribute payloads between ordering and writing to avoid serializing them twice
        CustomAttributePayloadCache payloads = new(module);

        // Sort independently emitted metadata before token assignment so worker completion order cannot affect the binary
        OrderGeneratedMetadata(args, module, interopDefinitions, payloads);

        args.Token.ThrowIfCancellationRequested();

        // Write the finalized module, publishing the incremental cache when a fingerprint is available
        WriteInteropModuleToDisk(args, module, fingerprint, payloads);

        args.Token.ThrowIfCancellationRequested();

        // Write the optional diagnostic report with discovery results and final output statistics
        WriteLog(args, discoveryState, module);
    }

    /// <summary>
    /// Normalizes equivalent assembly references in the loaded modules before emission.
    /// </summary>
    /// <param name="discoveryState">The completed discovery state.</param>
    /// <remarks>
    /// Only the in-memory metadata is updated, after parallel discovery (and its validation) has completed.
    /// This runs before computing the incremental generation fingerprint, so both see the same references.
    /// </remarks>
    private static void NormalizeAssemblyReferences(InteropGeneratorDiscoveryState discoveryState)
    {
        ModuleDefinition[] modules = [.. discoveryState.Modules.Values.Concat([
            discoveryState.WindowsRuntimeSdkProjectionModule,
            discoveryState.WindowsRuntimeSdkXamlProjectionModule,
            discoveryState.WindowsRuntimeProjectionModule,
            discoveryState.WindowsRuntimeComponentModule]).OfType<ModuleDefinition>()];

        Dictionary<AssemblyDescriptor, AssemblyDefinition> assemblies = new(discoveryState.SignatureComparer);

        // Index the loaded assembly definitions, preferring the highest version for equivalent identities. Implicit
        // imports copy processor-architecture flags from definitions (e.g. '0x70' in 'System.Runtime'), while ordinary
        // references omit them. These flags are not part of .NET assembly binding, so we can just remove them.
        foreach (ModuleDefinition module in modules.OrderByDescending(static module => module.Assembly?.Version))
        {
            if (module.Assembly is { } assembly)
            {
                assembly.Attributes &= ~AssemblyAttributes.FullMask;

                _ = assemblies.TryAdd(assembly, assembly);
            }
        }

        // Normalize all assembly references declared by each loaded module
        foreach (ModuleDefinition module in modules)
        {
            foreach (AssemblyReference reference in module.AssemblyReferences)
            {
                Normalize(reference);
            }

            if (module.CorLibTypeFactory.CorLibScope is AssemblyReference corLib)
            {
                Normalize(corLib);
            }
        }

        HashSet<TypeSignature> visited = new(ReferenceEqualityComparer.Instance);
        IEnumerable<TypeSignature> types = discoveryState.GenericDelegateTypes
            .Concat(discoveryState.IEnumerator1Types)
            .Concat(discoveryState.IEnumerable1Types)
            .Concat(discoveryState.IReadOnlyList1Types)
            .Concat(discoveryState.IList1Types)
            .Concat(discoveryState.IReadOnlyDictionary2Types)
            .Concat(discoveryState.IDictionary2Types)
            .Concat(discoveryState.KeyValuePairTypes)
            .Concat(discoveryState.IMapChangedEventArgs1Types)
            .Concat(discoveryState.IObservableVector1Types)
            .Concat(discoveryState.IObservableMap2Types)
            .Concat(discoveryState.IAsyncActionWithProgress1Types)
            .Concat(discoveryState.IAsyncOperation1Types)
            .Concat(discoveryState.IAsyncOperationWithProgress2Types)
            .Cast<TypeSignature>()
            .Concat(discoveryState.UserDefinedTypes)
            .Concat(discoveryState.SzArrayAndVtableTypes.Keys)
            .Concat(discoveryState.UserDefinedVtableTypes.SelectMany(static types => types))
            .Concat(discoveryState.SzArrayAndVtableTypes.Values.SelectMany(static types => types));

        // Also normalize the scopes of all discovered signatures, which can be created outside of module references
        foreach (TypeSignature type in types)
        {
            NormalizeType(type);
        }

        // Normalizes the scopes of a signature and of all its nested signatures
        void NormalizeType(TypeSignature type)
        {
            if (!visited.Add(type))
            {
                return;
            }

            if (type.Scope?.GetAssembly() is AssemblyReference reference)
            {
                Normalize(reference);
            }

            if (type is GenericInstanceTypeSignature generic)
            {
                foreach (TypeSignature argument in generic.TypeArguments)
                {
                    NormalizeType(argument);
                }
            }
            else if (type is TypeSpecificationSignature specification)
            {
                NormalizeType(specification.BaseType);
            }
        }

        // Normalizes the version, flags, and public key of an assembly reference
        void Normalize(AssemblyReference reference)
        {
            // Never retarget a CsWinRT 2.x reference to the loaded runtime (only reachable if validation is disabled)
            if (reference.Name?.AsSpan().SequenceEqual("WinRT.Runtime"u8) is true && reference.Version.Major < 3)
            {
                return;
            }

            // IgnoreVersion deduplication must not let a parallel worker randomly choose an
            // older BCL reference (e.g. System.Runtime 6 versus 10) for the emitted signature.
            if (assemblies.TryGetValue(reference, out AssemblyDefinition? definition))
            {
                reference.Version = definition.Version;
            }

            reference.Attributes &= ~AssemblyAttributes.FullMask;

            // Equivalent references can use either the full public key or its token
            if (reference.HasPublicKey)
            {
                reference.PublicKeyOrToken = reference.GetPublicKeyToken();
                reference.HasPublicKey = false;
            }
        }
    }

    /// <summary>
    /// Defines the interop module to emit.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="discoveryState"><inheritdoc cref="Emit" path="/param[@name='state']/node()"/></param>
    /// <param name="windowsRuntimeModule">The <see cref="ModuleDefinition"/> for the Windows Runtime assembly.</param>
    /// <param name="windowsRuntimeSdkProjectionModule">The <see cref="ModuleDefinition"/> for the Windows SDK projection assembly.</param>
    /// <returns>The interop module to populate and emit.</returns>
    private static ModuleDefinition DefineInteropModule(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        out ModuleDefinition windowsRuntimeModule,
        out ModuleDefinition windowsRuntimeSdkProjectionModule)
    {
        // Get the loaded module for the application .dll (this should always be available here)
        if (!discoveryState.Modules.TryGetValue(args.OutputAssemblyPath, out ModuleDefinition? assemblyModule))
        {
            throw WellKnownInteropExceptions.AssemblyModuleNotFound();
        }

        // Get the loaded module for the runtime .dll (this should also always be available here)
        if ((windowsRuntimeModule = discoveryState.Modules.FirstOrDefault(
            predicate: static kvp => Path.GetFileName(Path.Normalize(kvp.Key)).Equals("WinRT.Runtime.dll")).Value) is null)
        {
            throw WellKnownInteropExceptions.WinRTRuntimeModuleNotFound();
        }

        // Get the loaded module for the Windows SDK projection .dll (same as above)
        if ((windowsRuntimeSdkProjectionModule = discoveryState.Modules.FirstOrDefault(
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
            ModuleDefinition winRTInteropModule = new InteropModuleDefinition(discoveryState.RuntimeContext.TargetRuntime)
            {
                // We need a deterministic MVID for the generated module, so we create one based on the input assemblies.
                // This logic will produce a hash from each .NET assembly that was loaded and analyzed during discovery.
                Mvid = MvidGenerator.CreateMvid(discoveryState.Modules.Keys)
            };

            // Also create a containing assembly for it (needed for the emit phase). We don't actually need the assembly
            // ourselves, but creating it and adding the module will update the declaring assembly for types added to it.
            AssemblyDefinition winRTInteropAssembly = new(
                name: InteropNames.WindowsRuntimeInteropAssemblyNameUtf8,
                version: assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0))
            {
                Modules = { winRTInteropModule }
            };

            // Add this assembly to the runtime context we've been using since discovery.
            // This enables type resolution across assemblies (used for auto-import).
            discoveryState.RuntimeContext.AddAssembly(winRTInteropAssembly);

            return winRTInteropModule;
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DefineInteropAssemblyError(e);
        }
    }

    /// <summary>
    /// Defines the Windows Runtime type hierarchy lookup.
    /// </summary>
    /// <param name="context">The shared emission context.</param>
    private static void DefineTypeHierarchyLookup(InteropGeneratorEmissionContext context)
    {
        WindowsRuntimeTypeHierarchyBuilder.Lookup(
            context.DiscoveryState.TypeHierarchyEntries,
            context.Definitions,
            context.References,
            context.Module,
            context.Args.Token,
            out _);
    }

    /// <summary>
    /// Defines the interop types for generic delegates.
    /// </summary>
    /// <param name="context">The shared emission context.</param>
    private static void DefineGenericDelegateTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateGenericDelegateTypes())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition delegateComWrappersCallbackType);

                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    get_ReferenceIidMethod: get_ReferenceIidMethod,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
                if (interopReferences.SignatureComparer.Equals(typeSignature.GenericType, interopReferences.EventHandler1))
                {
                    InteropTypeDefinitionBuilder.EventSource.EventHandler1(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopDefinitions: interopDefinitions,
                        interopReferences: interopReferences,
                        module: module,
                        eventSourceType: out _);
                }
                else if (interopReferences.SignatureComparer.Equals(typeSignature.GenericType, interopReferences.EventHandler2))
                {
                    InteropTypeDefinitionBuilder.EventSource.EventHandler2(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopDefinitions: interopDefinitions,
                        interopReferences: interopReferences,
                        module: module,
                        eventSourceType: out _);
                }
                else if (interopReferences.SignatureComparer.Equals(typeSignature.GenericType, interopReferences.VectorChangedEventHandler1))
                {
                    InteropTypeDefinitionBuilder.EventSource.VectorChangedEventHandler1(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopDefinitions: interopDefinitions,
                        interopReferences: interopReferences,
                        emitState: emitState,
                        module: module,
                        eventSourceType: out _);
                }
                else if (interopReferences.SignatureComparer.Equals(typeSignature.GenericType, interopReferences.MapChangedEventHandler2))
                {
                    InteropTypeDefinitionBuilder.EventSource.MapChangedEventHandler2(
                        delegateType: typeSignature,
                        marshallerType: marshallerType,
                        interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIEnumeratorTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIEnumerator1Types())
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
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    enumeratorMethodsType: out _);

                InteropTypeDefinitionBuilder.IEnumerator1.NativeObject(
                    enumeratorType: typeSignature,
                    iteratorMethodsType: iteratorMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    nativeObjectType: out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersCallbackType(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    callbackType: out TypeDefinition enumeratorComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IEnumerator1.ComWrappersMarshallerAttribute(
                    enumeratorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition enumeratorComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: enumeratorComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIEnumerableTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIEnumerable1Types())
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
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    enumerableMethodsType: out _);

                InteropTypeDefinitionBuilder.IEnumerable1.NativeObject(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    nativeObjectType: out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersCallbackType(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    callbackType: out TypeDefinition enumerableComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IEnumerable1.ComWrappersMarshallerAttribute(
                    enumerableType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    marshallerType: out TypeDefinition enumerableComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: enumerableComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IEnumerable1.EnumerableAttribute(
                    enumerableType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    attributeType: out _);

                InteropTypeDefinitionBuilder.IEnumerable1.InterfaceImpl(
                    enumerableType: typeSignature,
                    iterableMethodsType: iterableMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: enumerableComWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIReadOnlyListTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIReadOnlyList1Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vectorViewMethodsType: out TypeDefinition vectorViewMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.Methods(
                    readOnlyListType: typeSignature,
                    vectorViewMethodsType: vectorViewMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    readOnlyListMethodsType: out TypeDefinition readOnlyListMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.NativeObject(
                    readOnlyListType: typeSignature,
                    vectorViewMethodsType: vectorViewMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersCallbackType(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition readOnlyListComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.ComWrappersMarshallerAttribute(
                    readOnlyListType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyListComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: readOnlyListComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyList1.InterfaceImpl(
                    readOnlyListType: typeSignature,
                    readOnlyListMethodsType: readOnlyListMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: readOnlyListComWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIListTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIList1Types())
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
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    vectorMethodsType: out TypeDefinition vectorMethodsType);

                InteropTypeDefinitionBuilder.IList1.Methods(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    listMethodsType: out TypeDefinition listMethodsType);

                InteropTypeDefinitionBuilder.IList1.NativeObject(
                    listType: typeSignature,
                    vectorMethodsType: vectorMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IList1.ComWrappersCallbackType(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition listComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IList1.ComWrappersMarshallerAttribute(
                    listType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition listComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: listComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IList1.InterfaceImpl(
                    listType: typeSignature,
                    listMethodsType: listMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: listComWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIReadOnlyDictionaryTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIReadOnlyDictionary2Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    mapViewMethodsType: out TypeDefinition mapViewMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.Methods(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    readOnlyDictionaryMethodsType: out TypeDefinition readOnlyDictionaryMethodsType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.NativeObject(
                    readOnlyDictionaryType: typeSignature,
                    mapViewMethodsType: mapViewMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersCallbackType(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition readOnlyDictionaryComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.ComWrappersMarshallerAttribute(
                    readOnlyDictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition readOnlyDictionaryComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: readOnlyDictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IReadOnlyDictionary2.InterfaceImpl(
                    readOnlyDictionaryType: typeSignature,
                    readOnlyDictionaryMethodsType: readOnlyDictionaryMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: readOnlyDictionaryComWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIDictionaryTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIDictionary2Types())
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
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    mapMethodsType: out TypeDefinition mapMethodsType);

                InteropTypeDefinitionBuilder.IDictionary2.Methods(
                    dictionaryType: typeSignature,
                    mapMethodsType: mapMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    dictionaryMethodsType: out TypeDefinition dictionaryMethodsType);

                InteropTypeDefinitionBuilder.IDictionary2.NativeObject(
                    dictionaryType: typeSignature,
                    mapMethodsType: mapMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersCallbackType(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition dictionaryComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IDictionary2.ComWrappersMarshallerAttribute(
                    dictionaryType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition dictionaryComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: dictionaryComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IDictionary2.InterfaceImpl(
                    dictionaryType: typeSignature,
                    dictionaryMethodsType: dictionaryMethodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: dictionaryComWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineKeyValuePairTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        // Generate shared code for helpers needed by instantiated 'KeyValuePair<,>' types
        try
        {
            module.TopLevelTypes.Add(interopDefinitions.KeyValuePairMethods);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.KeyValuePairTypeSharedCodeGenerationError(e).ThrowOrAttach(e);
        }

        // Generate specialized code for all discovered instantiations
        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateKeyValuePairTypes())
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
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIMapChangedEventArgsTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIMapChangedEventArgs1Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.ComWrappersCallbackType(
                    argsType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition argsComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IMapChangedEventArgs1.ComWrappersMarshallerAttribute(
                    argsType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition argsComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: argsComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIObservableVectorTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIObservableVector1Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    factoryType: out TypeDefinition factoryType);

                InteropTypeDefinitionBuilder.IObservableVector1.EventSourceCallback(
                    vectorType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    callbackType: out TypeDefinition callbackType);

                InteropTypeDefinitionBuilder.IObservableVector1.Methods(
                    vectorType: typeSignature,
                    eventSourceCallbackType: callbackType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    methodsType: out TypeDefinition methodsType);

                InteropTypeDefinitionBuilder.IObservableVector1.NativeObject(
                    vectorType: typeSignature,
                    factoryType: factoryType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IObservableVector1.ComWrappersCallbackType(
                    vectorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition comWrappersCallbackType);

                InteropTypeDefinitionBuilder.IObservableVector1.ComWrappersMarshallerAttribute(
                    vectorType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: comWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IObservableVector1.InterfaceImpl(
                    vectorType: typeSignature,
                    vectorMethodsType: methodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: comWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIObservableMapTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIObservableMap2Types())
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

                InteropTypeDefinitionBuilder.IObservableMap2.EventSourceFactory(
                    mapType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    factoryType: out TypeDefinition factoryType);

                InteropTypeDefinitionBuilder.IObservableMap2.EventSourceCallback(
                    mapType: typeSignature,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    callbackType: out TypeDefinition callbackType);

                InteropTypeDefinitionBuilder.IObservableMap2.Methods(
                    mapType: typeSignature,
                    eventSourceCallbackType: callbackType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    methodsType: out TypeDefinition methodsType);

                InteropTypeDefinitionBuilder.IObservableMap2.NativeObject(
                    mapType: typeSignature,
                    factoryType: factoryType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IObservableMap2.ComWrappersCallbackType(
                    mapType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition comWrappersCallbackType);

                InteropTypeDefinitionBuilder.IObservableMap2.ComWrappersMarshallerAttribute(
                    mapType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: comWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.IObservableMap2.InterfaceImpl(
                    mapType: typeSignature,
                    mapMethodsType: methodsType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    interfaceImplType: out TypeDefinition interfaceImplType);

                InteropTypeDefinitionBuilder.Proxy(
                    interfaceType: typeSignature,
                    comWrappersMarshallerAttributeType: comWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIAsyncActionWithProgressTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIAsyncActionWithProgress1Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.ComWrappersCallbackType(
                    actionType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition actionComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncActionWithProgress1.ComWrappersMarshallerAttribute(
                    actionType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition actionComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: actionComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIAsyncOperationTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIAsyncOperation1Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.ComWrappersCallbackType(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition operationComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncOperation1.ComWrappersMarshallerAttribute(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition operationComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: operationComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIAsyncOperationWithProgressTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIAsyncOperationWithProgress2Types())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition nativeObjectType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.ComWrappersCallbackType(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    out TypeDefinition operationComWrappersCallbackType);

                InteropTypeDefinitionBuilder.IAsyncOperationWithProgress2.ComWrappersMarshallerAttribute(
                    operationType: typeSignature,
                    nativeObjectType: nativeObjectType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition operationComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.Marshaller(
                    typeSignature: typeSignature,
                    interfaceComWrappersCallbackType: operationComWrappersCallbackType,
                    get_IidMethod: get_IidMethod,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineIReadOnlyCollectionKeyValuePair2Types(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateIReadOnlyCollectionKeyValuePair2Types())
        {
            args.Token.ThrowIfCancellationRequested();

            // Construct the 'IReadOnlyCollection<KeyValuePair<,>>' type for processing
            GenericInstanceTypeSignature readOnlyCollectionType = interopReferences.IReadOnlyCollection1.MakeGenericReferenceType([typeSignature.TypeArguments[0]]);

            try
            {
                InteropTypeDefinitionBuilder.IReadOnlyCollectionKeyValuePair2.ForwarderAttribute(
                    readOnlyCollectionType: readOnlyCollectionType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    forwarderAttributeType: out TypeDefinition forwarderAttributeType);

                InteropTypeDefinitionBuilder.IReadOnlyCollectionKeyValuePair2.InterfaceImpl(
                    readOnlyCollectionType: readOnlyCollectionType,
                    forwarderAttributeType: forwarderAttributeType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineICollectionKeyValuePair2Types(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach (GenericInstanceTypeSignature typeSignature in inputs.EnumerateICollectionKeyValuePair2Types())
        {
            args.Token.ThrowIfCancellationRequested();

            // Construct the 'ICollection<KeyValuePair<,>>' type for processing
            GenericInstanceTypeSignature collectionType = interopReferences.ICollection1.MakeGenericReferenceType([typeSignature.TypeArguments[0]]);

            try
            {
                InteropTypeDefinitionBuilder.ICollectionKeyValuePair2.ForwarderAttribute(
                    collectionType: collectionType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    forwarderAttributeType: out TypeDefinition forwarderAttributeType);

                InteropTypeDefinitionBuilder.ICollectionKeyValuePair2.InterfaceImpl(
                    collectionType: collectionType,
                    forwarderAttributeType: forwarderAttributeType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineSzArrayTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        foreach ((SzArrayTypeSignature typeSignature, TypeSignatureEquatableSet vtableTypes) in inputs.EnumerateSzArrayAndVtableTypes())
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    marshallerType: out TypeDefinition marshallerType);

                InteropTypeDefinitionBuilder.SzArray.ComWrappersCallback(
                    arrayType: typeSignature,
                    marshallerType: marshallerType,
                    interopDefinitions: interopDefinitions,
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
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition arrayComWrappersMarshallerType);

                InteropTypeDefinitionBuilder.SzArray.Proxy(
                    arrayType: typeSignature,
                    comWrappersMarshallerAttributeType: arrayComWrappersMarshallerType,
                    interopDefinitions: interopDefinitions,
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
    /// <param name="context">The shared emission context.</param>
    private static void RewriteMethodDefinitions(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropReferences interopReferences = context.References;

        // Rewrites within a method retain their original ordering, including rewrite kind and marker position.
        // Different methods can be rewritten independently without comparing their expensive metadata names.
        MethodRewriteInfo[][] rewritesByMethod = [.. emitState.EnumerateMethodRewriteInfos()
            .GroupBy(static rewrite => rewrite.Method)
            .Select(static group => group.Order().ToArray())];

        foreach (MethodRewriteInfo[] rewriteInfos in rewritesByMethod)
        {
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
    }

    /// <summary>
    /// Applies fixups to IL method bodies for marshalling stubs as part of two-pass IL generation.
    /// </summary>
    /// <param name="args"><inheritdoc cref="Emit" path="/param[@name='args']/node()"/></param>
    /// <param name="module">The interop module being built.</param>
    private static void FixupMethodDefinitions(InteropGeneratorArgs args, ModuleDefinition module)
    {
        InteropMethodFixup[] fixups =
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineUserDefinedTypes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropGeneratorEmitState emitState = context.EmitState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        IReadOnlyList<UserDefinedVtableGroup> groups = inputs.GetUserDefinedVtableGroups();

        // Share marshallers across types with the same explicitly implemented interfaces
        foreach (UserDefinedVtableGroup group in groups)
        {
            args.Token.ThrowIfCancellationRequested();

            TypeSignature? typeSignature = null;

            try
            {
                // Get the first user-defined with this vtable set as reference
                typeSignature = group.Representative;

                InteropTypeDefinitionBuilder.UserDefinedType.InterfaceEntriesImpl(
                    userDefinedType: typeSignature,
                    vtableTypes: group.VtableTypes,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    emitState: emitState,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    orderedVtableTypes: group.OrderedTypes.Types,
                    interfaceEntriesType: out TypeDefinition interfaceEntriesType,
                    interfaceEntriesImplType: out TypeDefinition interfaceEntriesImplType);

                InteropTypeDefinitionBuilder.UserDefinedType.ComWrappersMarshallerAttribute(
                    userDefinedType: typeSignature,
                    interfaceEntriesType: interfaceEntriesType,
                    interfaceEntriesImplType: interfaceEntriesImplType,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    out TypeDefinition comWrappersMarshallerType);

                // Track the marshaller attribute for the later proxy pass, which runs in global type order
                // rather than group order. Each proxy must reuse its group's single generated attribute type.
                group.MarshallerAttributeType = comWrappersMarshallerType;
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.UserDefinedVtableTypeCodeGenerationError(typeSignature?.Name, e).ThrowOrAttach(e);
            }
        }

        // Next, we can emit the actual proxy types for each user-defined type exposed as a CCW
        foreach (UserDefinedTypeInput input in inputs.EnumerateUserDefinedAndVtableTypes())
        {
            args.Token.ThrowIfCancellationRequested();

            try
            {
                InteropTypeDefinitionBuilder.UserDefinedType.Proxy(
                    userDefinedType: input.Type,
                    vtableTypes: input.VtableTypes,
                    comWrappersMarshallerAttributeType: input.Group.MarshallerAttributeType!,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module,
                    useWindowsUIXamlProjections: args.UseWindowsUIXamlProjections,
                    proxyType: out TypeDefinition proxyType);

                InteropTypeDefinitionBuilder.UserDefinedType.TypeMapAttributes(
                    userDefinedType: input.Type,
                    proxyType: proxyType,
                    interopReferences: interopReferences,
                    module: module);
            }
            catch (Exception e)
            {
                WellKnownInteropExceptions.UserDefinedTypeCodeGenerationError(input.Type.Name, e).ThrowOrAttach(e);
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
            _ = interopDefinitions.IIDRvaDataSize_16;
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineDynamicImplementationDetailTypes(InteropGeneratorEmissionContext context)
    {
        InteropDefinitions interopDefinitions = context.Definitions;
        ModuleDefinition module = context.Module;

        try
        {
            // Emit all shared COM interface entries types that are programmatically generated for user-defined types
            foreach (TypeDefinition typeDefinition in interopDefinitions.EnumerateUserDefinedInterfaceEntriesTypes())
            {
                module.TopLevelTypes.Add(typeDefinition);
            }

            // Also emit interface entries types for SZ arrays, same as for user-defined types above
            foreach (TypeDefinition typeDefinition in interopDefinitions.EnumerateSzArrayInterfaceEntriesTypes())
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
    /// <param name="context">The shared emission context.</param>
    private static void DefineDynamicCustomMappedTypeMapEntries(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

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
    /// Defines the external type map entries for Windows Runtime IID lookups.
    /// </summary>
    /// <param name="context">The shared emission context.</param>
    private static void DefineWindowsRuntimeIidTypeMapEntries(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorDiscoveryState discoveryState = context.DiscoveryState;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        // Skip the sidecar IID map when no input references 'GetIid<T>'
        if (!discoveryState.HasIidLookupReferences)
        {
            return;
        }

        try
        {
            WindowsRuntimeIidTypeMapBuilder.Build(
                args: args,
                inputs: inputs,
                interopDefinitions: interopDefinitions,
                interopReferences: interopReferences,
                module: module);
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.IidTypeMapEntriesCodeGenerationError(e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Defines the <c>[IgnoresAccessChecksTo]</c> attribute, and applies it to the assembly for each input reference.
    /// </summary>
    /// <param name="context">The shared emission context.</param>
    private static void DefineIgnoresAccessChecksToAttributes(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorDiscoveryState discoveryState = context.DiscoveryState;
        InteropDefinitions interopDefinitions = context.Definitions;
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

        try
        {
            // Emit the '[IgnoresAccessChecksTo]' type first
            module.TopLevelTypes.Add(interopDefinitions.IgnoresAccessChecksToAttribute);

            // Next, emit all the '[IgnoresAccessChecksTo]' attributes for each type
            IgnoresAccessChecksToBuilder.AssemblyAttributes(
                winRTSdkXamlProjectionModule: discoveryState.WindowsRuntimeSdkXamlProjectionModule,
                winRTProjectionModule: discoveryState.WindowsRuntimeSdkProjectionModule,
                winRTComponentModule: discoveryState.WindowsRuntimeComponentModule,
                referencePathModules: discoveryState.Modules.Values,
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
    /// <param name="context">The shared emission context.</param>
    private static void EmitMetadataAssemblyAttributes(InteropGeneratorEmissionContext context)
    {
        InteropReferences interopReferences = context.References;
        ModuleDefinition module = context.Module;

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
    /// <param name="fingerprint">The emission fingerprint to cache with the output, or an empty span to disable caching.</param>
    /// <param name="payloads">The assembly attribute payloads serialized by the final ordering pass.</param>
    private static void WriteInteropModuleToDisk(
        InteropGeneratorArgs args,
        ModuleDefinition module,
        ReadOnlySpan<byte> fingerprint,
        CustomAttributePayloadCache payloads)
    {
        string winRTInteropAssemblyPath = Path.Combine(args.GeneratedAssemblyDirectory, InteropNames.WindowsRuntimeInteropDllName);

        try
        {
            if (fingerprint.IsEmpty)
            {
                InteropModuleWriter.Write(module, winRTInteropAssemblyPath, payloads);
            }
            else
            {
                InteropGenerationCache.Write(args, module, fingerprint, payloads);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.EmitDllError(e).ThrowOrAttach(e);
        }
    }
}