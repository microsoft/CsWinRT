// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// State produced by the processing phase of <see cref="ProjectionGenerator"/>.
/// </summary>
/// <param name="sourcesFolder">The path to the folder where sources will be generated.</param>
/// <param name="referencesWithoutProjections">The reference assembly paths excluding projection assemblies.</param>
/// <param name="writerOptions">The options to pass to <see cref="global::WindowsRuntime.ProjectionWriter.ProjectionWriter.Run"/>.</param>
/// <param name="hasTypesToProject">Whether any types were found to project.</param>
internal sealed class ProjectionGeneratorProcessingState(
    string sourcesFolder,
    string[] referencesWithoutProjections,
    ProjectionWriterOptions writerOptions,
    bool hasTypesToProject = true)
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
    /// Gets the options used to invoke <see cref="global::WindowsRuntime.ProjectionWriter.ProjectionWriter.Run"/>.
    /// </summary>
    public ProjectionWriterOptions WriterOptions { get; } = writerOptions;

    /// <summary>
    /// Gets whether any types were found to project. When <c>false</c>, the source generation
    /// and emit phases should be skipped (no DLL will be produced).
    /// </summary>
    public bool HasTypesToProject { get; } = hasTypesToProject;
}

