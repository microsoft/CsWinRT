// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;

namespace WindowsRuntime.Generator;

/// <summary>
/// Common surface implemented by every per-tool arguments record.
/// </summary>
internal interface IGeneratorArgs
{
    /// <summary>
    /// Gets the cancellation token for the generator invocation.
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    /// Gets the directory where the debug repro should be written, if requested.
    /// </summary>
    string? DebugReproDirectory { get; }
}
