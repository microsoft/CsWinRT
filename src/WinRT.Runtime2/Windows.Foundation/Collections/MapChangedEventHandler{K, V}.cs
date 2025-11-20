// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Represents the method that handles the changed event of an observable map.
/// </summary>
/// <typeparam name="K">The type of keys in the observable map.</typeparam>
/// <typeparam name="V">The type of values in the observable map.</typeparam>
/// <param name="sender">The observable map that changed.</param>
/// <param name="event">The description of the change that occurred in the map.</param>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public delegate void MapChangedEventHandler<K, V>(IObservableMap<K, V> sender, IMapChangedEventArgs<K> @event);