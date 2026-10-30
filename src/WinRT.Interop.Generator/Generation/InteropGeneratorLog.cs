// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// A versioned report of the discovered and emitted interop types.
/// </summary>
/// <param name="Schema">The report schema version.</param>
/// <param name="AssemblySizeBytes">The size of the emitted interop assembly in bytes.</param>
/// <param name="TypeHierarchy">The runtime class names and their base class names.</param>
/// <param name="GenericInstantiations">The generic types grouped by discovery category.</param>
/// <param name="UserDefinedTypes">User-defined types and their COM interfaces.</param>
/// <param name="ArrayTypes">Array types and their COM interfaces.</param>
/// <param name="GeneratedTypes">All types emitted into the interop assembly.</param>
internal sealed record InteropGeneratorLog(
    int Schema,
    long AssemblySizeBytes,
    InteropGeneratorLog.TypeHierarchyEntry[] TypeHierarchy,
    InteropGeneratorLog.GenericInstantiationsInfo GenericInstantiations,
    InteropGeneratorLog.TypeWithInterfaces[] UserDefinedTypes,
    InteropGeneratorLog.TypeWithInterfaces[] ArrayTypes,
    InteropGeneratorLog.GeneratedType[] GeneratedTypes)
{
    /// <summary>
    /// A projected runtime class and its immediate base class.
    /// </summary>
    /// <param name="Type">The runtime class name.</param>
    /// <param name="BaseType">The base runtime class name.</param>
    internal sealed record TypeHierarchyEntry(string Type, string BaseType);

    /// <summary>
    /// The constructed generic types gathered for each interop category.
    /// </summary>
    /// <param name="GenericDelegates">Discovered generic delegates.</param>
    /// <param name="Enumerators">Discovered <c>IEnumerator&lt;T&gt;</c> types.</param>
    /// <param name="Enumerables">Discovered <c>IEnumerable&lt;T&gt;</c> types.</param>
    /// <param name="Lists">Discovered <c>IList&lt;T&gt;</c> types.</param>
    /// <param name="ReadOnlyLists">Discovered <c>IReadOnlyList&lt;T&gt;</c> types.</param>
    /// <param name="Dictionaries">Discovered <c>IDictionary&lt;K, V&gt;</c> types.</param>
    /// <param name="ReadOnlyDictionaries">Discovered <c>IReadOnlyDictionary&lt;K, V&gt;</c> types.</param>
    /// <param name="ObservableVectors">Discovered observable vectors.</param>
    /// <param name="ObservableMaps">Discovered observable maps.</param>
    /// <param name="MapChangedEventArgs">Discovered map-changed event args.</param>
    /// <param name="AsyncActionsWithProgress">Discovered async actions with progress.</param>
    /// <param name="AsyncOperations">Discovered async operations.</param>
    /// <param name="AsyncOperationsWithProgress">Discovered async operations with progress.</param>
    /// <param name="KeyValuePairs">Discovered <c>KeyValuePair&lt;K, V&gt;</c> types.</param>
    internal sealed record GenericInstantiationsInfo(
        string[] GenericDelegates,
        string[] Enumerators,
        string[] Enumerables,
        string[] Lists,
        string[] ReadOnlyLists,
        string[] Dictionaries,
        string[] ReadOnlyDictionaries,
        string[] ObservableVectors,
        string[] ObservableMaps,
        string[] MapChangedEventArgs,
        string[] AsyncActionsWithProgress,
        string[] AsyncOperations,
        string[] AsyncOperationsWithProgress,
        string[] KeyValuePairs);

    /// <summary>
    /// A discovered type and the interfaces used for its COM entries.
    /// </summary>
    /// <param name="Type">The assembly-qualified type name.</param>
    /// <param name="Interfaces">The assembly-qualified interface names.</param>
    internal sealed record TypeWithInterfaces(string Type, string[] Interfaces);

    /// <summary>
    /// An emitted type and metrics describing its generated code.
    /// </summary>
    /// <param name="Name">The emitted type name.</param>
    /// <param name="MethodCount">The number of methods.</param>
    /// <param name="FieldCount">The number of fields.</param>
    /// <param name="ILInstructionCount">The number of IL instructions across its method bodies.</param>
    internal sealed record GeneratedType(string Name, int MethodCount, int FieldCount, int ILInstructionCount);
}
