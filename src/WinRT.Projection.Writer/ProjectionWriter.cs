// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Public API for generating C# Windows Runtime projections from <c>.winmd</c> metadata.
/// <para>
/// Usage: call <see cref="Run"/> with the desired options. The tool will generate
/// <c>.cs</c> files in the specified output folder, one per Windows Runtime namespace.
/// </para>
/// </summary>
public static class ProjectionWriter
{
    /// <summary>
    /// Runs projection generation. Generates C# projections for the input WinRT metadata and writes them to the configured output folder.
    /// </summary>
    /// <param name="options">The generation options (input metadata, output folder, filters).</param>
    public static void Run(ProjectionWriterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.InputPaths == null || options.InputPaths.Count == 0)
        {
            throw WellKnownProjectionWriterExceptions.MissingInputPaths();
        }

        if (string.IsNullOrEmpty(options.OutputFolder))
        {
            throw WellKnownProjectionWriterExceptions.MissingOutputFolder();
        }

        Settings settings;

        // Translate the public options into the internal settings bag.
        try
        {
            settings = new()
            {
                Verbose = options.Verbose,
                Logger = options.Logger,
                MaxDegreesOfParallelism = options.MaxDegreesOfParallelism,
                Component = options.Component,
                PublicExclusiveTo = options.PublicExclusiveTo,
                IdicExclusiveTo = options.IdicExclusiveTo,
                ReferenceProjection = options.ReferenceProjection,
                OutputFolder = Path.GetFullPath(options.OutputFolder),
            };

            settings.Input.UnionWith(options.InputPaths);
            settings.Include.UnionWith(options.Include);
            settings.Exclude.UnionWith(options.Exclude);
            settings.AdditionExclude.UnionWith(options.AdditionExclude);

            settings.MakeReadOnly();

            _ = Directory.CreateDirectory(settings.OutputFolder);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionWriterException("parsing", e);
        }

        options.CancellationToken.ThrowIfCancellationRequested();

        MetadataCache cache;

        // Load the metadata cache from the configured input paths.
        try
        {
            cache = MetadataCache.Load(settings.Input);
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw new UnhandledProjectionWriterException("metadata-load", e);
        }

        options.CancellationToken.ThrowIfCancellationRequested();

        // Run the generator. Phase boundaries within the orchestrator wrap their own
        // discovery / emit blocks with UnhandledProjectionWriterException.
        ProjectionGenerator generator = new(settings, cache, options.CancellationToken);
        generator.Run();
    }
}
