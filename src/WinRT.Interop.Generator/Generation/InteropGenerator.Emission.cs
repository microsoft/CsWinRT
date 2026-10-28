// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

#pragma warning disable IDE0008 // Use 'var' to keep graph node declarations readable

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

        var delegates = graph.Add(DefineGenericDelegateTypes);
        var enumerators = graph.Add(DefineIEnumeratorTypes);
        var enumerables = graph.Add(DefineIEnumerableTypes, enumerators);
        var readOnlyLists = graph.Add(DefineIReadOnlyListTypes, enumerables);
        var lists = graph.Add(DefineIListTypes, enumerables);
        var readOnlyDictionaries = graph.Add(DefineIReadOnlyDictionaryTypes, enumerables);
        var dictionaries = graph.Add(DefineIDictionaryTypes, enumerables);
        var keyValuePairs = graph.Add(DefineKeyValuePairTypes);
        var mapChangedArguments = graph.Add(DefineIMapChangedEventArgsTypes);
        var observableVectors = graph.Add(DefineIObservableVectorTypes, delegates, lists);
        var observableMaps = graph.Add(DefineIObservableMapTypes, delegates, dictionaries);
        var asyncActions = graph.Add(DefineIAsyncActionWithProgressTypes, delegates);
        var asyncOperations = graph.Add(DefineIAsyncOperationTypes, delegates);
        var asyncProgressOperations = graph.Add(DefineIAsyncOperationWithProgressTypes, delegates);
        var readOnlyCollections = graph.Add(DefineIReadOnlyCollectionKeyValuePair2Types, readOnlyLists, readOnlyDictionaries);
        var collections = graph.Add(DefineICollectionKeyValuePair2Types, lists, dictionaries);

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

        var arrays = graph.Add(DefineSzArrayTypes, genericFamilies);
        var userDefinedTypes = graph.Add(DefineUserDefinedTypes, genericFamilies);

        _ = graph.Add(RewriteMethodDefinitions, arrays);
        _ = graph.Add(DefineDynamicImplementationDetailTypes, userDefinedTypes, arrays);

        return graph.Build();
    }
}
