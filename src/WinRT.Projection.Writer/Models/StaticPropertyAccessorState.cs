// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Models;

/// <summary>
/// Per-property state captured while walking the static members of a runtime class so that the
/// static getter and setter can be reconciled into a single static C# property declaration.
/// </summary>
internal sealed class StaticPropertyAccessorState
{
    public bool HasGetter;
    public bool HasSetter;
    public string PropTypeText = string.Empty;
    public string GetterAbiClass = string.Empty;
    public string GetterObjRef = string.Empty;
    public string SetterAbiClass = string.Empty;
    public string SetterObjRef = string.Empty;
    // Per-accessor platform attribute strings. Mirrors C++ getter_platform/setter_platform
    // tracking in code_writers.h:3328-3349.
    public string GetterPlatformAttribute = string.Empty;
    public string SetterPlatformAttribute = string.Empty;
}
