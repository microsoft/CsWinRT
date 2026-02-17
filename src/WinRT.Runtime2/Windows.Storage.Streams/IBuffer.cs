// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Represents a referenced array of bytes used by byte stream read and write interfaces.
/// </summary>
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
[Guid("905A0FE0-BC53-11DF-8C49-001E4FC686DA")]
[ContractVersion(typeof(UniversalApiContract), 65536u)]
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
