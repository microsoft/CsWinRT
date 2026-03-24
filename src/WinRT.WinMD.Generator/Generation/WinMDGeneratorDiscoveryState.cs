// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// The state produced by the discovery phase of the WinMD generator.
/// </summary>
internal sealed class WinMDGeneratorDiscoveryState
{
    /// <summary>
    /// Gets the loaded input module.
    /// </summary>
    public required ModuleDefinition InputModule { get; init; }

    /// <summary>
    /// Gets the assembly name from the input module.
    /// </summary>
    public required string AssemblyName { get; init; }

    /// <summary>
    /// Gets the public types discovered in the input assembly.
    /// </summary>
    public required IReadOnlyList<TypeDefinition> PublicTypes { get; init; }
}