// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Per-property state captured while walking the static members of a runtime class so that the
/// static getter and setter can be reconciled into a single static C# property declaration.
/// </summary>
internal sealed class StaticPropertyAccessorState
{
    /// <summary>Gets or sets whether a static getter accessor has been seen for this property.</summary>
    public bool HasGetter;

    /// <summary>Gets or sets whether a static setter accessor has been seen for this property.</summary>
    public bool HasSetter;

    /// <summary>Gets or sets the projected C# type text of the property (for the unified getter+setter declaration).</summary>
    public string PropTypeText = string.Empty;

    /// <summary>Gets or sets the ABI Methods class name used by the getter dispatch.</summary>
    public string GetterAbiClass = string.Empty;

    /// <summary>Gets or sets the field name of the <c>_objRef_</c> the getter dispatches through.</summary>
    public string GetterObjRef = string.Empty;

    /// <summary>Gets or sets the ABI Methods class name used by the setter dispatch.</summary>
    public string SetterAbiClass = string.Empty;

    /// <summary>Gets or sets the field name of the <c>_objRef_</c> the setter dispatches through.</summary>
    public string SetterObjRef = string.Empty;

    /// <summary>
    /// Gets or sets the platform-attribute string for the getter (emitted before the property when
    /// both accessors share a platform; otherwise per-accessor).
    /// </summary>
    public string GetterPlatformAttribute = string.Empty;

    /// <summary>
    /// Gets or sets the platform-attribute string for the setter (emitted before the property when
    /// both accessors share a platform; otherwise per-accessor).
    /// </summary>
    public string SetterPlatformAttribute = string.Empty;
}
