// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="IHasCustomAttribute"/> type.
/// </summary>
internal static class IHasCustomAttributeExtensions
{
    /// <summary>
    /// Checks whether a <see cref="IHasCustomAttribute"/> represents a projected Windows Runtime type.
    /// </summary>
    /// <param name="type">The input <see cref="IHasCustomAttribute"/> instance.</param>
    /// <returns>Whether <paramref name="type"/> represents a projected Windows Runtime type.</returns>
    public static bool IsProjectedWindowsRuntimeType(this IHasCustomAttribute type)
    {
        return type.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute");
    }
}
