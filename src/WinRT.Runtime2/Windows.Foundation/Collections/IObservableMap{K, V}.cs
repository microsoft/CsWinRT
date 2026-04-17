// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Notifies listeners of dynamic changes to a map, such as when items are added or removed.
/// </summary>
/// <typeparam name="K">The type of keys in the observable map.</typeparam>
/// <typeparam name="V">The type of values in the observable map.</typeparam>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
[Guid("65DF2BF5-BF39-41B5-AEBC-5A9D865E472B")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
public interface IObservableMap<K, V> : IDictionary<K, V>, ICollection<KeyValuePair<K, V>>, IEnumerable<KeyValuePair<K, V>>, IEnumerable
{
    /// <summary>
    /// Occurs when the map changes.
    /// </summary>
    /// <remarks>
    /// The event handler receives an <see cref="IMapChangedEventArgs{K}"/> object that contains data that describes the event.
    /// </remarks>
    event MapChangedEventHandler<K, V> MapChanged;
}