// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Discovery;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Resolvers;

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

        // Get the set of assemblies to actually process (filtered from all input assemblies)
        string[] assembliesToProcess = GetAssembliesToProcess(args);

        // Initialize the assembly resolver (we need to reuse this to allow caching)
        PathAssemblyResolver pathAssemblyResolver = new(assembliesToProcess);

        // Initialize the state, which contains all the discovered info we'll use for generation.
        // No additional parameters will be passed to later steps: all the info is in this object.
        InteropGeneratorDiscoveryState discoveryState = new() { AssemblyResolver = pathAssemblyResolver };

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

        // We want to ensure the state will never be mutated after this method completes
        discoveryState.MakeReadOnly();

        // Validate referenced assemblies for CsWinRT 2.x
        ValidateWinRTRuntimeDllVersion2References(args, discoveryState);

        return discoveryState;
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
        ModuleDefinition module;

        // Try to load the .dll at the current path
        try
        {
            module = ModuleDefinition.FromFile(path, ((PathAssemblyResolver)discoveryState.AssemblyResolver).ReaderParameters);
        }
        catch (BadImageFormatException)
        {
            // The input .dll is not a valid .NET assembly. This is generally the case either for
            // native .dll-s, or for malformed .NET .dll-s. We don't need to worry about either one.
            return;
        }

        discoveryState.TrackModuleDefinition(path, module);

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
            foreach (TypeDefinition type in module.GetAllTypes())
            {
                args.Token.ThrowIfCancellationRequested();

                InteropTypeDiscovery.TryTrackTypeHierarchyType(type, args, discoveryState);
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
            InteropReferences interopReferences = CreateDiscoveryInteropReferences(module);

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

            foreach (GenericInstanceTypeSignature typeSignature in module.EnumerateGenericInstanceTypeSignatures())
            {
                args.Token.ThrowIfCancellationRequested();

                // Track the constructed generic type (if it's not applicable, it will be a no-op)
                InteropTypeDiscovery.TryTrackGenericTypeInstance(
                    typeSignature: typeSignature,
                    args: args,
                    discoveryState: discoveryState,
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

            foreach (SzArrayTypeSignature typeSignature in module.EnumerateSzArrayTypeSignatures())
            {
                args.Token.ThrowIfCancellationRequested();

                // Track the SZ array type (if it's not applicable, it will be a no-op)
                InteropTypeDiscovery.TryTrackSzArrayType(
                    typeSignature: typeSignature,
                    args: args,
                    discoveryState: discoveryState,
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
        IEnumerable<string> invalidModuleNames = discoveryState.ModuleDefinitions
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
    [SuppressMessage("Style", "IDE0059", Justification = "Creating the 'AssemblyDefinition'-s is used to bind them to the contained modules.")]
    private static InteropReferences CreateDiscoveryInteropReferences(ModuleDefinition module)
    {
        // Create the interop references scoped to this module, which we need to lookup some references from
        // the 'WinRT.Runtime.dll' assembly. We haven't loaded it just here here, so we can't use the real
        // module definition for it. Instead, we just create an empty one here. This is only used to create
        // type and member references to APIs defined in that module, so this is good enough for this scenario.
        // We also do the same for the Windows Runtime projection assembly, the exact version doesn't matter.
        Version windowsRuntimeVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        AssemblyReference windowsRuntimeAssembly = new("WinRT.Runtime2"u8, new Version(3, 0, 0, 0));
        AssemblyReference windowsSdkProjectionAssembly = new("Microsoft.Windows.SDK.NET"u8, new Version(10, 0, 0, 0));

        // Set the public keys, as it's needed to ensure references compare as equals as expected
        windowsRuntimeAssembly.PublicKeyOrToken = InteropValues.PublicKeyData;
        windowsRuntimeAssembly.HasPublicKey = true;
        windowsSdkProjectionAssembly.PublicKeyOrToken = InteropValues.PublicKeyData;
        windowsSdkProjectionAssembly.HasPublicKey = true;

        // Import both assembly references, so the resolution scope for them is set correctly during discovery.
        // This is requires so that all kinds of signature comparisons during discovery actually work correctly.
        return new(module.CorLibTypeFactory, windowsRuntimeAssembly.Import(module), windowsSdkProjectionAssembly.Import(module));
    }

    /// <summary>
    /// Gets the set of assemblies that need to be processed by the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The set of assemblies that need to be processed by the generator.</returns>
    private static string[] GetAssembliesToProcess(InteropGeneratorArgs args)
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
        // the 'cswinrtimplgen' tool, which we don't need to process.
        foreach (string path in args.ImplementationAssemblyPaths)
        {
            if (!projectionFileNames.GetAlternateLookup<ReadOnlySpan<char>>().Contains(Path.GetFileName(path.AsSpan())))
            {
                assembliesToProcess.Add(path);
            }
        }

        return [.. assembliesToProcess];
    }
}