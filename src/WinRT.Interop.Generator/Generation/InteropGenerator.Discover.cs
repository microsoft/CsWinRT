// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Errors;
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
    private static InteropGeneratorState Discover(InteropGeneratorArgs args)
    {
        PathAssemblyResolver pathAssemblyResolver = new(args.ReferencePath);

        InteropGeneratorState state = new() { AssemblyResolver = pathAssemblyResolver };

        foreach (string path in args.ReferencePath.Concat([args.AssemblyPath]))
        {
            LoadAndProcessModule(args, state, path);
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
        if (!module.IsOrReferencesWindowsSDKProjectionsAssembly())
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
                if (!type.IsProjectedWindowsRuntimeClassType())
                {
                    continue;
                }

                // Ignore types that don't have another base class
                if (type.BaseType is null || SignatureComparer.Default.Equals(type.BaseType, module.CorLibTypeFactory.Object))
                {
                    continue;
                }

                // If the base type is also a projected Windows Runtime type, track it
                if (type.BaseType.IsProjectedWindowsRuntimeType())
                {
                    state.TrackTypeHierarchyEntry(type.FullName, type.BaseType.FullName);
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown())
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
            foreach (TypeSpecification typeSpecification in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
            {
                args.Token.ThrowIfCancellationRequested();

                if (typeSpecification.Resolve() is { IsDelegate: true } &&
                    typeSpecification.Signature is GenericInstanceTypeSignature { GenericType.Name.Value: "TypedEventHandler`2" } typeSignature)
                {
                    state.TrackGenericDelegateType(typeSignature);
                }

                if (typeSpecification.Resolve() is { IsValueType: true } &&
                    typeSpecification.Signature is GenericInstanceTypeSignature { GenericType.Name.Value: "KeyValuePair`2" } keyValuePairType)
                {
                    state.TrackKeyValuePairType(keyValuePairType);
                }
            }
        }
        catch (Exception e) when (!e.IsWellKnown())
        {
            throw WellKnownInteropExceptions.DiscoverGenericTypeInstantiationsError(module.Name, e);
        }
    }
}
