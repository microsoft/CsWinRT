// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="InteropGenerator"/>.
/// </summary>
internal sealed class InteropGeneratorArgs
{
    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='referencePath']/node()"/></summary>
    public required string[] ReferencePath { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='assemblyPath']/node()"/></summary>
    public required string AssemblyPath { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='outputDirectory']/node()"/></summary>
    public required string OutputDirectory { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='token']/node()"/></summary>
    public required CancellationToken Token { get; init; }
}
