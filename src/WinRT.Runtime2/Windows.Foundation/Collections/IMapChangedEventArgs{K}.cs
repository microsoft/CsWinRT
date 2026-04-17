// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Provides data for the changed event of a map collection.
/// </summary>
/// <typeparam name="K">The type of keys in the map.</typeparam>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
[Guid("9939F4DF-050A-4C0F-AA60-77075F9C4777")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
public interface IMapChangedEventArgs<K>
{
    /// <summary>
    /// Gets the type of change in the map.
    /// </summary>
    CollectionChange CollectionChange { get; }

    /// <summary>
    /// Gets the key of the item that changed.
    /// </summary>
    K Key { get; }
}