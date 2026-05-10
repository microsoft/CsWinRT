// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Metadata;
namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Per-emission context bundling all state that is shared by the projection writers when
/// emitting a single projection (settings + metadata cache + the active namespace + scoped
/// emission-mode flags).
/// </summary>
/// <remarks>
/// <para>
/// Replaces the implicit state previously held on <c>TypeWriter</c> (which mixed indented-text
/// emission with WinRT-specific state). (Replaces the static <c>_cacheRef</c> field that the 2.x design used.)
/// </para>
/// <para>
/// The two emission-mode flags (<see cref="InAbiNamespace"/> and <see cref="InAbiImplNamespace"/>)
/// are exposed as read-only properties; callers must enter/leave them via the scoped
/// <see cref="EnterAbiNamespace"/> / <see cref="EnterAbiImplNamespace"/> <see cref="IDisposable"/>
/// helpers. This eliminates the "did I forget to reset?" failure mode of the legacy mutable
/// flags on <c>TypeWriter</c>.
/// </para>
/// </remarks>
internal sealed class ProjectionEmitContext
{
    /// <summary>Initializes a new <see cref="ProjectionEmitContext"/>.</summary>
    /// <param name="settings">The active projection settings.</param>
    /// <param name="cache">The metadata cache for the current generation.</param>
    /// <param name="currentNamespace">The namespace currently being emitted (or <see cref="string.Empty"/> when not in a per-namespace pass).</param>
    public ProjectionEmitContext(Settings settings, MetadataCache cache, string currentNamespace)
    {
        Settings = settings;
        Cache = cache;
        CurrentNamespace = currentNamespace;
    }

    /// <summary>Gets the active projection settings.</summary>
    public Settings Settings { get; }

    /// <summary>Gets the metadata cache for the current generation.</summary>
    public MetadataCache Cache { get; }

    /// <summary>Gets the namespace currently being emitted, or <see cref="string.Empty"/> when not in a per-namespace pass.</summary>
    public string CurrentNamespace { get; }

    /// <summary>Gets a value indicating whether the writer is currently inside an ABI namespace block.</summary>
    public bool InAbiNamespace { get; private set; }

    /// <summary>Gets a value indicating whether the writer is currently inside an ABI.Impl namespace block.</summary>
    public bool InAbiImplNamespace { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether platform-attribute computation should suppress
    /// platforms that are less than or equal to <see cref="Platform"/>. Used to apply class-scope
    /// platform suppression so member-level <c>[SupportedOSPlatform]</c> attributes don't repeat
    /// information already on the enclosing type.
    /// </summary>
    public bool CheckPlatform { get; set; }

    /// <summary>Gets or sets the active platform string for the platform-attribute suppression mode.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Enters the ABI namespace mode. Returns an <see cref="IDisposable"/> token that resets the
    /// mode on dispose. Use as <c>using (context.EnterAbiNamespace()) { ... }</c>.
    /// </summary>
    /// <returns>The scope token.</returns>
    public AbiNamespaceScope EnterAbiNamespace()
    {
        InAbiNamespace = true;
        return new AbiNamespaceScope(this);
    }

    /// <summary>
    /// Enters the ABI.Impl namespace mode. Returns an <see cref="IDisposable"/> token that resets
    /// the mode on dispose. Use as <c>using (context.EnterAbiImplNamespace()) { ... }</c>.
    /// </summary>
    /// <returns>The scope token.</returns>
    public AbiImplNamespaceScope EnterAbiImplNamespace()
    {
        InAbiImplNamespace = true;
        return new AbiImplNamespaceScope(this);
    }

    /// <summary>Scope token for <see cref="EnterAbiNamespace"/>.</summary>
    public struct AbiNamespaceScope : IDisposable
    {
        private ProjectionEmitContext? _context;

        internal AbiNamespaceScope(ProjectionEmitContext context) { _context = context; }

        /// <summary>Resets the ABI namespace mode.</summary>
        public void Dispose()
        {
            _context?.SetInAbiNamespace(false);
            _context = null;
        }
    }

    /// <summary>Scope token for <see cref="EnterAbiImplNamespace"/>.</summary>
    public struct AbiImplNamespaceScope : IDisposable
    {
        private ProjectionEmitContext? _context;

        internal AbiImplNamespaceScope(ProjectionEmitContext context) { _context = context; }

        /// <summary>Resets the ABI.Impl namespace mode.</summary>
        public void Dispose()
        {
            _context?.SetInAbiImplNamespace(false);
            _context = null;
        }
    }

    /// <summary>
    /// Sets the <see cref="InAbiNamespace"/> mode flag without returning a scope. Used by the
    /// legacy <c>TypeWriter</c> passthrough (which opens/closes the ABI namespace as separate
    /// imperative calls). New code should use <see cref="EnterAbiNamespace"/> instead.
    /// </summary>
    /// <param name="value">The new value of <see cref="InAbiNamespace"/>.</param>
    internal void SetInAbiNamespace(bool value) => InAbiNamespace = value;

    /// <summary>
    /// Sets the <see cref="InAbiImplNamespace"/> mode flag without returning a scope. Used by the
    /// legacy <c>TypeWriter</c> passthrough. New code should use
    /// <see cref="EnterAbiImplNamespace"/> instead.
    /// </summary>
    /// <param name="value">The new value of <see cref="InAbiImplNamespace"/>.</param>
    internal void SetInAbiImplNamespace(bool value) => InAbiImplNamespace = value;
}