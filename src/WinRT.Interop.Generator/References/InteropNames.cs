// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropGenerator.References;

/// <summary>
/// Well known interop names (constants).
/// </summary>
internal static class InteropNames
{
    /// <summary>
    /// The name of the generated interop .dll.
    /// </summary>
    public const string InteropDllName = "WinRT.Interop.dll";

    /// <summary>
    /// The name of the generated interop assembly.
    /// </summary>
    public static ReadOnlySpan<byte> InteropAssemblyNameUtf8 => "WinRT.Interop"u8;

    /// <summary>
    /// The name of the generated interop .dll.
    /// </summary>
    public static ReadOnlySpan<byte> InteropDllNameUtf8 => "WinRT.Interop.dll"u8;

    /// <summary>
    /// The name of the WinRT runtime .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WinRTRuntimeDllNameUtf8 => "WinRT.Runtime.dll"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKDllNameUtf8 => "Microsoft.Windows.SDK.NET.dll"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKXamlDllNameUtf8 => "Microsoft.Windows.UI.Xaml.dll"u8;
}
