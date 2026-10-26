// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// The independent emit families and their completion barriers.
/// </summary>
internal enum EmissionStep
{
    /// <summary>The type hierarchy lookup emitted by <see cref="InteropGenerator.DefineTypeHierarchyLookup"/>.</summary>
    TypeHierarchy,

    /// <summary>Generic delegate marshalling emitted by <see cref="InteropGenerator.DefineGenericDelegateTypes"/>.</summary>
    Delegates,

    /// <summary>Generic enumerator marshalling emitted by <see cref="InteropGenerator.DefineIEnumeratorTypes"/>.</summary>
    Enumerators,

    /// <summary>Generic enumerable marshalling emitted by <see cref="InteropGenerator.DefineIEnumerableTypes"/>.</summary>
    Enumerables,

    /// <summary>Read-only list marshalling emitted by <see cref="InteropGenerator.DefineIReadOnlyListTypes"/>.</summary>
    ReadOnlyLists,

    /// <summary>Mutable list marshalling emitted by <see cref="InteropGenerator.DefineIListTypes"/>.</summary>
    Lists,

    /// <summary>Read-only dictionary marshalling emitted by <see cref="InteropGenerator.DefineIReadOnlyDictionaryTypes"/>.</summary>
    ReadOnlyDictionaries,

    /// <summary>Mutable dictionary marshalling emitted by <see cref="InteropGenerator.DefineIDictionaryTypes"/>.</summary>
    Dictionaries,

    /// <summary>Key-value pair marshalling emitted by <see cref="InteropGenerator.DefineKeyValuePairTypes"/>.</summary>
    KeyValuePairs,

    /// <summary>Map change event argument marshalling emitted by <see cref="InteropGenerator.DefineIMapChangedEventArgsTypes"/>.</summary>
    MapChangedArguments,

    /// <summary>Observable vector marshalling emitted by <see cref="InteropGenerator.DefineIObservableVectorTypes"/>.</summary>
    ObservableVectors,

    /// <summary>Observable map marshalling emitted by <see cref="InteropGenerator.DefineIObservableMapTypes"/>.</summary>
    ObservableMaps,

    /// <summary>Async action with progress marshalling emitted by <see cref="InteropGenerator.DefineIAsyncActionWithProgressTypes"/>.</summary>
    AsyncActions,

    /// <summary>Async operation marshalling emitted by <see cref="InteropGenerator.DefineIAsyncOperationTypes"/>.</summary>
    AsyncOperations,

    /// <summary>Async operation with progress marshalling emitted by <see cref="InteropGenerator.DefineIAsyncOperationWithProgressTypes"/>.</summary>
    AsyncProgressOperations,

    /// <summary>Read-only key-value pair collection marshalling emitted by <see cref="InteropGenerator.DefineIReadOnlyCollectionKeyValuePair2Types"/>.</summary>
    ReadOnlyCollections,

    /// <summary>Mutable key-value pair collection marshalling emitted by <see cref="InteropGenerator.DefineICollectionKeyValuePair2Types"/>.</summary>
    Collections,

    /// <summary>SZ array marshalling emitted by <see cref="InteropGenerator.DefineSzArrayTypes"/>.</summary>
    Arrays,

    /// <summary>Marshalling stub IL rewritten by <see cref="InteropGenerator.RewriteMethodDefinitions"/>.</summary>
    MethodRewrites,

    /// <summary>User-defined type CCW support emitted by <see cref="InteropGenerator.DefineUserDefinedTypes"/>.</summary>
    UserDefinedTypes,

    /// <summary>Shared interface entry types emitted by <see cref="InteropGenerator.DefineDynamicImplementationDetailTypes"/>.</summary>
    DynamicDefinitions,

    /// <summary>Custom-mapped type map entries emitted by <see cref="InteropGenerator.DefineDynamicCustomMappedTypeMapEntries"/>.</summary>
    CustomTypeMaps,

    /// <summary>IID lookup map entries emitted by <see cref="InteropGenerator.DefineWindowsRuntimeIidTypeMapEntries"/>.</summary>
    IidTypeMaps,

    /// <summary>Access check bypass attributes emitted by <see cref="InteropGenerator.DefineIgnoresAccessChecksToAttributes"/>.</summary>
    AccessCheckAttributes,

    /// <summary>Assembly metadata attributes emitted by <see cref="InteropGenerator.EmitMetadataAssemblyAttributes"/>.</summary>
    MetadataAttributes
}
