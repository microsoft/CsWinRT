// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known .winmd values (constants).
/// </summary>
internal static class WinMDValues
{
    /// <summary>
    /// The runtime version for .winmd files.
    /// </summary>
    public const string RuntimeVersion = "WindowsRuntime 1.4";

    /// <summary>
    /// Gets the version of the <c>mscorlib</c> reference for .winmd files.
    /// </summary>
    public static Version MSCorLibVersion { get; } = new(0xFF, 0xFF, 0xFF, 0xFF);
}