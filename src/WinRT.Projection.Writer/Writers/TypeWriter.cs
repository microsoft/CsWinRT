// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Legacy projection-writer surface inherited from the C++ port. Adds namespace context,
/// generic-parameter stack, and projection-specific begin/end helpers on top of <see cref="TextWriter"/>.
/// </summary>
/// <remarks>
/// During Pass 10 of the refactor this type acts as a passthrough that delegates the
/// WinRT-specific helpers (file header, projected/ABI namespace blocks) to extension methods
/// on <see cref="IndentedTextWriter"/> + <see cref="ProjectionEmitContext"/>. Existing callers
/// continue to use this type unchanged; migrated callers can take
/// <c>(IndentedTextWriter writer, ProjectionEmitContext context)</c> directly via
/// <see cref="TextWriter.Writer"/> + <see cref="Context"/>. Once every writer family has
/// migrated, this type will be deleted (Pass 10c).
/// </remarks>
internal sealed class TypeWriter : TextWriter
{
    /// <summary>Gets the namespace currently being emitted.</summary>
    public string CurrentNamespace { get; }

    /// <summary>Gets the active projection settings.</summary>
    public Settings Settings { get; }

    /// <summary>Gets a value indicating whether the writer is currently inside an ABI namespace block.</summary>
    public bool InAbiNamespace => Context.InAbiNamespace;

    /// <summary>Gets a value indicating whether the writer is currently inside an ABI.Impl namespace block.</summary>
    public bool InAbiImplNamespace => Context.InAbiImplNamespace;

    /// <summary>
    /// Gets or sets a value indicating whether platform-attribute computation should suppress
    /// platforms that are less than or equal to <see cref="Platform"/>. Mirrors the historical
    /// C++ writer's class-scope platform suppression mode.
    /// </summary>
    public bool CheckPlatform { get; set; }

    /// <summary>Gets or sets the active platform string for the platform-attribute suppression mode.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>Gets the stack of generic argument lists currently in scope.</summary>
    public List<object[]> GenericArgsStack { get; } = new();

    /// <summary>Gets the bundled <see cref="ProjectionEmitContext"/> for this writer.</summary>
    public ProjectionEmitContext Context { get; }

    /// <summary>
    /// Initializes a new <see cref="TypeWriter"/> for the given <paramref name="settings"/> and
    /// <paramref name="currentNamespace"/>.
    /// </summary>
    /// <param name="settings">The active projection settings.</param>
    /// <param name="currentNamespace">The namespace currently being emitted.</param>
    public TypeWriter(Settings settings, string currentNamespace)
    {
        Settings = settings;
        CurrentNamespace = currentNamespace;
        // Build the bundled context. The metadata cache is currently exposed via the static
        // CodeWriters._cacheRef; once Pass 11 eliminates that static, the cache will flow in
        // through this constructor.
        Context = new ProjectionEmitContext(settings, CodeWriters.GetMetadataCache()!, currentNamespace);
    }

    /// <summary>
    /// Initializes a new <see cref="TypeWriter"/> for the given <paramref name="context"/>.
    /// Used by migrated callers that already have a <see cref="ProjectionEmitContext"/>.
    /// </summary>
    /// <param name="context">The bundled context for this writer.</param>
    public TypeWriter(ProjectionEmitContext context)
    {
        Settings = context.Settings;
        CurrentNamespace = context.CurrentNamespace;
        Context = context;
    }

    /// <summary>
    /// Writes the standard auto-generated file header (banner + canonical <c>using</c> imports
    /// + suppression pragmas). Delegates to
    /// <see cref="IndentedTextWriterExtensions.WriteFileHeader(IndentedTextWriter, ProjectionEmitContext)"/>.
    /// </summary>
    public void WriteFileHeader()
    {
        Writer.WriteFileHeader(Context);
    }

    /// <summary>
    /// Writes the <c>namespace ...</c> opening block for the projected namespace. Delegates to
    /// <see cref="ProjectionWriterExtensions.WriteBeginProjectedNamespace(Writers.IndentedTextWriter, ProjectionEmitContext)"/>.
    /// </summary>
    public void WriteBeginProjectedNamespace()
    {
        Context.SetInAbiImplNamespace(Settings.Component);
        Writer.WriteBeginProjectedNamespace(Context);
    }

    /// <summary>Writes the closing <c>}</c> for the projected namespace.</summary>
    public void WriteEndProjectedNamespace()
    {
        Writer.WriteEndProjectedNamespace();
        Context.SetInAbiImplNamespace(false);
    }

    /// <summary>Writes the <c>namespace ABI.X</c> opening block plus its <c>CA1416</c> suppression pragma.</summary>
    public void WriteBeginAbiNamespace()
    {
        Writer.WriteBeginAbiNamespace(Context);
        Context.SetInAbiNamespace(true);
    }

    /// <summary>Writes the closing <c>}</c> + matching <c>CA1416</c> restore pragma for the ABI namespace.</summary>
    public void WriteEndAbiNamespace()
    {
        Writer.WriteEndAbiNamespace();
        Context.SetInAbiNamespace(false);
    }
}
