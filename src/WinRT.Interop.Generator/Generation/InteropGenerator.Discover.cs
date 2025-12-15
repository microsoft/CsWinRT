// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using WindowsRuntime.InteropGenerator.Visitors;

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

        // Initialize the assembly resolver (we need to reuse this to allow caching)
        PathAssemblyResolver pathAssemblyResolver = new(args.ReferenceAssemblyPaths);

        // Initialize the state, which contains all the discovered info we'll use for generation.
        // No additional parameters will be passed to later steps: all the info is in this object.
        InteropGeneratorDiscoveryState discoveryState = new() { AssemblyResolver = pathAssemblyResolver };

        try
        {
            // Load and process all modules, potentially in parallel
            ParallelLoopResult result = Parallel.ForEach(
                source: args.ReferenceAssemblyPaths.Concat([args.OutputAssemblyPath]),
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

                // We only care about projected Windows Runtime classes
                if (!type.IsProjectedWindowsRuntimeClassType)
                {
                    continue;
                }

                // Ignore types that don't have another base class
                if (!type.HasBaseType(out ITypeDefOrRef? baseType))
                {
                    continue;
                }

                // We need to resolve the base type to be able to look up attributes on it
                if (!baseType.IsFullyResolvable(out TypeDefinition? baseDefinition))
                {
                    WellKnownInteropExceptions.WindowsRuntimeClassTypeNotResolvedWarning(baseType, type).LogOrThrow(args.TreatWarningsAsErrors);

                    continue;
                }

                // If the base type is also a projected Windows Runtime type, track it
                if (baseType.IsProjectedWindowsRuntimeType)
                {
                    discoveryState.TrackTypeHierarchyEntry(type.FullName, baseType.FullName);
                }
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

                // Ignore all type definitions with generic parameters, because they would be
                // unconstructed (by definition). We'll process instantiations that we can see
                // separately in the discovery phase, same as we do for constructed interfaces.
                if (type.HasGenericParameters)
                {
                    continue;
                }

                // Track the type (if it's not applicable, we just ignore it)
                _ = InteropTypeDiscovery.TryTrackExposedUserDefinedType(
                    typeDefinition: type,
                    typeSignature: type.ToTypeSignature(),
                    args: args,
                    discoveryState: discoveryState,
                    interopReferences: interopReferences);
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

                // Filter all constructed generic type signatures we have. We don't care about generic type
                // definitions (eg. 'TypedEventHandler`1<!0, !1>') for the purposes of marshalling code.
                if (!typeSignature.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
                {
                    continue;
                }

                // Track the constructed generic type (ignore it if not applicable)
                _ = InteropTypeDiscovery.TryTrackGenericTypeInstance(
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

                // Filter all constructed generic type signatures we have. We don't care about
                // generic type definitions (eg. '!0[]') for the purposes of marshalling code.
                if (!typeSignature.AcceptVisitor(IsConstructedGenericTypeVisitor.Instance))
                {
                    continue;
                }

                // Ignore types that are not fully resolvable (this likely means a .dll is missing)
                if (!typeSignature.IsFullyResolvable(out _))
                {
                    // Log a warning the first time we fail to resolve this SZ array in this module
                    if (discoveryState.TrackFailedResolutionType(typeSignature, module))
                    {
                        WellKnownInteropExceptions.SzArrayTypeSignatureNotResolvedError(typeSignature, module).LogOrThrow(args.TreatWarningsAsErrors);
                    }

                    continue;
                }

                // Ignore array types that are not Windows Runtime types
                if (!typeSignature.IsWindowsRuntimeType(interopReferences))
                {
                    continue;
                }

                // Track all SZ array types, as we'll need to emit marshalling code for them
                discoveryState.TrackSzArrayType(typeSignature);
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

        return new(module.CorLibTypeFactory, windowsRuntimeAssembly.Import(module), windowsSdkProjectionAssembly.Import(module));
    }
}