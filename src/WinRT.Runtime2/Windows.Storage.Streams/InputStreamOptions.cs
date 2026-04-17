// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Versioning;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Specifies the read options for an input stream.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.inputstreamoptions"/>
[Flags]
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
#endif
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Storage.Streams.InputStreamOptions>")]
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[WindowsRuntimeReferenceType(typeof(InputStreamOptions?))]
#elif WINDOWS_RUNTIME_REFERENCE_ASSEMBLY
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(UniversalApiContract), 65536u)]
#endif
#if WINDOWS_RUNTIME_IMPLEMENTATION_ASSEMBLY
[ABI.Windows.Storage.Streams.InputStreamOptionsComWrappersMarshaller]
#endif
public enum InputStreamOptions : uint
{
    /// <summary>
    /// No options are specified.
    /// </summary>
    None = 0u,

    /// <summary>
    /// The asynchronous read operation completes when one or more bytes is available.
    /// </summary>
    Partial = 1u,

    /// <summary>
    /// The asynchronous read operation may optionally read ahead and prefetch additional bytes.
    /// </summary>
    ReadAhead = 2u
}
