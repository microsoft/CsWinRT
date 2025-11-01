// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Models;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Global state tracking type for <see cref="InteropGenerator"/>, specifically for the discovery phase.
/// </summary>
internal sealed class InteropGeneratorDiscoveryState
{
    /// <summary>Backing field for <see cref="ModuleDefinitions"/>.</summary>
    private readonly ConcurrentDictionary<string, ModuleDefinition> _moduleDefinitions = [];

    /// <summary>Backing field for <see cref="TypeHierarchyEntries"/>.</summary>
    private readonly ConcurrentDictionary<string, string> _typeHierarchyEntries = [];

    /// <summary>Backing field for <see cref="IEnumerator1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ienumerator1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IEnumerable1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ienumerable1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IList1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ilist1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IReadOnlyList1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ireadOnlyList1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IDictionary2Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _idictionary2Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IReadOnlyDictionary2Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _ireadOnlyDictionary2Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IObservableVector1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _iobservableVector1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IObservableMap2Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _iobservableMap2Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IMapChangedEventArgs1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _imapChangedEventArgs1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IAsyncActionWithProgress1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _iasyncActionWithProgress1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IAsyncOperation1Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _iasyncOperation1Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="IAsyncOperationWithProgress2Types"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _iasyncOperationWithProgress2Types = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="GenericDelegateTypes"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _genericDelegateTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="KeyValuePairTypes"/>.</summary>
    private readonly ConcurrentDictionary<GenericInstanceTypeSignature, byte> _keyValuePairTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="SzArrayTypes"/>.</summary>
    private readonly ConcurrentDictionary<SzArrayTypeSignature, byte> _szArrayTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="UserDefinedTypes"/>.</summary>
    private readonly ConcurrentDictionary<TypeSignature, TypeSignatureEquatableSet> _userDefinedTypes = new(SignatureComparer.IgnoreVersion);

    /// <summary>Backing field for <see cref="UserDefinedVtableTypes"/>.</summary>
    private readonly ConcurrentDictionary<TypeSignatureEquatableSet, TypeSignatureEquatableSet> _userDefinedVtableTypes = [];

    /// <summary>
    /// Indicates whether the current state is readonly.
    /// </summary>
    private volatile bool _isReadOnly;

    /// <summary>
    /// Indicates whether any of the loaded modules reference the WinRT runtime .dll version 2.
    /// </summary>
    private volatile bool _hasWinRTRuntimeDllVersion2References;

    /// <summary>
    /// Gets the assembly resolver to use for loading assemblies.
    /// </summary>
    public required IAssemblyResolver AssemblyResolver { get; init; }

    /// <summary>
    /// Gets the loaded modules.
    /// </summary>
    public IReadOnlyDictionary<string, ModuleDefinition> ModuleDefinitions => _moduleDefinitions;

    /// <summary>
    /// Gets the type hierarchy entries.
    /// </summary>
    public IReadOnlyDictionary<string, string> TypeHierarchyEntries => _typeHierarchyEntries;

    /// <summary>
    /// Gets all <see cref="IEnumerator{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IEnumerator1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ienumerator1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IEnumerable{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IEnumerable1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ienumerable1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IList{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IList1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ilist1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IReadOnlyList{T}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IReadOnlyList1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ireadOnlyList1Types.Keys;

    /// <summary>
    /// Gets all <see cref="IDictionary{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IDictionary2Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_idictionary2Types.Keys;

    /// <summary>
    /// Gets all <see cref="IReadOnlyDictionary{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IReadOnlyDictionary2Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_ireadOnlyDictionary2Types.Keys;

    /// <summary>
    /// Gets all <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IObservableVector1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_iobservableVector1Types.Keys;

    /// <summary>
    /// Gets all <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IObservableMap2Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_iobservableMap2Types.Keys;

    /// <summary>
    /// Gets all <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IMapChangedEventArgs1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_imapChangedEventArgs1Types.Keys;

    /// <summary>
    /// Gets all <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IAsyncActionWithProgress1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_iasyncActionWithProgress1Types.Keys;

    /// <summary>
    /// Gets all <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IAsyncOperation1Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_iasyncOperation1Types.Keys;

    /// <summary>
    /// Gets all <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> IAsyncOperationWithProgress2Types => (IReadOnlyCollection<GenericInstanceTypeSignature>)_iasyncOperationWithProgress2Types.Keys;

    /// <summary>
    /// Gets all generic delegate types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> GenericDelegateTypes => (IReadOnlyCollection<GenericInstanceTypeSignature>)_genericDelegateTypes.Keys;

    /// <summary>
    /// Gets all <see cref="KeyValuePair{TKey, TValue}"/> types.
    /// </summary>
    public IReadOnlyCollection<GenericInstanceTypeSignature> KeyValuePairTypes => (IReadOnlyCollection<GenericInstanceTypeSignature>)_keyValuePairTypes.Keys;

