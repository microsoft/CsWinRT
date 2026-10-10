// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// State produced by the processing phase of <see cref="ProjectionGenerator"/>.
/// </summary>
/// <param name="sourcesFolder">The path to the folder where sources will be generated.</param>
/// <param name="referencesWithoutProjections">The reference assembly paths excluding projection assemblies.</param>
/// <param name="writerOptions">The options to pass to <see cref="ProjectionWriter.ProjectionWriter.Run"/>.</param>
/// <param name="hasTypesToProject">Whether any types were found to project.</param>
/// <param name="componentAssemblyNames">Sorted simple names of all input <c>[WindowsRuntimeComponentAssembly]</c> references (component-mode only).</param>
/// <param name="activationFactoryAssemblyNames">Sorted simple names of input references that only contribute an activation entry point (component-mode only).</param>
internal sealed class ProjectionGeneratorProcessingState(
    string sourcesFolder,
    string[] referencesWithoutProjections,
    ProjectionWriterOptions writerOptions,
    bool hasTypesToProject = true,
    IReadOnlyList<string>? componentAssemblyNames = null,
    IReadOnlyList<string>? activationFactoryAssemblyNames = null)
{
    /// <summary>
    /// Gets the path to the folder where sources will be generated.
    /// </summary>
    public string SourcesFolder { get; } = sourcesFolder;

    /// <summary>
    /// Gets the reference assembly paths excluding projection assemblies.
    /// </summary>
    public string[] ReferencesWithoutProjections { get; } = referencesWithoutProjections;

    /// <summary>
    /// Gets the options used to invoke <see cref="ProjectionWriter.ProjectionWriter.Run"/>.
    /// </summary>
    public ProjectionWriterOptions WriterOptions { get; } = writerOptions;

    /// <summary>
    /// Gets whether any types were found to project. When <c>false</c>, the source generation
    /// and emit phases should be skipped (no DLL will be produced).
    /// </summary>
    public bool HasTypesToProject { get; } = hasTypesToProject;

    /// <summary>
    /// Gets the simple names of all input assemblies marked with
    /// <c>[WindowsRuntimeComponentAssembly]</c>. Empty unless this is a component-mode run
    /// (i.e. producing <c>WinRT.Component.dll</c>).
    /// </summary>
    public IReadOnlyList<string> ComponentAssemblyNames { get; } = componentAssemblyNames ?? [];

    /// <summary>
    /// Gets the simple names of input assemblies that only contribute an activation entry point to the merged
    /// activation chain (i.e. they implement Windows Runtime classes declared in existing metadata, so they have
    /// no <c>.winmd</c> of their own). Empty unless this is a component-mode run.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ComponentAssemblyNames"/>, whose <c>ABI.&lt;Name&gt;.ManagedExports</c> is generated into
    /// <c>WinRT.Component.dll</c> itself, these resolve to the type the source generator emitted into the assembly,
    /// whose namespace has been escaped into a valid identifier.
    /// </remarks>
    public IReadOnlyList<string> ActivationFactoryAssemblyNames { get; } = activationFactoryAssemblyNames ?? [];

    /// <summary>
    /// Gets whether any assembly takes part in the merged activation performed by <c>WinRT.Component.dll</c>.
    /// </summary>
    public bool HasMergedActivation => ComponentAssemblyNames.Count > 0 || ActivationFactoryAssemblyNames.Count > 0;
}

