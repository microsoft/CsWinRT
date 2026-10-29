// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Carries the shared invocation state for emission steps without changing its ownership or mutability.
/// </summary>
/// <param name="args">The invocation arguments.</param>
/// <param name="discoveryState">The frozen discovery state.</param>
/// <param name="inputs">The canonicalized discovery inputs.</param>
/// <param name="emitState">The shared definition lookup state.</param>
/// <param name="definitions">The shared default definitions.</param>
/// <param name="references">The shared references.</param>
/// <param name="module">The output module.</param>
internal sealed class InteropGeneratorEmissionContext(
    InteropGeneratorArgs args,
    InteropGeneratorDiscoveryState discoveryState,
    InteropGeneratorEmitInputs inputs,
    InteropGeneratorEmitState emitState,
    InteropDefinitions definitions,
    InteropReferences references,
    ModuleDefinition module)
{
    /// <summary>Gets the invocation arguments.</summary>
    public InteropGeneratorArgs Args { get; } = args;

    /// <summary>Gets the frozen discovery state.</summary>
    public InteropGeneratorDiscoveryState DiscoveryState { get; } = discoveryState;

    /// <summary>Gets the canonicalized discovery inputs.</summary>
    public InteropGeneratorEmitInputs Inputs { get; } = inputs;

    /// <summary>Gets the shared definition lookup state.</summary>
    public InteropGeneratorEmitState EmitState { get; } = emitState;

    /// <summary>Gets the shared default definitions.</summary>
    public InteropDefinitions Definitions { get; } = definitions;

    /// <summary>Gets the shared references.</summary>
    public InteropReferences References { get; } = references;

    /// <summary>Gets the output module.</summary>
    public ModuleDefinition Module { get; } = module;
}
