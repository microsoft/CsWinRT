// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
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

        InteropGenerationWorkQueue<InteropGeneratorEmissionContext> queue = CreateEmissionQueue();

        queue.Run(context, args.GetEffectiveMaxDegreesOfParallelism(), args.Token);

        // Fixups enumerate generated types, so all emit workers must have finished
        FixupMethodDefinitions(args, module);
    }

    /// <summary>
    /// Creates the fixed emission graph using node references for all dependencies.
    /// </summary>
    /// <returns>The emission queue for this invocation.</returns>
    private static InteropGenerationWorkQueue<InteropGeneratorEmissionContext> CreateEmissionQueue()
    {
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Builder graph = new();

        _ = graph.Add(DefineTypeHierarchyLookup);
        _ = graph.Add(DefineDynamicCustomMappedTypeMapEntries);
        _ = graph.Add(DefineWindowsRuntimeIidTypeMapEntries);
        _ = graph.Add(DefineIgnoresAccessChecksToAttributes);
        _ = graph.Add(EmitMetadataAssemblyAttributes);

        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node delegates = graph.Add(DefineGenericDelegateTypes);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node enumerators = graph.Add(DefineIEnumeratorTypes);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node enumerables = graph.Add(DefineIEnumerableTypes, enumerators);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node readOnlyLists = graph.Add(DefineIReadOnlyListTypes, enumerables);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node lists = graph.Add(DefineIListTypes, enumerables);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node readOnlyDictionaries = graph.Add(DefineIReadOnlyDictionaryTypes, enumerables);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node dictionaries = graph.Add(DefineIDictionaryTypes, enumerables);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node keyValuePairs = graph.Add(DefineKeyValuePairTypes);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node mapChangedArguments = graph.Add(DefineIMapChangedEventArgsTypes);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node observableVectors = graph.Add(DefineIObservableVectorTypes, delegates, lists);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node observableMaps = graph.Add(DefineIObservableMapTypes, delegates, dictionaries);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node asyncActions = graph.Add(DefineIAsyncActionWithProgressTypes, delegates);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node asyncOperations = graph.Add(DefineIAsyncOperationTypes, delegates);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node asyncProgressOperations = graph.Add(DefineIAsyncOperationWithProgressTypes, delegates);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node readOnlyCollections = graph.Add(DefineIReadOnlyCollectionKeyValuePair2Types, readOnlyLists, readOnlyDictionaries);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node collections = graph.Add(DefineICollectionKeyValuePair2Types, lists, dictionaries);

        // Arrays and user-defined CCWs can resolve generic IID/vtable entries from any of these families. Group every
        // family into a completion barrier so all referenced definitions exist before either consumer runs.
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node[] genericFamilies =
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

        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node arrays = graph.Add(DefineSzArrayTypes, genericFamilies);
        InteropGenerationWorkQueue<InteropGeneratorEmissionContext>.Node userDefinedTypes = graph.Add(DefineUserDefinedTypes, genericFamilies);

        _ = graph.Add(RewriteMethodDefinitions, arrays);
        _ = graph.Add(DefineDynamicImplementationDetailTypes, userDefinedTypes, arrays);

        return graph.Build();
    }
}
