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
        if (!module.IsOrReferencesWindowsRuntimeAssembly && !module.IsWindowsRuntimeAssembly)
        {
            return;
        }

        // If the module references the CsWinRT 2.x runtime assembly, we need to stop, as it's invalid.
        // We'll emit an error after loading all modules, to let the user know of the wrong configuration.
        if (module.ReferencesWinRTRuntimeDllVersion2)
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
                if (type.BaseType is null || SignatureComparer.IgnoreVersion.Equals(type.BaseType, module.CorLibTypeFactory.Object))
                {
                    continue;
                }

                // If the base type is also a projected Windows Runtime type, track it
                if (type.BaseType.IsProjectedWindowsRuntimeType)
                {
                    discoveryState.TrackTypeHierarchyEntry(type.FullName, type.BaseType.FullName);
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DiscoverTypeHierarchyTypesError(module.Name, e);
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

                // We want to process all non-generic user-defined types that are potentially exposed to Windows Runtime
                if (type.IsPossiblyWindowsRuntimeExposedType &&
                    !type.IsProjectedWindowsRuntimeType &&
                    !type.IsWindowsRuntimeManagedOnlyType(interopReferences))
                {
                    // Since we're reusing the builder for all types, make sure to clear it first
                    interfaces.Clear();

                    // Gather all implemented Windows Runtime interfaces for the current type
                    for (TypeDefinition? currentType = type;
                        currentType is not null && !SignatureComparer.IgnoreVersion.Equals(currentType, module.CorLibTypeFactory.Object);
                        currentType = currentType.BaseType?.Resolve())
                    {
                        foreach (InterfaceImplementation implementation in currentType.Interfaces)
                        {
                            if (implementation.Interface?.IsProjectedWindowsRuntimeType is true ||
                                implementation.Interface?.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences) is true ||
                                (implementation.Interface?.ToReferenceTypeSignature() is GenericInstanceTypeSignature genSig &&
                                genSig.GenericType.IsCustomMappedWindowsRuntimeGenericInterfaceType(interopReferences) &&
                                genSig.TypeArguments.All(arg => arg.IsFullyResolvable && arg.Resolve()!.IsProjectedWindowsRuntimeType)))
                            {
                                interfaces.Add(implementation.Interface.ToReferenceTypeSignature());
                            }
                        }
                    }

                    // If the user-defined type doesn't implement any Windows Runtime interfaces, it's not considered exposed
                    if (interfaces.IsEmpty)
                    {
                        continue;
                    }

                    discoveryState.TrackUserDefinedType(type.ToTypeSignature(), interfaces.ToEquatableSet());
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DiscoverExposedUserDefinedTypesError(module.Name, e);
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

                // Ignore types that are not fully resolvable (this likely means a .dll is missing)
                if (!typeSignature.IsFullyResolvable)
                {
                    continue;
                }

                TypeDefinition typeDefinition = typeSignature.Resolve()!;

                // Gather all known delegate types. We want to gather all projected delegate types, plus any
                // custom-mapped ones (e.g. 'EventHandler<TEventArgs>' and 'EventHandler<TSender, TEventArgs>').
                // We need to check whether the type is a projected Windows Runtime type from the resolved type
                // definition, and not from the generic type definition we can retrieve from the type signature.
                // If we did the latter, the resulting type definition would not include any custom attributes.
                if (typeDefinition.IsDelegate &&
                    (typeSignature.IsCustomMappedWindowsRuntimeDelegateType(interopReferences) ||
                     typeDefinition.IsProjectedWindowsRuntimeType))
                {
                    discoveryState.TrackGenericDelegateType(typeSignature);

                    continue;
                }

                // Gather all 'KeyValuePair<,>' instances
                if (typeDefinition.IsValueType &&
                    SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.KeyValuePair))
                {
                    discoveryState.TrackKeyValuePairType(typeSignature);

                    continue;
                }

                // Track all projected Windows Runtime generic interfaces
                if (typeDefinition.IsInterface)
                {
                    discoveryState.TrackGenericInterfaceType(typeSignature, interopReferences);

                    // We also want to crawl base interfaces
                    foreach (GenericInstanceTypeSignature interfaceSignature in typeDefinition.EnumerateGenericInstanceInterfaceSignatures(typeSignature))
                    {
                        discoveryState.TrackGenericInterfaceType(interfaceSignature, interopReferences);
                    }

                    continue;
                }

                // Also track all user-defined types that should be exposed to Windows Runtime
                if (typeDefinition.IsPossiblyWindowsRuntimeExposedType &&
                    !typeDefinition.IsProjectedWindowsRuntimeType &&
                    !typeDefinition.IsWindowsRuntimeManagedOnlyType(interopReferences))
                {
                    // TODO
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DiscoverGenericTypeInstantiationsError(module.Name, e);
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
                if (!typeSignature.IsFullyResolvable)
                {
                    continue;
                }

                // Track all SZ array types, as we'll need to emit marshalling code for them
                discoveryState.TrackSzArrayType(typeSignature);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DiscoverSzArrayTypesError(module.Name, e);
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
            .Where(static module => module.ReferencesWinRTRuntimeDllVersion2)
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
        AssemblyReference windowsRuntimeAssembly = new("WinRT.Runtime"u8, new Version(3, 0, 0, 0));
        AssemblyReference windowsSdkProjectionAssembly = new("Microsoft.Windows.SDK.NET"u8, new Version(10, 0, 0, 0));

        // Set the public keys, as it's needed to ensure references compare as equals as expected
        windowsRuntimeAssembly.PublicKeyOrToken = InteropValues.PublicKeyData;
        windowsRuntimeAssembly.HasPublicKey = true;
        windowsSdkProjectionAssembly.PublicKeyOrToken = InteropValues.PublicKeyData;
        windowsSdkProjectionAssembly.HasPublicKey = true;

        return new(module.CorLibTypeFactory, windowsRuntimeAssembly.Import(module), windowsSdkProjectionAssembly.Import(module));
    }
}
