// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>Runtime class names for XAML interfaces that differ between UWP XAML and WinUI 3.</summary>
internal static class WellKnownXamlRuntimeClassNames
{
    /// <summary>
    /// Gets the runtime class name for <c>IBindableIterable</c>.
    /// </summary>
    public static string IBindableIterable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Interop.IBindableIterable"
            : "Microsoft.UI.Xaml.Interop.IBindableIterable";
    }

    /// <summary>
    /// Gets the runtime class name for <c>IBindableIterator</c>.
    /// </summary>
    public static string IBindableIterator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Interop.IBindableIterator"
            : "Microsoft.UI.Xaml.Interop.IBindableIterator";
    }

    /// <summary>
    /// Gets the runtime class name for <c>IBindableVector</c>.
    /// </summary>
    public static string IBindableVector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Interop.IBindableVector"
            : "Microsoft.UI.Xaml.Interop.IBindableVector";
    }

    /// <summary>
    /// Gets the runtime class name for <c>IBindableVectorView</c>.
    /// </summary>
    public static string IBindableVectorView
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Interop.IBindableVectorView"
            : "Microsoft.UI.Xaml.Interop.IBindableVectorView";
    }

    /// <summary>
    /// Gets the runtime class name for <c>NotifyCollectionChangedEventArgs</c>.
    /// </summary>
    public static string NotifyCollectionChangedEventArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs"
            : "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs";
    }

    /// <summary>
    /// Gets the runtime class name for <c>PropertyChangedEventArgs</c>.
    /// </summary>
    public static string PropertyChangedEventArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? "Windows.UI.Xaml.Data.PropertyChangedEventArgs"
            : "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs";
    }
}