// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
    /// <summary>
    /// Runs whole emit families as soon as their prerequisites complete.
    /// </summary>
    /// <param name="args">The invocation arguments.</param>
    /// <param name="discoveryState">The frozen discovery state.</param>
    /// <param name="inputs">The canonicalized discovery inputs.</param>
    /// <param name="emitState">The shared definition lookup state.</param>
    /// <param name="definitions">The shared default definitions.</param>
    /// <param name="references">The shared references.</param>
    /// <param name="module">The output module.</param>
    private static void RunEmission(
        InteropGeneratorArgs args,
        InteropGeneratorDiscoveryState discoveryState,
        InteropGeneratorEmitInputs inputs,
        InteropGeneratorEmitState emitState,
        InteropDefinitions definitions,
        InteropReferences references,
        ModuleDefinition module)
    {
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

        InteropGenerationWorkQueue<EmissionStep> queue = new(
        [
            new(EmissionStep.TypeHierarchy,
                () => WindowsRuntimeTypeHierarchyBuilder.Lookup(discoveryState.TypeHierarchyEntries, definitions, references, module, args.Token, out _)),
            new(EmissionStep.Delegates,
                () => DefineGenericDelegateTypes(args, inputs, emitState, definitions, references, module)),
            new(EmissionStep.Enumerators,
                () => DefineIEnumeratorTypes(args, inputs, emitState, definitions, references, module)),
            new(EmissionStep.Enumerables,
                () => DefineIEnumerableTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Enumerators),
            new(EmissionStep.ReadOnlyLists,
                () => DefineIReadOnlyListTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Enumerables),
            new(EmissionStep.Lists,
                () => DefineIListTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Enumerables),
            new(EmissionStep.ReadOnlyDictionaries,
                () => DefineIReadOnlyDictionaryTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Enumerables),
            new(EmissionStep.Dictionaries,
                () => DefineIDictionaryTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Enumerables),
            new(EmissionStep.KeyValuePairs,
                () => DefineKeyValuePairTypes(args, inputs, emitState, definitions, references, module)),
            new(EmissionStep.MapChangedArguments,
                () => DefineIMapChangedEventArgsTypes(args, inputs, emitState, definitions, references, module)),
            new(EmissionStep.ObservableVectors,
                () => DefineIObservableVectorTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Delegates, EmissionStep.Lists),
            new(EmissionStep.ObservableMaps,
                () => DefineIObservableMapTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Delegates, EmissionStep.Dictionaries),
            new(EmissionStep.AsyncActions,
                () => DefineIAsyncActionWithProgressTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Delegates),
            new(EmissionStep.AsyncOperations,
                () => DefineIAsyncOperationTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Delegates),
            new(EmissionStep.AsyncProgressOperations,
                () => DefineIAsyncOperationWithProgressTypes(args, inputs, emitState, definitions, references, module), EmissionStep.Delegates),
            new(EmissionStep.ReadOnlyCollections,
                () => DefineIReadOnlyCollectionKeyValuePair2Types(args, inputs, emitState, definitions, references, module), EmissionStep.ReadOnlyLists, EmissionStep.ReadOnlyDictionaries),
            new(EmissionStep.Collections,
                () => DefineICollectionKeyValuePair2Types(args, inputs, emitState, definitions, references, module), EmissionStep.Lists, EmissionStep.Dictionaries),
            new(EmissionStep.Arrays,
                () => DefineSzArrayTypes(args, inputs, emitState, definitions, references, module), genericFamilies),
            new(EmissionStep.MethodRewrites,
                () => RewriteMethodDefinitions(args, emitState, references), EmissionStep.Arrays),
            new(EmissionStep.UserDefinedTypes,
                () => DefineUserDefinedTypes(args, inputs, emitState, definitions, references, module), genericFamilies),
            new(EmissionStep.DynamicDefinitions,
                () => DefineDynamicImplementationDetailTypes(definitions, module), EmissionStep.UserDefinedTypes, EmissionStep.Arrays),
            new(EmissionStep.CustomTypeMaps,
                () => DefineDynamicCustomMappedTypeMapEntries(args, definitions, references, module)),
            new(EmissionStep.IidTypeMaps,
                () => DefineWindowsRuntimeIidTypeMapEntries(args, discoveryState, inputs, definitions, references, module)),
            new(EmissionStep.AccessCheckAttributes,
                () => DefineIgnoresAccessChecksToAttributes(discoveryState, definitions, references, module)),
            new(EmissionStep.MetadataAttributes,
                () => EmitMetadataAssemblyAttributes(references, module))
        ]);

        queue.Run(args.MaxDegreesOfParallelism, args.Token);

        // Fixups enumerate generated types, so all emit workers must have finished.
        FixupMethodDefinitions(args, module);
    }
}
