// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// The independent emit families and their completion barriers.
/// </summary>
internal enum EmissionStep
{
    TypeHierarchy,
    Delegates,
    Enumerators,
    Enumerables,
    ReadOnlyLists,
    Lists,
    ReadOnlyDictionaries,
    Dictionaries,
    KeyValuePairs,
    MapChangedArguments,
    ObservableVectors,
    ObservableMaps,
    AsyncActions,
    AsyncOperations,
    AsyncProgressOperations,
    ReadOnlyCollections,
    Collections,
    Arrays,
    MethodRewrites,
    UserDefinedTypes,
    DynamicDefinitions,
    CustomTypeMaps,
    IidTypeMaps,
    AccessCheckAttributes,
    MetadataAttributes
}
