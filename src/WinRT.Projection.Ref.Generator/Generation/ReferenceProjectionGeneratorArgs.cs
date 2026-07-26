// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.Attributes;

namespace WindowsRuntime.ReferenceProjectionGenerator.Generation;

/// <summary>
/// Input parameters for <see cref="ReferenceProjectionGenerator"/>.
/// </summary>
internal sealed class ReferenceProjectionGeneratorArgs : IGeneratorArgs
{
    /// <summary>Gets the input <c>.winmd</c> paths (files, directories to recursively scan, or special
    /// tokens like <c>"local"</c>, <c>"sdk"</c>, <c>"sdk+"</c>, or a version like <c>"10.0.26100.0"</c>).</summary>
    [CommandLineArgumentName("--input-paths")]
    public required string[] InputPaths { get; init; }

    /// <summary>Gets the directory where the generated <c>.cs</c> files will be placed.</summary>
    [CommandLineArgumentName("--output-directory")]
    public required string OutputDirectory { get; init; }

    /// <summary>Gets the target framework being built for (must start with <c>net10.0</c>).</summary>
    [CommandLineArgumentName("--target-framework")]
    public required string TargetFramework { get; init; }

    /// <summary>Gets the namespace prefixes to include in the projection.</summary>
    [CommandLineArgumentName("--include-namespaces")]
    public string[] IncludeNamespaces { get; init; } = [];

    /// <summary>Gets the namespace prefixes to exclude from the projection.</summary>
    [CommandLineArgumentName("--exclude-namespaces")]
    public string[] ExcludeNamespaces { get; init; } = [];

    /// <summary>Gets the namespace prefixes to exclude from the projection additions.</summary>
    [CommandLineArgumentName("--addition-exclude-namespaces")]
    public string[] AdditionExcludeNamespaces { get; init; } = [];

    /// <summary>Gets whether verbose progress logging should be enabled.</summary>
    [CommandLineArgumentName("--verbose")]
    public bool Verbose { get; init; }

    /// <summary>Gets whether to generate a Windows Runtime component projection.</summary>
    [CommandLineArgumentName("--component")]
    public bool Component { get; init; }

    /// <summary>Gets whether to make exclusive-to interfaces public in the projection.</summary>
    [CommandLineArgumentName("--public-exclusive-to")]
    public bool PublicExclusiveTo { get; init; }

    /// <summary>Gets whether to generate abstract base classes so runtime classes in the input
    /// metadata can be authored (implemented) in C#.</summary>
    [CommandLineArgumentName("--author-exclusive-to")]
    public bool AuthorExclusiveToInterfaces { get; init; }

    /// <summary>Gets whether exclusive-to interfaces should support <c>IDynamicInterfaceCastable</c>.</summary>
    [CommandLineArgumentName("--idic-exclusive-to")]
    public bool IdicExclusiveTo { get; init; }

    /// <summary>Gets whether to generate a projection to be used as a reference assembly.</summary>
    [CommandLineArgumentName("--reference-projection")]
    public bool ReferenceProjection { get; init; }

    /// <inheritdoc/>
    public required CancellationToken Token { get; init; }

    /// <inheritdoc/>
    [CommandLineArgumentName("--debug-repro-directory")]
    public string? DebugReproDirectory { get; init; }
}


