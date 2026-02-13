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
[WindowsRuntimeMetadata("Windows.Foundation.UniversalApiContract")]
[WindowsRuntimeClassName("Windows.Foundation.IReference`1<Windows.Storage.Streams.InputStreamOptions>")]
[WindowsRuntimeReferenceType(typeof(InputStreamOptions?))]
[SupportedOSPlatform("Windows10.0.10240.0")]
[ContractVersion(typeof(UniversalApiContract), 65536u)]
[ABI.Windows.Storage.Streams.InputStreamOptionsComWrappersMarshaller]
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
