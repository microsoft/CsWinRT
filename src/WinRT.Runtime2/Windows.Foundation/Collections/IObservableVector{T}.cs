// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Metadata;
using WindowsRuntime.InteropServices;

namespace Windows.Foundation.Collections;

/// <summary>
/// Notifies listeners of changes to the vector.
/// </summary>
/// <typeparam name="T">The type of elements in the observable vector.</typeparam>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public interface IObservableVector<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
{
    /// <summary>
    /// Occurs when the vector changes.
    /// </summary>
    /// <remarks>
    /// The event handler receives an <see cref="IVectorChangedEventArgs"/> object that contains data that describes the event.
    /// </remarks>
    event VectorChangedEventHandler<T> VectorChanged;
}
