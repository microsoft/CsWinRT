// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Public API for generating C# Windows Runtime projections from <c>.winmd</c> metadata.
/// 
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
            throw new ArgumentException("At least one input metadata path must be provided.", nameof(options));
        }
        if (string.IsNullOrEmpty(options.OutputFolder))
        {
            throw new ArgumentException("Output folder must be provided.", nameof(options));
        }

        // Configure global settings
        Settings settings = new()
        {
            Verbose = options.Verbose,
            Component = options.Component,
            Internal = options.Internal,
            Embedded = options.Embedded,
            PublicEnums = options.PublicEnums,
            PublicExclusiveTo = options.PublicExclusiveTo,
            IdicExclusiveTo = options.IdicExclusiveTo,
            ReferenceProjection = options.ReferenceProjection,
            OutputFolder = Path.GetFullPath(options.OutputFolder),
        };

        foreach (string p in options.InputPaths) { _ = settings.Input.Add(p); }
        foreach (string p in options.Include) { _ = settings.Include.Add(p); }
        foreach (string p in options.Exclude) { _ = settings.Exclude.Add(p); }
        foreach (string p in options.AdditionExclude) { _ = settings.AdditionExclude.Add(p); }

        settings.Filter = new TypeFilter(settings.Include, settings.Exclude);
        settings.AdditionFilter = new TypeFilter(settings.Include, settings.AdditionExclude);

        _ = Directory.CreateDirectory(settings.OutputFolder);

        // Load metadata
        MetadataCache cache = MetadataCache.Load(settings.Input);

        // Run the generator
        ProjectionGenerator generator = new(settings, cache, options.CancellationToken);
        generator.Run();
    }
}
