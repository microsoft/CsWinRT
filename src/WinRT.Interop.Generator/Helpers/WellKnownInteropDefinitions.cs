// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Factories;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known definitions to APIs from the interop assembly being produced.
/// </summary>
internal sealed class WellKnownInteropDefinitions
{
    /// <summary>
    /// The <see cref="ModuleDefinition"/> for the interop assembly being produced.
    /// </summary>
    private readonly ModuleDefinition _interopModule;

    /// <summary>
    /// Creates a new <see cref="WellKnownInteropReferences"/> instance.
    /// </summary>
    /// <param name="interopModule">The <see cref="ModuleDefinition"/> for the interop assembly being produced.</param>
    public WellKnownInteropDefinitions(ModuleDefinition interopModule)
    {
        _interopModule = interopModule;
    }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>InteropImplementationDetails</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition InteropImplementationDetails => field ??= WellKnownTypeDefinitionFactory.InteropImplementationDetails(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>RvaFields</c> type.
    /// </summary>
    /// <remarks>
    /// This type has exactly one nested type, for RVA fields of size 16 (ie. <see cref="System.Guid"/>).
    /// </remarks>
    [field: MaybeNull, AllowNull]
    public TypeDefinition RvaFields => field ??= WellKnownTypeDefinitionFactory.RvaFields(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IIDRvaDataSize=16</c> type.
    /// </summary>
    public TypeDefinition IIDRvaDataSize_16 => RvaFields.NestedTypes[0];

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IUnknownVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IUnknownVftbl => field ??= WellKnownTypeDefinitionFactory.IUnknownVftbl(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IInspectableVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IInspectableVftbl => field ??= WellKnownTypeDefinitionFactory.IInspectableVftbl(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition DelegateVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateVftbl(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateReferenceVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition DelegateReferenceVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateReferenceVftbl(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateInterfaceEntries</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition DelegateInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.DelegateInterfaceEntriesType(_interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairVftbl</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IKeyValuePairVftbl => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairVftbl(_interopModule.CorLibTypeFactory, _interopModule.DefaultImporter);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairInterfaceEntries</c> type.
    /// </summary>
    [field: MaybeNull, AllowNull]
    public TypeDefinition IKeyValuePairInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairInterfaceEntriesType(_interopModule.DefaultImporter);
}
