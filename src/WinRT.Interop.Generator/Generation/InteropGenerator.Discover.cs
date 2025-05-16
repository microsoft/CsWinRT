// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
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
    private static InteropGeneratorState Discover(InteropGeneratorArgs args)
    {
        // Initialize the assembly resolver (we need to reuse this to allow caching)
        PathAssemblyResolver pathAssemblyResolver = new(args.ReferencePath);

        // Initialize the state, which contains all the discovered info we'll use for generation.
        // No additional parameters will be passed to later steps: all the info is in this object.
        InteropGeneratorState state = new() { AssemblyResolver = pathAssemblyResolver };

        try
        {
            // Load and process all modules, potentially in parallel
            ParallelLoopResult result = Parallel.ForEach(
                source: args.ReferencePath.Concat([args.AssemblyPath]),
                parallelOptions: new ParallelOptions { CancellationToken = args.Token, MaxDegreeOfParallelism = args.MaxDegreesOfParallelism },
                body: path => LoadAndProcessModule(args, state, path));

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
        state.MakeReadOnly();

        return state;
    }

    /// <summary>
    /// Loads and processes a module definition.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="state">The state for this invocation.</param>
    /// <param name="path">The path of the module to load.</param>
    private static void LoadAndProcessModule(
        InteropGeneratorArgs args,
        InteropGeneratorState state,
        string path)
    {
        ModuleDefinition module;

        // Try to load the .dll at the current path
        try
        {
            module = ModuleDefinition.FromFile(path, ((PathAssemblyResolver)state.AssemblyResolver).ReaderParameters);
        }
        catch (BadImageFormatException)
        {
            // The input .dll is not a valid .NET assembly. This is generally the case either for
            // native .dll-s, or for malformed .NET .dll-s. We don't need to worry about either one.
            return;
        }

        state.TrackModuleDefinition(path, module);

        args.Token.ThrowIfCancellationRequested();

        // We're only interested in harvesting .dll-s which reference the Windows SDK projections.
        // This is true for all .dll-s that were built targeting 'netX.0-windows10.0.XXXX.0'.
        // So this check effectively lets us filter all .dll-s that were in projects with this TFM.
        if (!module.IsOrReferencesWindowsSDKProjectionsAssembly)
        {
            return;
        }

        args.Token.ThrowIfCancellationRequested();

        // Discover all type hierarchy types
        DiscoverTypeHierarchyTypes(args, state, module);

        args.Token.ThrowIfCancellationRequested();

        // Discover all generic type instantiations
        DiscoverGenericTypeInstantiations(args, state, module);
    }

    /// <summary>
    /// Discovers all type hierarchy types in a given assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="state">The state for this invocation.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void DiscoverTypeHierarchyTypes(
        InteropGeneratorArgs args,
        InteropGeneratorState state,
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
                    state.TrackTypeHierarchyEntry(type.FullName, type.BaseType.FullName);
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DiscoverTypeHierarchyTypesError(module.Name, e);
        }
    }

    /// <summary>
    /// Discovers all generic type instantiations in a given assembly.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="state">The state for this invocation.</param>
    /// <param name="module">The module currently being analyzed.</param>
    private static void DiscoverGenericTypeInstantiations(
        InteropGeneratorArgs args,
        InteropGeneratorState state,
        ModuleDefinition module)
    {
        try
        {
            // Create the interop references scoped to this module. We're not going to use any references
            // from the 'WinRT.Runtime.dll' assembly, so we can just pass 'null' here and suppress warnings.
            InteropReferences interopReferences = new(module, null!);

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

                // Gather all known delegate types. We want to gather all projected delegate types,
                // plus any custom mapped ones. For now, that's only 'EventHandler<T>'.
                if (typeDefinition.IsDelegate &&
                    (typeSignature.IsCustomMappedWindowsRuntimeDelegateType(interopReferences) ||
                     typeSignature.GenericType.IsProjectedWindowsRuntimeType))
                {
                    state.TrackGenericDelegateType(typeSignature);
                }

                // Gather all 'KeyValuePair<,>' instances
                if (typeDefinition.IsValueType &&
                    SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.KeyValuePair))
                {
                    state.TrackKeyValuePairType(typeSignature);
                }

                // Track all projected Windows Runtime generic interfaces
                if (typeDefinition.IsInterface)
                {
                    if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IEnumerator1))
                    {
                        state.TrackIEnumerator1Type(typeSignature);
                    }
                    else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IEnumerable1))
                    {
                        state.TrackIEnumerable1Type(typeSignature);
                    }
                    else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IList1))
                    {
                        state.TrackIList1Type(typeSignature);
                    }
                    else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IReadOnlyList1))
                    {
                        state.TrackIReadOnlyList1Type(typeSignature);
                    }
                    else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IDictionary2))
                    {
                        state.TrackIDictionary2Type(typeSignature);
                    }
                    else if (SignatureComparer.IgnoreVersion.Equals(typeSignature.GenericType, interopReferences.IReadOnlyDictionary2))
                    {
                        state.TrackIReadOnlyDictionary2Type(typeSignature);
                    }
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownInteropExceptions.DiscoverGenericTypeInstantiationsError(module.Name, e);
        }
    }
}
