// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Windows.Foundation.Collections;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Describes the action that causes a change to a collection.
/// </summary>
/// <remarks>
/// There is only one notification per type of change to a collection. For example, if an item is inserted, then only a notification
/// event arguments interface in use (e.g. <see cref="IMapChangedEventArgs{K}"/>) to determine the location of the change.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.collectionchange"/>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference<Windows.Foundation.Collections.CollectionChange>")]
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
