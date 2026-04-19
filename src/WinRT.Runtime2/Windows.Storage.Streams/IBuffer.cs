// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation;
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Represents a referenced array of bytes used by byte stream read and write interfaces.
/// </summary>
[Guid("905A0FE0-BC53-11DF-8C49-001E4FC686DA")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(UniversalApiContract), 65536u)]
#elif WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
#endif
public interface IBuffer
{
    /// <summary>
    /// Gets the maximum number of bytes that the buffer can hold.
    /// </summary>
    uint Capacity { get; }

    /// <summary>
    /// Gets the number of bytes currently in use in the buffer.
    /// </summary>
    uint Length { get; set; }
}
