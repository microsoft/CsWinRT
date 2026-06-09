// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;

namespace WindowsRuntime.GeneratorCli;

/// <summary>
/// Common surface implemented by every per-tool args record (e.g. <c>ImplGeneratorArgs</c>,
/// <c>InteropGeneratorArgs</c>) so the shared <see cref="GeneratorHost"/> entry-point scaffold
/// can dispatch through a couple of well-known properties.
/// </summary>
internal interface IGeneratorArgs
{
    /// <summary>
    /// Gets the token for the operation.
    /// </summary>
    CancellationToken Token { get; }

    /// <summary>
    /// Gets the directory where the debug repro should be written, if requested.
    /// </summary>
    string? DebugReproDirectory { get; }
}
