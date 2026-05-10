// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Per-property state captured while walking the members of a runtime class so that the getter
/// and setter (which may come from different interfaces, with different platform attributes
/// and ABI Methods classes) can be reconciled into a single C# property declaration.
/// </summary>
internal sealed class PropertyAccessorState
{
    /// <summary>
    /// Gets or sets whether a getter accessor has been seen for this property.
    /// </summary>
    public bool HasGetter { get; set; }

    /// <summary>
    /// Gets or sets whether a setter accessor has been seen for this property.
    /// </summary>
    public bool HasSetter { get; set; }

    /// <summary>
    /// Gets or sets the projected C# type text of the property (for the unified getter+setter declaration).
    /// </summary>
    public string PropTypeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the C# accessibility modifier text (e.g. <c>"public "</c>).
    /// </summary>
    public string Access { get; set; } = "public ";

    /// <summary>
    /// Gets or sets the method-spec modifier text (e.g. <c>"override "</c>, <c>"new "</c>).
    /// </summary>
    public string MethodSpec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ABI Methods class name used by the getter dispatch.
    /// </summary>
    public string GetterAbiClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name of the <c>_objRef_</c> the getter dispatches through.
    /// </summary>
    public string GetterObjRef { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ABI Methods class name used by the setter dispatch.
    /// </summary>
    public string SetterAbiClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name of the <c>_objRef_</c> the setter dispatches through.
    /// </summary>
    public string SetterObjRef { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the getter dispatches through a generic-instantiation marshaller.
    /// </summary>
    public bool GetterIsGeneric { get; set; }

    /// <summary>
    /// Gets or sets whether the setter dispatches through a generic-instantiation marshaller.
    /// </summary>
    public bool SetterIsGeneric { get; set; }

    /// <summary>
    /// Gets or sets the interop type name string used by the getter's <c>UnsafeAccessor</c>.
    /// </summary>
    public string GetterGenericInteropType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accessor name used for the getter's <c>UnsafeAccessor</c>.
    /// </summary>
    public string GetterGenericAccessorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the projected property type text used by the getter dispatch.
    /// </summary>
    public string GetterPropTypeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the interop type name string used by the setter's <c>UnsafeAccessor</c>.
    /// </summary>
    public string SetterGenericInteropType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accessor name used for the setter's <c>UnsafeAccessor</c>.
    /// </summary>
    public string SetterGenericAccessorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the projected property type text used by the setter dispatch.
    /// </summary>
    public string SetterPropTypeText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this property comes from an <c>[Overridable]</c> interface (and so
    /// needs an explicit interface implementation).
    /// </summary>
    public bool IsOverridable { get; set; }

    /// <summary>
    /// Gets or sets the originating interface (used to qualify the explicit interface implementation
    /// when <see cref="IsOverridable"/> is set).
    /// </summary>
    public ITypeDefOrRef? OverridableInterface { get; set; }

    /// <summary>
    /// Gets or sets the platform-attribute string for the getter (in reference-projection mode,
    /// emitted before the property when both accessors share a platform; otherwise per-accessor).
    /// </summary>
    public string GetterPlatformAttribute { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform-attribute string for the setter (in reference-projection mode,
    /// emitted before the property when both accessors share a platform; otherwise per-accessor).
    /// </summary>
    public string SetterPlatformAttribute { get; set; } = string.Empty;
}
