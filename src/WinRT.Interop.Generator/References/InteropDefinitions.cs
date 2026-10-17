// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Models;
using WindowsRuntime.InteropGenerator.Resolvers;

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
    /// The generated IID getters indexed by their unique names.
    /// </summary>
    private readonly Dictionary<Utf8String, MethodDefinition> _interfaceIIDMethods = [];

    /// <summary>
    /// Parameterized IIDs shared by marshalling builders and metadata map emission.
    /// </summary>
    private readonly ConcurrentDictionary<string, Guid> _interfaceIids = new(StringComparer.Ordinal);

    /// <summary>
    /// The map of generated COM interface entries types for user-defined types.
    /// </summary>
    private readonly ConcurrentDictionary<int, TypeDefinition> _userDefinedInterfaceEntries;

    /// <summary>
    /// The map of generated COM interface entries types for SZ array types (with a Windows Runtime type for their element type).
    /// </summary>
    private readonly ConcurrentDictionary<int, TypeDefinition> _szArrayInterfaceEntries;

    /// <summary>
    /// Creates a new <see cref="InteropReferences"/> instance.
    /// </summary>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="windowsRuntimeSdkProjectionModule">The <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly for the Windows SDK (i.e. <c>WinRT.Sdk.Projection.dll</c>).</param>
    /// <param name="windowsRuntimeSdkXamlProjectionModule">The <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly for the Windows SDK XAML types (i.e. <c>WinRT.Sdk.Xaml.Projection.dll</c>).</param>
    /// <param name="windowsRuntimeProjectionModule">The <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly (i.e. <c>WinRT.Projection.dll</c>).</param>
    /// <param name="windowsRuntimeComponentModule">The <see cref="ModuleDefinition"/> for the Windows Runtime component assembly (i.e. <c>WinRT.Component.dll</c>).</param>
    /// <param name="enableDefaultCustomPropertyProviderSupport">Whether generated CCWs should expose the default custom property provider.</param>
    public InteropDefinitions(
        InteropReferences interopReferences,
        ModuleDefinition windowsRuntimeSdkProjectionModule,
        ModuleDefinition? windowsRuntimeSdkXamlProjectionModule,
        ModuleDefinition? windowsRuntimeProjectionModule,
        ModuleDefinition? windowsRuntimeComponentModule,
        bool enableDefaultCustomPropertyProviderSupport)
    {
        _interopReferences = interopReferences;
        _userDefinedInterfaceEntries = [];
        _szArrayInterfaceEntries = [];

        WindowsRuntimeSdkProjectionModule = windowsRuntimeSdkProjectionModule;
        WindowsRuntimeSdkXamlProjectionModule = windowsRuntimeSdkXamlProjectionModule;
        WindowsRuntimeProjectionModule = windowsRuntimeProjectionModule;
        WindowsRuntimeComponentModule = windowsRuntimeComponentModule;
        EnableDefaultCustomPropertyProviderSupport = enableDefaultCustomPropertyProviderSupport;
    }

    /// <summary>
    /// Gets whether generated CCWs should expose the default custom property provider.
    /// </summary>
    public bool EnableDefaultCustomPropertyProviderSupport { get; }

    /// <summary>
    /// Gets the <see cref="AsmResolver.DotNet.RuntimeContext"/> currently in use.
    /// </summary>
    public RuntimeContext RuntimeContext => _interopReferences.RuntimeContext;

    /// <summary>
    /// Gets a previously computed IID or computes it using the existing signature algorithm.
    /// </summary>
    /// <param name="signature">The Windows Runtime parameterized signature.</param>
    /// <param name="factory">The shared IID computation callback.</param>
    /// <returns>The IID associated with the signature.</returns>
    public Guid GetOrCreateIID(string signature, Func<string, Guid> factory)
    {
        return _interfaceIids.GetOrAdd(signature, factory);
    }

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly for the Windows SDK (i.e. <c>WinRT.Sdk.Projection.dll</c>).
    /// </summary>
    public ModuleDefinition WindowsRuntimeSdkProjectionModule { get; }

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly for the Windows SDK XAML types (i.e. <c>WinRT.Sdk.Xaml.Projection.dll</c>).
    /// </summary>
    public ModuleDefinition? WindowsRuntimeSdkXamlProjectionModule { get; }

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtime projection assembly (i.e. <c>WinRT.Projection.dll</c>).
    /// </summary>
    public ModuleDefinition? WindowsRuntimeProjectionModule { get; }

    /// <summary>
    /// Gets the <see cref="ModuleDefinition"/> for the Windows Runtime component assembly (i.e. <c>WinRT.Component.dll</c>).
    /// </summary>
    public ModuleDefinition? WindowsRuntimeComponentModule { get; }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IgnoresAccessChecksToAttribute</c> type.
    /// </summary>
    public TypeDefinition IgnoresAccessChecksToAttribute => field ??= WellKnownTypeDefinitionFactory.IgnoresAccessChecksToAttribute(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>RvaFields</c> type.
    /// </summary>
    /// <remarks>
    /// This type has exactly one nested type, for RVA fields of size 16 (ie. <see cref="System.Guid"/>).
    /// </remarks>
    public TypeDefinition RvaFields => field ??= WellKnownTypeDefinitionFactory.RvaFields(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IIDRvaDataSize=16</c> type.
    /// </summary>
    public TypeDefinition IIDRvaDataSize_16 => RvaFields.NestedTypes[0];

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>InterfaceIIDs</c> type.
    /// </summary>
    /// <remarks>
    /// Use <see cref="AddInterfaceIID"/> and <see cref="GetInterfaceIIDMethod"/> to add and look up IIDs,
    /// keeping the generated members and their lookup index in sync.
    /// </remarks>
    public TypeDefinition InterfaceIIDs => field ??= WellKnownTypeDefinitionFactory.InterfaceIIDs(_interopReferences);

    /// <summary>
    /// Adds an IID's generated members and indexes its getter for subsequent lookup.
    /// </summary>
    /// <param name="iidRvaField">The field containing the IID bytes.</param>
    /// <param name="getIidMethod">The IID getter.</param>
    /// <param name="iidProperty">The IID property.</param>
    public void AddInterfaceIID(FieldDefinition iidRvaField, MethodDefinition getIidMethod, PropertyDefinition iidProperty)
    {
        // Name mangling (e.g. '.' to '-') can map distinct input names to the same getter name.
        // Reject collisions rather than emitting ambiguous metadata and resolving the wrong interface IID.
        if (!_interfaceIIDMethods.TryAdd(getIidMethod.Name!, getIidMethod))
        {
            throw WellKnownInteropExceptions.DuplicateGeneratedInterfaceIID(getIidMethod.Name!.ToString());
        }

        RvaFields.Fields.Add(iidRvaField);
        InterfaceIIDs.Methods.Add(getIidMethod);
        InterfaceIIDs.Properties.Add(iidProperty);
    }

    /// <summary>
    /// Gets the generated IID getter with the specified name.
    /// </summary>
    /// <param name="name">The getter name.</param>
    /// <returns>The generated IID getter.</returns>
    public MethodDefinition GetInterfaceIIDMethod(Utf8String name)
    {
        return _interfaceIIDMethods[name];
    }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IUnknownVftbl</c> type.
    /// </summary>
    public TypeDefinition IUnknownVftbl => field ??= WellKnownTypeDefinitionFactory.IUnknownVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IInspectableVftbl</c> type.
    /// </summary>
    public TypeDefinition IInspectableVftbl => field ??= WellKnownTypeDefinitionFactory.IInspectableVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateVftbl</c> type.
    /// </summary>
    public TypeDefinition DelegateVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateReferenceVftbl</c> type.
    /// </summary>
    public TypeDefinition DelegateReferenceVftbl => field ??= WellKnownTypeDefinitionFactory.DelegateReferenceVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>DelegateInterfaceEntries</c> type.
    /// </summary>
    public TypeDefinition DelegateInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.DelegateInterfaceEntriesType(_interopReferences, EnableDefaultCustomPropertyProviderSupport);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IEnumerator1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IEnumerator1Vftbl => field ??= WellKnownTypeDefinitionFactory.IEnumerator1Vftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IEnumerable1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IEnumerable1Vftbl => field ??= WellKnownTypeDefinitionFactory.IEnumerable1Vftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReadOnlyList1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IReadOnlyList1Vftbl => field ??= WellKnownTypeDefinitionFactory.IReadOnlyList1Vftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IList1Vftbl</c> type.
    /// </summary>
    public TypeDefinition IList1Vftbl => field ??= WellKnownTypeDefinitionFactory.IList1Vftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReadOnlyDictionary2Vftbl</c> type.
    /// </summary>
    public TypeDefinition IReadOnlyDictionary2Vftbl => field ??= WellKnownTypeDefinitionFactory.IReadOnlyDictionary2Vftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IDictionary2Vftbl</c> type.
    /// </summary>
    public TypeDefinition IDictionary2Vftbl => field ??= WellKnownTypeDefinitionFactory.IDictionary2Vftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairVftbl</c> type.
    /// </summary>
    public TypeDefinition IKeyValuePairVftbl => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IKeyValuePairInterfaceEntries</c> type.
    /// </summary>
    public TypeDefinition IKeyValuePairInterfaceEntries => field ??= WellKnownTypeDefinitionFactory.IKeyValuePairInterfaceEntriesType(_interopReferences, EnableDefaultCustomPropertyProviderSupport);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IMapChangedEventArgsVftbl</c> type.
    /// </summary>
    public TypeDefinition IMapChangedEventArgsVftbl => field ??= WellKnownTypeDefinitionFactory.IMapChangedEventArgsVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IObservableVectorVftbl</c> type.
    /// </summary>
    public TypeDefinition IObservableVectorVftbl => field ??= WellKnownTypeDefinitionFactory.IObservableVectorVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IObservableMapVftbl</c> type.
    /// </summary>
    public TypeDefinition IObservableMapVftbl => field ??= WellKnownTypeDefinitionFactory.IObservableMapVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IAsyncActionWithProgressVftbl</c> type.
    /// </summary>
    public TypeDefinition IAsyncActionWithProgressVftbl => field ??= WellKnownTypeDefinitionFactory.IAsyncActionWithProgressVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IAsyncOperationVftbl</c> type.
    /// </summary>
    public TypeDefinition IAsyncOperationVftbl => field ??= WellKnownTypeDefinitionFactory.IAsyncOperationVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IAsyncOperationWithProgressVftbl</c> type.
    /// </summary>
    public TypeDefinition IAsyncOperationWithProgressVftbl => field ??= WellKnownTypeDefinitionFactory.IAsyncOperationWithProgressVftbl(_interopReferences);

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the <c>IReferenceArrayVftbl</c> type.
    /// </summary>
    public TypeDefinition IReferenceArrayVftbl => field ??= WellKnownTypeDefinitionFactory.ReferenceArrayVftbl(_interopReferences);

    /// <summary>
    /// Enumerates all necessary COM interface entries types for user-defined types.
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
    /// <param name="vtableTypes">The interfaces explicitly implemented by the user-defined type.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    /// <remarks>
    /// A layout without additional entries requires an explicit <c>IStringable</c> implementation.
    /// Types implementing only <c>IMarshal</c> are not exposed by Windows Runtime type discovery.
    /// </remarks>
    public TypeDefinition UserDefinedInterfaceEntries(int numberOfEntries, TypeSignatureEquatableSet vtableTypes)
    {
        // Validate each request before consulting the count-keyed cache, including cache hits.
        // Only an explicit implementation selected for the reserved slot permits a built-in-only layout.
        if (numberOfEntries != InteropInterfaceEntriesResolver.NumberOfNativeComInterfaceEntries ||
            !InteropInterfaceEntriesResolver.TryGetUserDefinedIStringableInterfaceImplementation(vtableTypes, _interopReferences, out _))
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numberOfEntries, InteropInterfaceEntriesResolver.NumberOfNativeComInterfaceEntries + 1);
        }

        return _userDefinedInterfaceEntries.GetOrAdd(
            key: numberOfEntries,
            valueFactory: numberOfEntries => WellKnownTypeDefinitionFactory.UserDefinedInterfaceEntriesType(numberOfEntries, _interopReferences));
    }

    /// <summary>
    /// Enumerates all necessary COM interface entries types for SZ array types.
    /// </summary>
    /// <returns>The sequence of all necessary COM interface entries types.</returns>
    /// <remarks>
    /// This method must be called after all necessary calls to <see cref="SzArrayInterfaceEntries"/>.
    /// </remarks>
    public IEnumerable<TypeDefinition> EnumerateSzArrayInterfaceEntriesTypes()
    {
        return _szArrayInterfaceEntries.Values;
    }

    /// <summary>
    /// Gets the <see cref="TypeDefinition"/> for the COM interface entries type for SZ array types with the specified number of entries.
    /// </summary>
    /// <param name="numberOfEntries">The number of COM interface entries to generate in the type.</param>
    /// <returns>The resulting <see cref="TypeDefinition"/> instance.</returns>
    public TypeDefinition SzArrayInterfaceEntries(int numberOfEntries)
    {
        return _szArrayInterfaceEntries.GetOrAdd(
            key: numberOfEntries,
            valueFactory: numberOfEntries => WellKnownTypeDefinitionFactory.SzArrayInterfaceEntriesType(numberOfEntries, _interopReferences));
    }
}