// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Provides data for the changed event of a vector.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
[ContractVersion(typeof(FoundationContract), 65536u)]
public interface IVectorChangedEventArgs
{
    /// <summary>
    /// Gets the type of change in the vector.
    /// </summary>
    CollectionChange CollectionChange { get; }

    /// <summary>
    /// Gets the position where the change occurred in the vector.
    /// </summary>
    uint Index { get; }
}
