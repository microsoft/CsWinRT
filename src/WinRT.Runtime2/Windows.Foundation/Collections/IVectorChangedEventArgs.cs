// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Foundation.Collections;

/// <summary>
/// Provides data for the changed event of a vector.
/// </summary>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.FoundationContract")]
#endif
[Guid("575933DF-34FE-4480-AF15-07691F3D5D9B")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(FoundationContract), 65536u)]
#endif
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