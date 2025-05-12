// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known interop names (constants).
/// </summary>
internal static class WellKnownInteropNames
{
    /// <summary>
    /// The name of the CsWinRT runtime .dll.
    /// </summary>
    public const string WindowsRuntimeDllName = "WinRT.Runtime.dll";

    /// <summary>
    /// The name of the generated interop .dll.
    /// </summary>
    public const string InteropDllName = "WinRT.Interop.dll";

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public const string WindowsSDKDllName = "Microsoft.Windows.SDK.NET.dll";

    /// <summary>
    /// The name of the Windows SDK XAML projections .dll.
    /// </summary>
    public const string WindowsSDKXamlDllName = "Microsoft.Windows.UI.Xaml.dll";

    /// <summary>
    /// The name of the generated interop .dll.
    /// </summary>
    public static ReadOnlySpan<byte> InteropDllNameUtf8 => "WinRT.Interop.dll"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKDllNameUtf8 => "Microsoft.Windows.SDK.NET.dll"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKXamlDllNameUtf8 => "Microsoft.Windows.UI.Xaml.dll"u8;
}
