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

    /// <summary>
    /// Gets the assembly version stamped onto every authored .winmd, matching the WinRT convention
    /// of <c>255.255.255.255</c> (the "unbound" version used by all Windows Runtime metadata).
    /// </summary>
    public static Version AssemblyVersion { get; } = new(0xFF, 0xFF, 0xFF, 0xFF);
}