    /// <summary>
    /// Gets all SZ array types.
    /// </summary>
    public IReadOnlyCollection<SzArrayTypeSignature> SzArrayTypes => (IReadOnlyCollection<SzArrayTypeSignature>)_szArrayTypes.Keys;

    /// <summary>
    /// Gets all user-defined types.
    /// </summary>
    public IReadOnlyCollection<TypeSignature> UserDefinedTypes => (IReadOnlyCollection<TypeSignature>)_userDefinedTypes.Keys;

    /// <summary>
    /// Gets all user-defined types and their vtable types.
    /// </summary>
    public IReadOnlyDictionary<TypeSignature, TypeSignatureEquatableSet> UserDefinedAndVtableTypes => _userDefinedTypes;

    /// <summary>
    /// Gets all user-defined vtable types (for each user-defined type).
    /// </summary>
    public IReadOnlyCollection<TypeSignatureEquatableSet> UserDefinedVtableTypes => (IReadOnlyCollection<TypeSignatureEquatableSet>)_userDefinedVtableTypes.Keys;

    /// <summary>
    /// Gets whether any of the loaded modules reference the WinRT runtime .dll version 2.
    /// </summary>
    public bool HasWinRTRuntimeDllVersion2References => _hasWinRTRuntimeDllVersion2References;

    /// <summary>
    /// Tracks a loaded module definition.
    /// </summary>
    /// <param name="path">The assembly path.</param>
    /// <param name="module">The loaded module.</param>
    public void TrackModuleDefinition(string path, ModuleDefinition module)
    {
        ThrowIfReadOnly();

        _ = _moduleDefinitions.TryAdd(path, module);
    }

    /// <summary>
    /// Tracks a pair of runtime class names for the type hierarchy.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name to use as key.</param>
    /// <param name="baseRuntimeClassName">The runtime class name of the base type.</param>
    public void TrackTypeHierarchyEntry(string runtimeClassName, string baseRuntimeClassName)
    {
        ThrowIfReadOnly();

        _ = _typeHierarchyEntries.TryAdd(runtimeClassName, baseRuntimeClassName);
    }

