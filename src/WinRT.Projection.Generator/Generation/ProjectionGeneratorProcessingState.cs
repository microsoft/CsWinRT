// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// State produced by the processing phase of <see cref="ProjectionGenerator"/>.
/// </summary>
/// <param name="sourcesFolder">The path to the folder where sources will be generated.</param>
/// <param name="rspFilePath">The path to the response file (kept as a debug artifact).</param>
/// <param name="referencesWithoutProjections">The reference assembly paths excluding projection assemblies.</param>
/// <param name="writerOptions">The options to pass to <see cref="Writer.ProjectionWriter.Run"/>.</param>
/// <param name="hasTypesToProject">Whether any types were found to project.</param>
internal sealed class ProjectionGeneratorProcessingState(
    string sourcesFolder,
    string rspFilePath,
    string[] referencesWithoutProjections,
    Writer.ProjectionWriterOptions writerOptions,
    bool hasTypesToProject = true)
{
    /// <summary>
    /// Gets the path to the folder where sources will be generated.
    /// </summary>
    public string SourcesFolder { get; } = sourcesFolder;

    /// <summary>
    /// Gets the path to the generated response file (kept as a debug artifact for inspection).
    /// </summary>
    public string RspFilePath { get; } = rspFilePath;

    /// <summary>
    /// Gets the reference assembly paths excluding projection assemblies.
    /// </summary>
    public string[] ReferencesWithoutProjections { get; } = referencesWithoutProjections;

    /// <summary>
    /// Gets the options used to invoke <see cref="Writer.ProjectionWriter.Run"/>.
    /// </summary>
    public Writer.ProjectionWriterOptions WriterOptions { get; } = writerOptions;

    /// <summary>
    /// Gets whether any types were found to project. When <c>false</c>, the source generation
    /// and emit phases should be skipped (no DLL will be produced).
    /// </summary>
    public bool HasTypesToProject { get; } = hasTypesToProject;
}

