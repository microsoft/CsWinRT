// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Builders;

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

        EmissionStep[] genericFamilies =
        [
            EmissionStep.Delegates,
            EmissionStep.Enumerators,
            EmissionStep.Enumerables,
            EmissionStep.ReadOnlyLists,
            EmissionStep.Lists,
            EmissionStep.ReadOnlyDictionaries,
            EmissionStep.Dictionaries,
            EmissionStep.KeyValuePairs,
            EmissionStep.MapChangedArguments,
            EmissionStep.ObservableVectors,
            EmissionStep.ObservableMaps,
            EmissionStep.AsyncActions,
            EmissionStep.AsyncOperations,
            EmissionStep.AsyncProgressOperations,
            EmissionStep.ReadOnlyCollections,
            EmissionStep.Collections
        ];

        InteropGenerationWorkQueue<EmissionStep, InteropGeneratorEmissionContext> queue = new(
        [
            new(EmissionStep.TypeHierarchy,
                DefineTypeHierarchyLookup),
            new(EmissionStep.Delegates,
                DefineGenericDelegateTypes),
            new(EmissionStep.Enumerators,
                DefineIEnumeratorTypes),
            new(EmissionStep.Enumerables,
                DefineIEnumerableTypes, EmissionStep.Enumerators),
            new(EmissionStep.ReadOnlyLists,
                DefineIReadOnlyListTypes, EmissionStep.Enumerables),
            new(EmissionStep.Lists,
                DefineIListTypes, EmissionStep.Enumerables),
            new(EmissionStep.ReadOnlyDictionaries,
                DefineIReadOnlyDictionaryTypes, EmissionStep.Enumerables),
            new(EmissionStep.Dictionaries,
                DefineIDictionaryTypes, EmissionStep.Enumerables),
            new(EmissionStep.KeyValuePairs,
                DefineKeyValuePairTypes),
            new(EmissionStep.MapChangedArguments,
                DefineIMapChangedEventArgsTypes),
            new(EmissionStep.ObservableVectors,
                DefineIObservableVectorTypes, EmissionStep.Delegates, EmissionStep.Lists),
            new(EmissionStep.ObservableMaps,
                DefineIObservableMapTypes, EmissionStep.Delegates, EmissionStep.Dictionaries),
            new(EmissionStep.AsyncActions,
                DefineIAsyncActionWithProgressTypes, EmissionStep.Delegates),
            new(EmissionStep.AsyncOperations,
                DefineIAsyncOperationTypes, EmissionStep.Delegates),
            new(EmissionStep.AsyncProgressOperations,
                DefineIAsyncOperationWithProgressTypes, EmissionStep.Delegates),
            new(EmissionStep.ReadOnlyCollections,
                DefineIReadOnlyCollectionKeyValuePair2Types, EmissionStep.ReadOnlyLists, EmissionStep.ReadOnlyDictionaries),
            new(EmissionStep.Collections,
                DefineICollectionKeyValuePair2Types, EmissionStep.Lists, EmissionStep.Dictionaries),
            new(EmissionStep.Arrays,
                DefineSzArrayTypes, genericFamilies),
            new(EmissionStep.MethodRewrites,
                RewriteMethodDefinitions, EmissionStep.Arrays),
            new(EmissionStep.UserDefinedTypes,
                DefineUserDefinedTypes, genericFamilies),
            new(EmissionStep.DynamicDefinitions,
                DefineDynamicImplementationDetailTypes, EmissionStep.UserDefinedTypes, EmissionStep.Arrays),
            new(EmissionStep.CustomTypeMaps,
                DefineDynamicCustomMappedTypeMapEntries),
            new(EmissionStep.IidTypeMaps,
                DefineWindowsRuntimeIidTypeMapEntries),
            new(EmissionStep.AccessCheckAttributes,
                DefineIgnoresAccessChecksToAttributes),
            new(EmissionStep.MetadataAttributes,
                EmitMetadataAssemblyAttributes)
        ]);

        queue.Run(context, args.MaxDegreesOfParallelism, args.Token);

        // Fixups enumerate generated types, so all emit workers must have finished.
        FixupMethodDefinitions(args, module);
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
