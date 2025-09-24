// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Describes the action that causes a change to a collection.
/// </summary>
/// <remarks>
/// There is only one notification per type of change to a collection. For example, if an item is inserted,
/// then only a notification for an insertion is sent to a listener that is subscribed to receive change
/// notifications. Use the <see cref="IVectorChangedEventArgs.Index"/> property or the
/// <see cref="IMapChangedEventArgs{K}.Key"/> property to determine the location of the change.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.collectionchange"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Collections.CollectionChange>")]
[ContractVersion(typeof(FoundationContract), 65536u)]
[CollectionChangeComWrappersMarshaller]
public enum CollectionChange
{
    /// <summary>
    /// The collection is changed.
    /// </summary>
    Reset = 0,

    /// <summary>
    /// An item is added to the collection.
    /// </summary>
    ItemInserted = 1,

    /// <summary>
    /// An item is removed from the collection.
    /// </summary>
    ItemRemoved = 2,

    /// <summary>
    /// An item is changed in the collection.
    /// </summary>
    ItemChanged = 3
}
