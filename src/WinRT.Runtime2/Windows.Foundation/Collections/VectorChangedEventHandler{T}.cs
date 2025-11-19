// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Represents the method that handles the changed event of an observable vector.
/// </summary>
/// <typeparam name="T">The type of elements in the observable vector.</typeparam>
/// <param name="sender">The observable vector that changed.</param>
/// <param name="event">The description of the change that occurred in the vector.</param>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public delegate void VectorChangedEventHandler<T>(IObservableVector<T> sender, IVectorChangedEventArgs @event);