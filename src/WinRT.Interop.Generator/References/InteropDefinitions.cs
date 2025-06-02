// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
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
    /// Creates a new <see cref="InteropReferences"/> instance.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="interopModule">The <see cref="ModuleDefinition"/> for the interop assembly being produced.</param>
    public InteropDefinitions(InteropReferences interopReferences, ModuleDefinition interopModule)
    {
        _interopReferences = interopReferences;
        _interopModule = interopModule;
    }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IgnoreAccessChecksToAttribute</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IgnoreAccessChecksToAttribute => field ??= WellKnownTypeDefinitionFactory.IgnoreAccessChecksToAttribute(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>InteropImplementationDetails</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition InteropImplementationDetails => field ??= WellKnownTypeDefinitionFactory.InteropImplementationDetails(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>RvaFields</c> type.
    /// </summary>
    /// <remarks>
    /// This type has exactly one nested type, for RVA fields of size 16 (ie. <see cref="System.Guid"/>).
    /// </remarks>
    [field: MaybeNull, AllowNull]
    public TypeDefinition RvaFields => field ??= WellKnownTypeDefinitionFactory.RvaFields(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IIDRvaDataSize=16</c> type.
    /// </summary>
    public TypeDefinition IIDRvaDataSize_16 => RvaFields.NestedTypes[0];

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>InterfaceIIDs</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition InterfaceIIDs => field ??= WellKnownTypeDefinitionFactory.InterfaceIIDs(_interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IUnknownVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IUnknownVftbl => field ??= WellKnownTypeDefinitionFactory.IUnknownVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IInspectableVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IInspectableVftbl => field ??= WellKnownTypeDefinitionFactory.IInspectableVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition DelegateVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateReferenceVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition DelegateReferenceVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateReferenceVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateInterfaceEntries</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition DelegateInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.DelegateInterfaceEntriesType(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IEnumerator1Vftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IEnumerator1Vftbl => field ??= WellKnownTypeDefinitionFactory.IEnumerator1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IEnumerable1Vftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IEnumerable1Vftbl => field ??= WellKnownTypeDefinitionFactory.IEnumerable1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReadOnlyList1Vftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IReadOnlyList1Vftbl => field ??= WellKnownTypeDefinitionFactory.IReadOnlyList1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IList1Vftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IList1Vftbl => field ??= WellKnownTypeDefinitionFactory.IList1Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReadOnlyDictionary2Vftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IReadOnlyDictionary2Vftbl => field ??= WellKnownTypeDefinitionFactory.IReadOnlyDictionary2Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IDictionary2Vftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IDictionary2Vftbl => field ??= WellKnownTypeDefinitionFactory.IDictionary2Vftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IKeyValuePairVftbl => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairVftbl(_interopReferences, _interopModule);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairInterfaceEntries</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IKeyValuePairInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairInterfaceEntriesType(_interopReferences, _interopModule);
}
