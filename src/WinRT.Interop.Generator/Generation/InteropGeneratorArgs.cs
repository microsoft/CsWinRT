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
    public required string[] ReferenceAssemblyPaths { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='assemblyPath']/node()"/></summary>
    public required string OutputAssemblyPath { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='outputDirectory']/node()"/></summary>
    public required string OutputDirectory { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='useWindowsUIXamlProjections']/node()"/></summary>
    public required bool UseWindowsUIXamlProjections { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='maxDegreesOfParallelism']/node()"/></summary>
    public required int MaxDegreesOfParallelism { get; init; }

    /// <summary><inheritdoc cref="InteropGenerator.Run" path="/param[@name='token']/node()"/></summary>
    public required CancellationToken Token { get; init; }
}
