// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;

namespace WindowsRuntime;

/// <summary>
/// Indicates that a given type is a Windows Runtime type (either a projected type, or a proxy type for a custom-mapped type).
/// </summary>
/// <remarks>
/// This is a marker attribute carrying no data: it only signals that the decorated type participates in Windows Runtime
/// marshalling. The mapping from a projected type to its source Windows Runtime metadata file (.winmd) is instead recorded
/// on the centralized <c>ABI.WindowsRuntimeMetadataTypes</c> lookup type (via <see cref="WindowsRuntimeMetadataAttribute"/>),
/// so that the (build-time only) metadata can be trimmed away when not needed.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = false)]
[WindowsRuntimeImplementationOnlyMember]
public sealed class WindowsRuntimeTypeAttribute : Attribute;
