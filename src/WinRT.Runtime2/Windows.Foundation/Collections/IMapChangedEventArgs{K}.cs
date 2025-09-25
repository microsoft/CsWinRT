// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Provides data for the changed event of a map collection.
/// </summary>
/// <typeparam name="K">The type of keys in the map.</typeparam>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
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
