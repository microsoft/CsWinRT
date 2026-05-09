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
    public bool HasGetter;
    public bool HasSetter;
    public string PropTypeText = string.Empty;
    public string Access = "public ";
    public string MethodSpec = string.Empty;
    public string GetterAbiClass = string.Empty;
    public string GetterObjRef = string.Empty;
    public string SetterAbiClass = string.Empty;
    public string SetterObjRef = string.Empty;
    public string Name = string.Empty;
    public bool GetterIsGeneric;
    public bool SetterIsGeneric;
    public string GetterGenericInteropType = string.Empty;
    public string GetterGenericAccessorName = string.Empty;
    public string GetterPropTypeText = string.Empty;
    public string SetterGenericInteropType = string.Empty;
    public string SetterGenericAccessorName = string.Empty;
    public string SetterPropTypeText = string.Empty;
    // True if this property comes from an Overridable interface (needs explicit interface impl).
    public bool IsOverridable;
    // The originating interface (used to qualify the explicit interface impl).
    public ITypeDefOrRef? OverridableInterface;
    // Per-accessor platform attribute strings from the originating interface's [ContractVersion],
    // emitted before the property in ref mode. Mirrors C++ getter_platform/setter_platform
    // tracking in / 4323/4330. When both match, emit at the property
    // level only; when they differ (getter and setter come from different interfaces with
    // different platforms), emit per-accessor.
    public string GetterPlatformAttribute = string.Empty;
    public string SetterPlatformAttribute = string.Empty;
}