    /// <summary>
    /// Tracks a <see cref="IEnumerator{T}"/> type.
    /// </summary>
    /// <param name="enumeratorType">The <see cref="IEnumerator{T}"/> type.</param>
    public void TrackIEnumerator1Type(GenericInstanceTypeSignature enumeratorType)
    {
        ThrowIfReadOnly();

        _ = _ienumerator1Types.TryAdd(enumeratorType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IEnumerable{T}"/> type.
    /// </summary>
    /// <param name="enumerableType">The <see cref="IEnumerable{T}"/> type.</param>
    public void TrackIEnumerable1Type(GenericInstanceTypeSignature enumerableType)
    {
        ThrowIfReadOnly();

        _ = _ienumerable1Types.TryAdd(enumerableType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IList{T}"/> type.
    /// </summary>
    /// <param name="listType">The <see cref="IList{T}"/> type.</param>
    public void TrackIList1Type(GenericInstanceTypeSignature listType)
    {
        ThrowIfReadOnly();

        _ = _ilist1Types.TryAdd(listType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IReadOnlyList{T}"/> type.
    /// </summary>
    /// <param name="listType">The <see cref="IReadOnlyList{T}"/> type.</param>
    public void TrackIReadOnlyList1Type(GenericInstanceTypeSignature listType)
    {
        ThrowIfReadOnly();

        _ = _ireadOnlyList1Types.TryAdd(listType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IDictionary{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="dictionaryType">The <see cref="IDictionary{TKey, TValue}"/> type.</param>
    public void TrackIDictionary2Type(GenericInstanceTypeSignature dictionaryType)
    {
        ThrowIfReadOnly();

        _ = _idictionary2Types.TryAdd(dictionaryType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="IReadOnlyDictionary{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="dictionaryType">The <see cref="IReadOnlyDictionary{TKey, TValue}"/> type.</param>
    public void TrackIReadOnlyDictionary2Type(GenericInstanceTypeSignature dictionaryType)
    {
        ThrowIfReadOnly();

        _ = _ireadOnlyDictionary2Types.TryAdd(dictionaryType, 0);
    }

    /// <summary>
    /// Tracks a <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> type.
    /// </summary>
    /// <param name="vectorType">The <c>Windows.Foundation.Collections.IObservableVector&lt;T&gt;</c> type.</param>
    public void TrackIObservableVector1Type(GenericInstanceTypeSignature vectorType)
    {
        ThrowIfReadOnly();

        _ = _iobservableVector1Types.TryAdd(vectorType, 0);
    }

    /// <summary>
    /// Tracks a <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> type.
    /// </summary>
    /// <param name="mapType">The <c>Windows.Foundation.Collections.IObservableMap&lt;K, V&gt;</c> type.</param>
    public void TrackIObservableMap2Type(GenericInstanceTypeSignature mapType)
    {
        ThrowIfReadOnly();

        _ = _iobservableMap2Types.TryAdd(mapType, 0);
    }

    /// <summary>
    /// Tracks a <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> type.
    /// </summary>
    /// <param name="argsType">The <c>Windows.Foundation.Collections.IMapChangedEventArgs&lt;K&gt;</c> type.</param>
    public void TrackIMapChangedEventArgs1Type(GenericInstanceTypeSignature argsType)
    {
        ThrowIfReadOnly();

        _ = _imapChangedEventArgs1Types.TryAdd(argsType, 0);
    }

    /// <summary>
    /// Tracks a <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> type.
    /// </summary>
    /// <param name="actionType">The <c>Windows.Foundation.IAsyncActionWithProgress&lt;TProgress&gt;</c> type.</param>
    public void TrackIAsyncActionWithProgress1Type(GenericInstanceTypeSignature actionType)
    {
        ThrowIfReadOnly();

        _ = _iasyncActionWithProgress1Types.TryAdd(actionType, 0);
    }

    /// <summary>
    /// Tracks a <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c> type.
    /// </summary>
    /// <param name="operationType">The <c>Windows.Foundation.IAsyncOperation&lt;TResult&gt;</c> type.</param>
    public void TrackIAsyncOperation1Type(GenericInstanceTypeSignature operationType)
    {
        ThrowIfReadOnly();

        _ = _iasyncOperation1Types.TryAdd(operationType, 0);
    }

    /// <summary>
    /// Tracks a <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> type.
    /// </summary>
    /// <param name="operationType">The <c>Windows.Foundation.IAsyncOperationWithProgress&lt;TResult, TProgress&gt;</c> type.</param>
    public void TrackIAsyncOperationWithProgress2Type(GenericInstanceTypeSignature operationType)
    {
        ThrowIfReadOnly();

        _ = _iasyncOperationWithProgress2Types.TryAdd(operationType, 0);
    }

    /// <summary>
    /// Tracks a generic delegate type.
    /// </summary>
    /// <param name="delegateType">The delegate type.</param>
    public void TrackGenericDelegateType(GenericInstanceTypeSignature delegateType)
    {
        ThrowIfReadOnly();

        _ = _genericDelegateTypes.TryAdd(delegateType, 0);
    }

    /// <summary>
    /// Tracks a <see cref="KeyValuePair{TKey, TValue}"/> type.
    /// </summary>
    /// <param name="keyValuePairType">The <see cref="KeyValuePair{TKey, TValue}"/> type.</param>
    public void TrackKeyValuePairType(GenericInstanceTypeSignature keyValuePairType)
    {
        ThrowIfReadOnly();

        _ = _keyValuePairTypes.TryAdd(keyValuePairType, 0);
    }

    /// <summary>
    /// Tracks a SZ array type.
    /// </summary>
    /// <param name="szArrayType">The SZ array type.</param>
    public void TrackSzArrayType(SzArrayTypeSignature szArrayType)
    {
        ThrowIfReadOnly();

        _ = _szArrayTypes.TryAdd(szArrayType, 0);
    }

    /// <summary>
    /// Tracks a user-defined type.
    /// </summary>
    /// <param name="userDefinedType">The user-defined type.</param>
    /// <param name="vtableTypes">The vtable types for <paramref name="userDefinedType"/>.</param>
    public void TrackUserDefinedType(TypeSignature userDefinedType, TypeSignatureEquatableSet vtableTypes)
    {
        ThrowIfReadOnly();

        // We want to de-duplicate all vtables across all user-defined types we need to generate CCW code for.
        // This is because a significant portion of these vtables will be identical (it will be the case for
        // all user-defined types that implement the same set of projected Windows Runtime interfaces). So here
        // we first add this set to our map, and reuse the cached instance for this set (if it exists) later.
        TypeSignatureEquatableSet cachedVtableTypes = _userDefinedVtableTypes.GetOrAdd(vtableTypes, vtableTypes);

        _ = _userDefinedTypes.TryAdd(userDefinedType, cachedVtableTypes);
    }

    /// <summary>
    /// Marks the current state as readonly.
    /// </summary>
    public void MakeReadOnly()
    {
        _isReadOnly = true;
    }

    /// <summary>
    /// Marks the state as having references to the WinRT runtime .dll version 2.
    /// </summary>
    public void MarkWinRTRuntimeDllVersion2References()
    {
        ThrowIfReadOnly();

        _hasWinRTRuntimeDllVersion2References = true;
    }

    /// <summary>
    /// Throws if <see cref="MakeReadOnly"/> was called.
    /// </summary>
    private void ThrowIfReadOnly()
    {
        if (_isReadOnly)
        {
            throw WellKnownInteropExceptions.StateChangeAfterMakeReadOnly();
        }
    }
}
