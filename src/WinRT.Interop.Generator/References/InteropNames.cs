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
    /// The name of the generated interop .dll (i.e. <c>WinRT.Interop.dll</c>).
    /// </summary>
    public const string WindowsRuntimeInteropDllName = "WinRT.Interop.dll";

    /// <summary>
    /// The name of the precompiled projection .dll for the Windows SDK (i.e. <c>WinRT.Sdk.Projection.dll</c>).
    /// </summary>
    public const string WindowsRuntimeSdkProjectionDllName = "WinRT.Sdk.Projection.dll";

    /// <summary>
    /// The name of the precompiled projection .dll for the Windows SDK XAML types (i.e. <c>WinRT.Sdk.Xaml.Projection.dll</c>).
    /// </summary>
    public const string WindowsRuntimeSdkXamlProjectionDllName = "WinRT.Sdk.Xaml.Projection.dll";

    /// <summary>
    /// The name of the generated projection .dll (i.e. <c>WinRT.Projection.dll</c>).
    /// </summary>
    public const string WindowsRuntimeProjectionDllName = "WinRT.Projection.dll";

    /// <summary>
    /// The name of the generated component .dll (i.e. <c>WinRT.Component.dll</c>).
    /// </summary>
    public const string WindowsRuntimeComponentDllName = "WinRT.Component.dll";

    /// <summary>
    /// The name of the generated interop assembly (i.e. <c>WinRT.Interop.dll</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeInteropAssemblyNameUtf8 => "WinRT.Interop"u8;

    /// <summary>
    /// The assembly name of the precompiled projection for the Windows SDK (i.e. <c>WinRT.Sdk.Projection</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeSdkProjectionAssemblyNameUtf8 => "WinRT.Sdk.Projection"u8;

    /// <summary>
    /// The assembly name of the precompiled projection for the Windows SDK XAML types (i.e. <c>WinRT.Sdk.Xaml.Projection</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeSdkXamlProjectionAssemblyNameUtf8 => "WinRT.Sdk.Xaml.Projection"u8;

    /// <summary>
    /// The assembly name of the generated projection (i.e. <c>WinRT.Projection</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsRuntimeProjectionAssemblyNameUtf8 => "WinRT.Projection"u8;

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
    /// The assembly name of the Windows SDK projections (i.e. <c>Microsoft.Windows.SDK.NET</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKAssemblyNameUtf8 => "Microsoft.Windows.SDK.NET"u8;

    /// <summary>
    /// The name of the Windows SDK projections .dll.
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKXamlDllNameUtf8 => "Microsoft.Windows.UI.Xaml.dll"u8;

    /// <summary>
    /// The assembly name of the Windows SDK XAML projections (i.e. <c>Microsoft.Windows.UI.Xaml</c>).
    /// </summary>
    public static ReadOnlySpan<byte> WindowsSDKXamlAssemblyNameUtf8 => "Microsoft.Windows.UI.Xaml"u8;
}