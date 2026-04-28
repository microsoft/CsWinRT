// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE;
using WindowsRuntime.InteropGenerator.Discovery;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal partial class InteropGenerator
{
    /// <summary>
    /// Runs the discovery logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The resulting state.</returns>
    private static InteropGeneratorDiscoveryState Discover(InteropGeneratorArgs args)
    {
        args.Token.ThrowIfCancellationRequested();

        // Get the .NET runtime version that the output .dll is targeting
        DotNetRuntimeInfo targetRuntime = GetTargetRuntimeInfo(args);

        args.Token.ThrowIfCancellationRequested();

        // Get the set of assemblies to actually process (filtered from all input assemblies),
        // and the full set of paths for the assembly resolver (which also includes the actual
        // projection assembly paths, needed for dependency resolution).
        (string[] assembliesToProcess, string[] resolverPaths) = GetAssembliesToProcess(args);

        // Initialize the assembly resolver (we need to reuse this to allow caching)
        PathAssemblyResolver pathAssemblyResolver = new(resolverPaths);

        args.Token.ThrowIfCancellationRequested();

        // Create the runtime context that we'll reuse for the entire generation. This will internally
        // use our assembly resolver, and it will also take care of caching all loaded modules. Every
        // module load will go through this object, rather than using the assembly resolver directly.
        RuntimeContext runtimeContext = new(targetRuntime, pathAssemblyResolver);

        // Initialize the state, which contains all the discovered info we'll use for generation.
        // No additional parameters will be passed to later steps: all the info is in this object.
        InteropGeneratorDiscoveryState discoveryState = new() { RuntimeContext = runtimeContext };

        // First, load the special 'WinRT.Sdk.Projection.dll', 'WinRT.Sdk.Xaml.Projection.dll', 'WinRT.Projection.dll'
        // and 'WinRT.Component.dll' modules (the last three are optional). These are necessary for surfacing some
        // information needed to generate code, that is not present otherwise.
        LoadWinRTModules(args, discoveryState);

        try
        {
            // Load and process all modules, potentially in parallel
            ParallelLoopResult result = Parallel.ForEach(
                source: assembliesToProcess.Concat([args.OutputAssemblyPath]),
                parallelOptions: new ParallelOptions { CancellationToken = args.Token, MaxDegreeOfParallelism = args.MaxDegreesOfParallelism },
                body: path => LoadAndProcessModule(args, discoveryState, path));

            // Ensure we did complete all iterations (this should always be the case)
            if (!result.IsCompleted)
            {
                throw WellKnownInteropExceptions.LoadAndDiscoverModulesLoopDidNotComplete();
            }
        }
        catch (AggregateException e)
        {
            Exception innerException = e.InnerExceptions.FirstOrDefault()!;

            // If the first inner exception is well known, just rethrow it.
            // We're not concerned about always throwing the same one across
            // re-runs with parallelism. It can be disabled for debugging.
            throw innerException.IsWellKnown
                ? innerException
                : WellKnownInteropExceptions.LoadAndDiscoverModulesLoopError(innerException);
        }

        // Discover activation factory types from the component module, if available.
        // These types are in 'WinRT.Component.dll' (which is not in the main processing set),
        // and they need CCW support so that 'WindowsRuntimeInterfaceMarshaller<IActivationFactory>'
        // can create COM callable wrappers for them when they are handed out to native callers.
        DiscoverComponentActivationFactoryTypes(args, discoveryState);

        // We want to ensure the state will never be mutated after this method completes
        discoveryState.MakeReadOnly();

        // Validate referenced assemblies for CsWinRT 2.x
        ValidateWinRTRuntimeDllVersion2References(args, discoveryState);

        return discoveryState;
    }

    /// <summary>
    /// Gets the <see cref="DotNetRuntimeInfo"/> for the target output assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The <see cref="DotNetRuntimeInfo"/> for the target output assembly.</returns>
    private static DotNetRuntimeInfo GetTargetRuntimeInfo(InteropGeneratorArgs args)
    {
        PEImage outputAssemblyImage;

        // Load the output assembly as a PE image (we don't need the full .NET module yet)
        try
        {
            outputAssemblyImage = PEImage.FromFile(args.OutputAssemblyPath);
        }
        catch (Exception e)
        {
            throw WellKnownInteropExceptions.OutputAssemblyLoadError(args.OutputAssemblyPath, e);
        }

        // Probe the .NET runtime version for the output .dll (which might be greater than that of 'WinRT.Runtime.dll')
        if (!TargetRuntimeProber.TryGetLikelyTargetRuntime(outputAssemblyImage, out DotNetRuntimeInfo targetRuntime))
        {
            throw WellKnownInteropExceptions.OutputAssemblyRuntimeVersionNotFound(args.OutputAssemblyPath);
        }

        // Validate that the probed runtime version is valid
        if (!targetRuntime.IsNetCoreApp || targetRuntime.Version < new Version(10, 0))
        {
            throw WellKnownInteropExceptions.OutputAssemblyRuntimeVersionNotSupported(args.OutputAssemblyPath, targetRuntime);
        }

        return targetRuntime;
    }

    /// <summary>
    /// Loads the special WinRT module definitions.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    private static void LoadWinRTModules(InteropGeneratorArgs args, InteropGeneratorDiscoveryState discoveryState)
    {
        // Load the 'WinRT.Sdk.Projection.dll' module, this should always be available
        ModuleDefinition winRTSdkProjectionModule = discoveryState.RuntimeContext.LoadModule(args.WinRTSdkProjectionAssemblyPath);

        discoveryState.TrackWindowsRuntimeSdkProjectionModule(winRTSdkProjectionModule);

        args.Token.ThrowIfCancellationRequested();

        // Load the 'WinRT.Sdk.Xaml.Projection.dll' module, if available
        if (args.WinRTSdkXamlProjectionAssemblyPath is not null)
        {
            ModuleDefinition winRTSdkXamlProjectionModule = discoveryState.RuntimeContext.LoadModule(args.WinRTSdkXamlProjectionAssemblyPath);

            discoveryState.TrackWindowsRuntimeSdkXamlProjectionModule(winRTSdkXamlProjectionModule);

            args.Token.ThrowIfCancellationRequested();
        }

        // Load the 'WinRT.Projection.dll' module, if available
        if (args.WinRTProjectionAssemblyPath is not null)
        {
            ModuleDefinition winRTProjectionModule = discoveryState.RuntimeContext.LoadModule(args.WinRTProjectionAssemblyPath);

            discoveryState.TrackWindowsRuntimeProjectionModule(winRTProjectionModule);

            args.Token.ThrowIfCancellationRequested();
        }

        // Load the 'WinRT.Component.dll' module, if available
        if (args.WinRTComponentAssemblyPath is not null)
        {
            ModuleDefinition winRTComponentModule = discoveryState.RuntimeContext.LoadModule(args.WinRTComponentAssemblyPath);

            discoveryState.TrackWindowsRuntimeComponentModule(winRTComponentModule);
        }
    }

    /// <summary>
    /// Loads and processes a module definition.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="path">The path of the module to load.</param>
    private static void LoadAndProcessModule(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        string path)
    {
        ReadOnlySpan<char> fileName = Path.GetFileName(path.AsSpan());

        // Validate that the possible private implementation detail .dll-s we expect have a matching path. These are:
        //   - 'WinRT.Sdk.Projection.dll': the precompiled projection assembly for the Windows SDK.
        //   - 'WinRT.Sdk.Xaml.Projection.dll': the precompiled projection assembly for the Windows SDK XAML types.
        //   - 'WinRT.Projection.dll': the generated merged projection assembly.
        //   - 'WinRT.Component.dll': the optional generated merged component assembly.
        if ((fileName.SequenceEqual(InteropNames.WindowsRuntimeSdkProjectionDllName) && path != args.WinRTSdkProjectionAssemblyPath) ||
            (fileName.SequenceEqual(InteropNames.WindowsRuntimeSdkXamlProjectionDllName) && path != args.WinRTSdkXamlProjectionAssemblyPath) ||
            (fileName.SequenceEqual(InteropNames.WindowsRuntimeProjectionDllName) && path != args.WinRTProjectionAssemblyPath) ||
            (fileName.SequenceEqual(InteropNames.WindowsRuntimeComponentDllName) && path != args.WinRTComponentAssemblyPath))
        {
            throw WellKnownInteropExceptions.ReservedDllOriginalPathMismatch(fileName.ToString());
        }

        // If the current module is one of those two .dll-s, we just skip it. They will be loaded separately (see above).
        // However since they're also passed in the reference set (as they need to be referenced by the app directly),
        // they will also show up here. This is intended, and it simplifies the targets (no need for them to filter items).
        if (fileName.SequenceEqual(InteropNames.WindowsRuntimeSdkProjectionDllName) ||
            fileName.SequenceEqual(InteropNames.WindowsRuntimeSdkXamlProjectionDllName) ||
            fileName.SequenceEqual(InteropNames.WindowsRuntimeProjectionDllName) ||
            fileName.SequenceEqual(InteropNames.WindowsRuntimeComponentDllName))
        {
            return;
        }

        // Validate that the reserved 'WinRT.Interop.dll' is not passed as input. This is the .dll that this tool is generating,
        // so for it to already exist and be passed as input would always be invalid (and would indicate some kind of build issue).
        if (fileName.SequenceEqual(InteropNames.WindowsRuntimeInteropDllName))
        {
            throw WellKnownInteropExceptions.ReservedDllNameReferenceError(fileName.ToString());
        }

        ModuleDefinition module;

        // Try to load the .dll at the current path
        try
        {
            module = discoveryState.RuntimeContext.LoadModule(path);
        }
        catch (BadImageFormatException)
        {
            // The input .dll is not a valid .NET assembly. This is generally the case either for
            // native .dll-s, or for malformed .NET .dll-s. We don't need to worry about either one.
            return;
        }

        discoveryState.TrackModule(path, module);

        args.Token.ThrowIfCancellationRequested();

        // We're only interested in harvesting .dll-s which reference the Windows SDK projections.
        // This is true for all .dll-s that were built targeting 'netX.0-windows10.0.XXXX.0'.
        // So this check effectively lets us filter all .dll-s that were in projects with this TFM.
        if (!module.ReferencesWindowsRuntimeAssembly && !module.IsWindowsRuntimeModule)
        {
            return;
        }

        // If the module references the CsWinRT 2.x runtime assembly, we need to stop, as it's invalid.
        // We'll emit an error after loading all modules, to let the user know of the wrong configuration.
        if (module.ReferencesWindowsRuntimeVersion2Assembly)
        {
            discoveryState.MarkWinRTRuntimeDllVersion2References();

            return;
        }

        args.Token.ThrowIfCancellationRequested();

        // Discover all type hierarchy types
        DiscoverTypeHierarchyTypes(args, discoveryState, module);

        args.Token.ThrowIfCancellationRequested();

        // Discover all exposed non-generic, user-defined types
        DiscoverExposedUserDefinedTypes(args, discoveryState, module);

        args.Token.ThrowIfCancellationRequested();

        // Discover all generic type instantiations
        DiscoverGenericTypeInstantiations(args, discoveryState, module);

        args.Token.ThrowIfCancellationRequested();

        // Discover all SZ array types
        DiscoverSzArrayTypes(args, discoveryState, module);
    }

    /// <summary>
    /// Discovers all type hierarchy types in a given assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void DiscoverTypeHierarchyTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        ModuleDefinition module)
    {
        try
        {
            // Optimization: we only need to crawl Windows Runtime reference assemblies for
            // this, since normal assemblies will never contain projected types we could want.
            if (module.Assembly is not { IsWindowsRuntimeReferenceAssembly: true })
            {
                return;
            }

            InteropReferences interopReferences = CreateDiscoveryInteropReferences(module);

            foreach (TypeDefinition type in module.GetAllTypes())
            {
                args.Token.ThrowIfCancellationRequested();

                InteropTypeDiscovery.TryTrackTypeHierarchyType(type, args, discoveryState, interopReferences);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DiscoverTypeHierarchyTypesError(module.Name, e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Discovers all (non-generic) exposed user-defined types in a given assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void DiscoverExposedUserDefinedTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        ModuleDefinition module)
    {
        try
        {
            // Optimization: we only need to crawl assemblies that are not Windows Runtime
            // reference assemblies, since otherwise they'd only contain projections and no
            // user-defined types we'd actually need to generate CCW marshalling code for.
            if (module.Assembly is not { IsWindowsRuntimeReferenceAssembly: false })
            {
                return;
            }

            InteropReferences interopReferences = CreateDiscoveryInteropReferences(module);
            InteropDefinitions interopDefinitions = new(
                interopReferences: interopReferences,
                windowsRuntimeSdkProjectionModule: discoveryState.WindowsRuntimeSdkProjectionModule!,
                windowsRuntimeSdkXamlProjectionModule: discoveryState.WindowsRuntimeSdkXamlProjectionModule,
                windowsRuntimeProjectionModule: discoveryState.WindowsRuntimeProjectionModule,
                windowsRuntimeComponentModule: discoveryState.WindowsRuntimeComponentModule);

            // We can share a single builder when processing all types to reduce allocations
            TypeSignatureEquatableSet.Builder interfaces = new();

            foreach (TypeDefinition type in module.GetAllTypes())
            {
                args.Token.ThrowIfCancellationRequested();

                // Track the type (if it's not applicable, it will be a no-op)
                InteropTypeDiscovery.TryTrackExposedUserDefinedType(
                    typeDefinition: type,
                    typeSignature: type.ToTypeSignature(),
                    args: args,
                    discoveryState: discoveryState,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DiscoverExposedUserDefinedTypesError(module.Name, e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Discovers all generic type instantiations in a given assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void DiscoverGenericTypeInstantiations(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        ModuleDefinition module)
    {
        try
        {
            InteropReferences interopReferences = CreateDiscoveryInteropReferences(module);
            InteropDefinitions interopDefinitions = new(
                interopReferences: interopReferences,
                windowsRuntimeSdkProjectionModule: discoveryState.WindowsRuntimeSdkProjectionModule!,
                windowsRuntimeSdkXamlProjectionModule: discoveryState.WindowsRuntimeSdkXamlProjectionModule,
                windowsRuntimeProjectionModule: discoveryState.WindowsRuntimeProjectionModule,
                windowsRuntimeComponentModule: discoveryState.WindowsRuntimeComponentModule);

            foreach (GenericInstanceTypeSignature typeSignature in module.EnumerateGenericInstanceTypeSignatures())
            {
                args.Token.ThrowIfCancellationRequested();

                // Track the constructed generic type (if it's not applicable, it will be a no-op)
                InteropTypeDiscovery.TryTrackGenericTypeInstance(
                    typeSignature: typeSignature,
                    args: args,
                    discoveryState: discoveryState,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DiscoverGenericTypeInstantiationsError(module.Name, e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Discovers all SZ array types in a given assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void DiscoverSzArrayTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        ModuleDefinition module)
    {
        try
        {
            InteropReferences interopReferences = CreateDiscoveryInteropReferences(module);
            InteropDefinitions interopDefinitions = new(
                interopReferences: interopReferences,
                windowsRuntimeSdkProjectionModule: discoveryState.WindowsRuntimeSdkProjectionModule!,
                windowsRuntimeSdkXamlProjectionModule: discoveryState.WindowsRuntimeSdkXamlProjectionModule,
                windowsRuntimeProjectionModule: discoveryState.WindowsRuntimeProjectionModule,
                windowsRuntimeComponentModule: discoveryState.WindowsRuntimeComponentModule);

            foreach (SzArrayTypeSignature typeSignature in module.EnumerateSzArrayTypeSignatures())
            {
                args.Token.ThrowIfCancellationRequested();

                // Track the SZ array type (both for Windows Runtime types and user-defined types)
                InteropTypeDiscovery.TryTrackSzArrayType(
                    typeSignature: typeSignature,
                    args: args,
                    discoveryState: discoveryState,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: module);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DiscoverSzArrayTypesError(module.Name, e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Discovers activation factory types from the component module (<c>WinRT.Component.dll</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Activation factory types are generated by <c>cswinrt.exe</c> in component mode and live in <c>WinRT.Component.dll</c>.
    /// They implement <c>WindowsRuntime.InteropServices.IActivationFactory</c> (and potentially additional static/factory
    /// interfaces), and need CCW support so that the marshalling infrastructure can create COM callable wrappers for them.
    /// </para>
    /// <para>
    /// Since <c>WinRT.Component.dll</c> is not included in the main set of assemblies to process, these types would not be
    /// discovered by the normal <see cref="DiscoverExposedUserDefinedTypes"/> logic. This method fills that gap by scanning
    /// the component module for types that implement <c>IActivationFactory</c> and feeding them into the discovery pipeline.
    /// </para>
    /// </remarks>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    private static void DiscoverComponentActivationFactoryTypes(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState)
    {
        if (discoveryState.WindowsRuntimeComponentModule is not { } componentModule)
        {
            return;
        }

        try
        {
            InteropReferences interopReferences = CreateDiscoveryInteropReferences(componentModule);
            InteropDefinitions interopDefinitions = new(
                interopReferences: interopReferences,
                windowsRuntimeSdkProjectionModule: discoveryState.WindowsRuntimeSdkProjectionModule!,
                windowsRuntimeSdkXamlProjectionModule: discoveryState.WindowsRuntimeSdkXamlProjectionModule,
                windowsRuntimeProjectionModule: discoveryState.WindowsRuntimeProjectionModule,
                windowsRuntimeComponentModule: componentModule);

            foreach (TypeDefinition type in componentModule.GetAllTypes())
            {
                args.Token.ThrowIfCancellationRequested();

                // Only look for types implementing IActivationFactory
                bool implementsIActivationFactory = false;

                foreach (InterfaceImplementation interfaceImpl in type.Interfaces)
                {
                    if (interfaceImpl.Interface is { Namespace: { } ns, Name: { } name } &&
                        ns == "WindowsRuntime.InteropServices" &&
                        name == "IActivationFactory")
                    {
                        implementsIActivationFactory = true;
                        break;
                    }
                }

                if (!implementsIActivationFactory)
                {
                    continue;
                }

                InteropTypeDiscovery.TryTrackExposedUserDefinedType(
                    typeDefinition: type,
                    typeSignature: type.ToTypeSignature(),
                    args: args,
                    discoveryState: discoveryState,
                    interopDefinitions: interopDefinitions,
                    interopReferences: interopReferences,
                    module: componentModule);
            }
        }
        catch (Exception e)
        {
            WellKnownInteropExceptions.DiscoverExposedUserDefinedTypesError(componentModule.Name, e).ThrowOrAttach(e);
        }
    }

    /// <summary>
    /// Validates that no assemblies targeting CsWinRT 2.x are referenced.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="discoveryState">The discovery state for this invocation.</param>
    private static void ValidateWinRTRuntimeDllVersion2References(InteropGeneratorArgs args, InteropGeneratorDiscoveryState discoveryState)
    {
        // Fast-path if no invalid module has been discovered
        if (!discoveryState.HasWinRTRuntimeDllVersion2References)
        {
            return;
        }

        // Skip the validation if the opt-out switch is set (mostly useful for testing)
        if (!args.ValidateWinRTRuntimeDllVersion2References)
        {
            return;
        }

        // Filter all invalid modules (i.e. that reference the 'WinRT.Runtime.dll' assembly version 2)
        IEnumerable<string> invalidModuleNames = discoveryState.Modules
            .Values
            .Where(static module => module.ReferencesWindowsRuntimeVersion2Assembly)
            .Select(static module => module.Name?.ToString() ?? "")
            .Order();

        throw WellKnownInteropExceptions.WinRTRuntimeDllVersion2References(invalidModuleNames);
    }

    /// <summary>
    /// Creates an <see cref="InteropReferences"/> instance that can be used for the discovery phase.
    /// </summary>
    /// <param name="module">The module currently being analyzed.</param>
    /// <returns>The <see cref="InteropReferences"/> instance to use for the discovery phase.</returns>
    private static InteropReferences CreateDiscoveryInteropReferences(ModuleDefinition module)
    {
        // Create the interop references scoped to this module, which we need to lookup some references from
        // the 'WinRT.Runtime.dll' assembly. We haven't loaded it here just yet, so we can't use the real
        // module definition for it. Instead, we just create an empty one here. This is only used to create
        // type and member references to APIs defined in that module, so this is good enough for this scenario.
        // We also do the same for the Windows Runtime projection assembly, the exact version doesn't matter.
        Version windowsRuntimeVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        AssemblyReference windowsRuntimeAssembly = new("WinRT.Runtime"u8, windowsRuntimeVersion)
        {
            // Set the public keys, as it's needed to ensure references compare as equals as expected
            PublicKeyOrToken = InteropValues.CsWinRTPublicKeyData,
            HasPublicKey = true
        };

        // Validate that the module has a runtime context, which is required
        // for reference resolution (this should just always be the case).
        if (module.RuntimeContext is null)
        {
            throw WellKnownInteropExceptions.ModuleMissingRuntimeContext(module);
        }

        // We don't need to import the 'WinRT.Runtime.dll' module here, as we're reusing the same runtime context everywhere
        return new(
            runtimeContext: module.RuntimeContext,
            corLibTypeFactory: module.CorLibTypeFactory,
            windowsRuntimeModule: windowsRuntimeAssembly);
    }

    /// <summary>
    /// Gets the set of assemblies that need to be processed by the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>A tuple of (assemblies to process, resolver paths including projection assemblies).</returns>
    private static (string[] AssembliesToProcess, string[] ResolverPaths) GetAssembliesToProcess(InteropGeneratorArgs args)
    {
        // Local path assembly resolver just scoped to the full set of reference assemblies
        PathAssemblyResolver pathAssemblyResolver = new(args.ReferenceAssemblyPaths);

        HashSet<string> projectionFileNames = new(args.ReferenceAssemblyPaths.Length, StringComparer.OrdinalIgnoreCase);
        List<string> assembliesToProcess = new(args.ImplementationAssemblyPaths.Length);

        // Iterate through the reference assemblies first to filter them
        foreach (string path in args.ReferenceAssemblyPaths)
        {
            ModuleDefinition module = ModuleDefinition.FromFile(path, pathAssemblyResolver.ReaderParameters);

            // If the current assembly is a Windows Runtime reference assembly, we want to process it
            // All other reference assemblies are ignored: we want to inspect the actual implementation.
            if (module.Assembly is { IsWindowsRuntimeReferenceAssembly: true })
            {
                _ = projectionFileNames.Add(Path.GetFileName(path));

                assembliesToProcess.Add(path);
            }
        }

        // Iterate through all implementation assemblies, and track all of them except for
        // those that have a matching Windows Runtime projection assembly. For those, the
        // implementation assembly will just be the dummy implementation .dll generated by
        // the 'cswinrtimplgen' tool, which we don't need to process. We also skip any
        // forwarder .dll-s for the reserved projection assembly names, as they are loaded
        // separately via their explicit paths and would conflict in the assembly resolver.
        foreach (string path in args.ImplementationAssemblyPaths)
        {
            ReadOnlySpan<char> fileName = Path.GetFileName(path.AsSpan());

            if (projectionFileNames.GetAlternateLookup<ReadOnlySpan<char>>().Contains(fileName))
            {
                continue;
            }

            if (fileName.SequenceEqual(InteropNames.WindowsRuntimeSdkProjectionDllName) ||
                fileName.SequenceEqual(InteropNames.WindowsRuntimeSdkXamlProjectionDllName) ||
                fileName.SequenceEqual(InteropNames.WindowsRuntimeProjectionDllName) ||
                fileName.SequenceEqual(InteropNames.WindowsRuntimeComponentDllName))
            {
                continue;
            }

            assembliesToProcess.Add(path);
        }

        return ([.. assembliesToProcess], [.. assembliesToProcess]);
    }
}