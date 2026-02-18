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
    /// The name of the generated interop .dll  (i.e. <c>WinRT.Interop.dll</c>).
    /// </summary>
    public const string WindowsRuntimeInteropDllName = "WinRT.Interop.dll";

    /// <summary>
    /// The name of the generated projection .dll  (i.e. <c>WinRT.Projection.dll</c>).
    /// </summary>
    public const string WindowsRuntimeProjectionDllName = "WinRT.Projection.dll";

    /// <summary>
    /// The name of the generated component .dll  (i.e. <c>WinRT.Component.dll</c>).
    /// </summary>
    public const string WindowsRuntimeComponentDllName = "WinRT.Component.dll";

    /// <summary>
    /// The name of the generated interop assembly (i.e. <c>WinRT.Interop.dll</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeInteropAssemblyNameUtf8 => "WinRT.Interop"u8;

    /// <summary>
    /// The name of the generated interop .dll (i.e. <c>WinRT.Interop.dll</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeInteropDllNameUtf8 => "WinRT.Interop.dll"u8;

    /// <summary>
    /// The name of the Windows Runtime .dll (i.e. <c>WinRT.Runtime.dll</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeDllNameUtf8 => "WinRT.Runtime.dll"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKDllNameUtf8 => "Microsoft.Windows.SDK.NET.dll"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKXamlDllNameUtf8 => "Microsoft.Windows.UI.Xaml.dll"u8;
}