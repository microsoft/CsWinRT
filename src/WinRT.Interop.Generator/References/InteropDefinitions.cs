// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Factories;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known definitions to APIs from the interop assembly being produced.
/// </summary>
internal sealed class InteropDefinitions
{
    /// <summary>
    /// The <see cref="InteropReferences"/> instance to use.
    /// </summary>
    private readonly InteropReferences _interopReferences;

    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the interop assembly being produced.
    /// </summary>
    private readonly ModuleDefinition _interopModule;

    /// <summary>
    /// The map of generated COM interface entries types for user-defined types.
    /// </summary>
    private readonly ConcurrentDictionary<int, TypeDefinition> _userDefinedInterfaceEntries;

    /// <summary>
    /// Creates a new <see cref="InteropReferences"/> instance.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="interopModule">The <see cref="ModuleDefinition"/> for the interop assembly being produced.</param>
    public InteropDefinitions(InteropReferences interopReferences, ModuleDefinition interopModule)
    {
        _interopReferences = interopReferences;
        _interopModule = interopModule;
        _userDefinedInterfaceEntries = [];
    }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IgnoresAccessChecksToAttribute</c> type.
    /// </summary>
    public TypeDefinition IgnoresAccessChecksToAttribute => field ??= WellKnownTypeDefinitionFactory.IgnoresAccessChecksToAttribute(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>RvaFields</c> type.
    /// </summary>
    /// <remarks>
    /// This type has exactly one nested type, for RVA fields of size 16 (ie. <see cref="System.Guid"/>).
    /// </remarks>
    public TypeDefinition RvaFields => field ??= WellKnownTypeDefinitionFactory.RvaFields(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IIDRvaDataSize=16</c> type.
    /// </summary>
    public TypeDefinition IIDRvaDataSize_16 => RvaFields.NestedTypes[0];

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>InterfaceIIDs</c> type.
    /// </summary>
    public TypeDefinition InterfaceIIDs => field ??= WellKnownTypeDefinitionFactory.InterfaceIIDs(_interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IUnknownVftbl</c> type.
    /// </summary>
    public TypeDefinition IUnknownVftbl => field ??= WellKnownTypeDefinitionFactory.IUnknownVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IInspectableVftbl</c> type.
    /// </summary>
    public TypeDefinition IInspectableVftbl => field ??= WellKnownTypeDefinitionFactory.IInspectableVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateVftbl</c> type.
    /// </summary>
    public TypeDefinition DelegateVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateReferenceVftbl</c> type.
    /// </summary>
    public TypeDefinition DelegateReferenceVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateReferenceVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateInterfaceEntries</c> type.
    /// </summary>
    public TypeDefinition DelegateInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.DelegateInterfaceEntriesType(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IEnumerator1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IEnumerator1Vftbl => field ??= WellKnownTypeDefinitionFactory.IEnumerator1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IEnumerable1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IEnumerable1Vftbl => field ??= WellKnownTypeDefinitionFactory.IEnumerable1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReadOnlyList1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IReadOnlyList1Vftbl => field ??= WellKnownTypeDefinitionFactory.IReadOnlyList1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IList1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IList1Vftbl => field ??= WellKnownTypeDefinitionFactory.IList1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReadOnlyDictionary2Vftbl</c> type.
    /// </summary>
    public TypeDefinition IReadOnlyDictionary2Vftbl => field ??= WellKnownTypeDefinitionFactory.IReadOnlyDictionary2Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IDictionary2Vftbl</c> type.
    /// </summary>
    public TypeDefinition IDictionary2Vftbl => field ??= WellKnownTypeDefinitionFactory.IDictionary2Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairVftbl</c> type.
    /// </summary>
    public TypeDefinition IKeyValuePairVftbl => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairInterfaceEntries</c> type.
    /// </summary>
    public TypeDefinition IKeyValuePairInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairInterfaceEntriesType(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IMapChangedEventArgsVftbl</c> type.
    /// </summary>
    public TypeDefinition IMapChangedEventArgsVftbl => field ??= WellKnownTypeDefinitionFactory.IMapChangedEventArgsVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IObservableVectorVftbl</c> type.
    /// </summary>
    public TypeDefinition IObservableVectorVftbl => field ??= WellKnownTypeDefinitionFactory.IObservableVectorVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IObservableMapVftbl</c> type.
    /// </summary>
    public TypeDefinition IObservableMapVftbl => field ??= WellKnownTypeDefinitionFactory.IObservableMapVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IAsyncActionWithProgressVftbl</c> type.
    /// </summary>
    public TypeDefinition IAsyncActionWithProgressVftbl => field ??= WellKnownTypeDefinitionFactory.IAsyncActionWithProgressVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IAsyncOperationVftbl</c> type.
    /// </summary>
    public TypeDefinition IAsyncOperationVftbl => field ??= WellKnownTypeDefinitionFactory.IAsyncOperationVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IAsyncOperationWithProgressVftbl</c> type.
    /// </summary>
    public TypeDefinition IAsyncOperationWithProgressVftbl => field ??= WellKnownTypeDefinitionFactory.IAsyncOperationWithProgressVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReferenceArrayVftbl</c> type.
    /// </summary>
    public TypeDefinition IReferenceArrayVftbl => field ??= WellKnownTypeDefinitionFactory.ReferenceArrayVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReferenceArrayInterfaceEntries</c> type.
    /// </summary>
    public TypeDefinition IReferenceArrayInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.ReferenceArrayInterfaceEntriesType(_interopReferences, _interopModule);

    /// <summary>
    /// Enumerates all necessary COM interface entries types.
    /// </summary>
    /// <returns>The sequence of all necessary COM interface entries types.</returns>
    /// <remarks>
    /// This method must be called after all necessary calls to <see cref="UserDefinedInterfaceEntries"/>.
    /// </remarks>
    public IEnumerable<TypeDefinition> EnumerateUserDefinedInterfaceEntriesTypes()
    {
        return _userDefinedInterfaceEntries.Values;
    }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the COM interface entries type for user-defined types with the specified number of entries.
    /// </summary>
    /// <param name="numberOfEntries">The number of COM interface entries to generate in the type.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public TypeDefinition UserDefinedInterfaceEntries(int numberOfEntries)
    {
        return _userDefinedInterfaceEntries.GetOrAdd(
            key: numberOfEntries,
            valueFactory: numberOfEntries => WellKnownTypeDefinitionFactory.UserDefinedInterfaceEntriesType(numberOfEntries, _interopReferences, _interopModule));
    }
}
