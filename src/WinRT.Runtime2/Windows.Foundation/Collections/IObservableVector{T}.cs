// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Notifies listeners of changes to the vector.
/// </summary>
/// <typeparam name="T"></typeparam>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
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
