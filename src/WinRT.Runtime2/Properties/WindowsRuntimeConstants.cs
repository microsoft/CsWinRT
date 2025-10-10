// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime;

/// <summary>
/// Internal constants for various scenarios.
/// </summary>
internal static class WindowsRuntimeConstants
{
    /// <summary>
    /// A message for private implementation detail types.
    /// </summary>
    public const string PrivateImplementationDetailObsoleteMessage =
        "This type or method is a private implementation detail, and it's only meant to be consumed by generated projections (produced by 'cswinrt.exe') " +
        "and by generated interop code (produced by 'cswinrtgen.exe'). Private implementation detail types are not considered part of the versioned " +
        "API surface, and they are ignored when determining the assembly version following semantic versioning. Types might be modified or removed " +
        "across any version change for 'WinRT.Runtime.dll', and using them in user code is undefined behavior and not supported.";
}
