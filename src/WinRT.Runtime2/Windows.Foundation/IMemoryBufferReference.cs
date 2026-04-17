// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
using Windows.Foundation.Metadata;
#endif
using WindowsRuntime;

namespace Windows.Foundation;

/// <summary>
/// Represents a reference to an <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybuffer"><c>IMemoryBuffer</c></see> object.
/// </summary>
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
#endif
[Guid("FBC4DD29-245B-11E4-AF98-689423260CF8")]
#if WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[ContractVersion(typeof(UniversalApiContract), 65536u)]
#endif
public interface IMemoryBufferReference : IDisposable
{
    /// <summary>
    /// Gets the size of the memory buffer in bytes.
    /// </summary>
    uint Capacity { get; }

    /// <summary>
    /// Occurs when <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybuffer.close"><c>IMemoryBuffer.Close</c></see>
    /// has been called, but before this <see cref="IMemoryBufferReference"/> instance has been closed.
    /// </summary>
    /// <remarks>
    /// This event gives one last chance to access the <see cref="IMemoryBufferReference"/> before it is gone.
    /// </remarks>
    event EventHandler<IMemoryBufferReference, object> Closed;
}
