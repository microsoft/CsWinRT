// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Builders;
using EmissionNode = WindowsRuntime.InteropGenerator.Generation.InteropGenerationWorkQueue<WindowsRuntime.InteropGenerator.Generation.InteropGeneratorEmissionContext>.Node;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
    /// <summary>The fixed emission graph, shared by all invocations.</summary>
    private static readonly InteropGenerationWorkQueue<InteropGeneratorEmissionContext> EmissionQueue = CreateEmissionQueue();

    /// <summary>
    /// Runs whole emit families as soon as their prerequisites complete.
    /// </summary>
    /// <param name="context">The shared emission context.</param>
    private static void RunEmission(InteropGeneratorEmissionContext context)
    {
        InteropGeneratorArgs args = context.Args;
        InteropGeneratorEmitInputs inputs = context.Inputs;
        ModuleDefinition module = context.Module;

        inputs.PrepareForConcurrentEmission();

        EmissionQueue.Run(context, args.MaxDegreesOfParallelism, args.Token);

        // Fixups enumerate generated types, so all emit workers must have finished
        FixupMethodDefinitions(args, module);
    }

    /// <summary>
    /// Defines the emission graph once, using node references for all dependencies.
    /// </summary>
    /// <returns>The reusable emission queue.</returns>
    private static InteropGenerationWorkQueue<InteropGeneratorEmissionContext> CreateEmissionQueue()
    {
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Builder graph = new();

        _ = graph.Add(DefineTypeHierarchyLookup);
        _ = graph.Add(DefineDynamicCustomMappedTypeMapEntries);
        _ = graph.Add(DefineWindowsRuntimeIidTypeMapEntries);
        _ = graph.Add(DefineIgnoresAccessChecksToAttributes);
        _ = graph.Add(EmitMetadataAssemblyAttributes);

        EmissionNode delegates = graph.Add(DefineGenericDelegateTypes);
        EmissionNode enumerators = graph.Add(DefineIEnumeratorTypes);
        EmissionNode enumerables = graph.Add(DefineIEnumerableTypes, enumerators);
        EmissionNode readOnlyLists = graph.Add(DefineIReadOnlyListTypes, enumerables);
        EmissionNode lists = graph.Add(DefineIListTypes, enumerables);
        EmissionNode readOnlyDictionaries = graph.Add(DefineIReadOnlyDictionaryTypes, enumerables);
        EmissionNode dictionaries = graph.Add(DefineIDictionaryTypes, enumerables);
        EmissionNode keyValuePairs = graph.Add(DefineKeyValuePairTypes);
        EmissionNode mapChangedArguments = graph.Add(DefineIMapChangedEventArgsTypes);
        EmissionNode observableVectors = graph.Add(DefineIObservableVectorTypes, delegates, lists);
        EmissionNode observableMaps = graph.Add(DefineIObservableMapTypes, delegates, dictionaries);
        EmissionNode asyncActions = graph.Add(DefineIAsyncActionWithProgressTypes, delegates);
        EmissionNode asyncOperations = graph.Add(DefineIAsyncOperationTypes, delegates);
        EmissionNode asyncProgressOperations = graph.Add(DefineIAsyncOperationWithProgressTypes, delegates);
        EmissionNode readOnlyCollections = graph.Add(DefineIReadOnlyCollectionKeyValuePair2Types, readOnlyLists, readOnlyDictionaries);
        EmissionNode collections = graph.Add(DefineICollectionKeyValuePair2Types, lists, dictionaries);
        EmissionNode[] genericFamilies =
        [
            delegates,
            enumerators,
            enumerables,
            readOnlyLists,
            lists,
            readOnlyDictionaries,
            dictionaries,
            keyValuePairs,
            mapChangedArguments,
            observableVectors,
            observableMaps,
            asyncActions,
            asyncOperations,
            asyncProgressOperations,
            readOnlyCollections,
            collections
        ];

        EmissionNode arrays = graph.Add(DefineSzArrayTypes, genericFamilies);
        EmissionNode userDefinedTypes = graph.Add(DefineUserDefinedTypes, genericFamilies);

        _ = graph.Add(RewriteMethodDefinitions, arrays);
        _ = graph.Add(DefineDynamicImplementationDetailTypes, userDefinedTypes, arrays);

        return graph.Build();
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
}
