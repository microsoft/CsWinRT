// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>IIDs for XAML interfaces that differ between UWP XAML and WinUI 3.</summary>
internal static class WellKnownXamlInterfaceIIDs
{
    /// <summary>
    /// Gets the IID for <c>INotifyPropertyChanged</c>.
    /// </summary>
    public static ref readonly Guid IID_INotifyPropertyChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_INotifyPropertyChanged
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_INotifyPropertyChanged;
    }

    /// <summary>
    /// Gets the IID for <c>INotifyCollectionChanged</c>.
    /// </summary>
    public static ref readonly Guid IID_INotifyCollectionChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_INotifyCollectionChanged
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_INotifyCollectionChanged;
    }

    /// <summary>
    /// Gets the IID for <c>INotifyCollectionChangedEventArgs</c>.
    /// </summary>
    public static ref readonly Guid IID_INotifyCollectionChangedEventArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_INotifyCollectionChangedEventArgs
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_INotifyCollectionChangedEventArgs;
    }

    /// <summary>
    /// Gets the IID for <c>INotifyCollectionChangedEventArgsFactory</c>.
    /// </summary>
    public static ref readonly Guid IID_INotifyCollectionChangedEventArgsFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_INotifyCollectionChangedEventArgsFactory
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_INotifyCollectionChangedEventArgsFactory;
    }

    /// <summary>
    /// Gets the IID for <c>NotifyCollectionChangedEventHandler</c>.
    /// </summary>
    public static ref readonly Guid IID_NotifyCollectionChangedEventHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_NotifyCollectionChangedEventHandler
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_NotifyCollectionChangedEventHandler;
    }

    /// <summary>
    /// Gets the IID for <c>PropertyChangedEventArgs</c>.
    /// </summary>
    public static ref readonly Guid IID_PropertyChangedEventArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_PropertyChangedEventArgs
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_PropertyChangedEventArgs;
    }

    /// <summary>
    /// Gets the IID for <c>PropertyChangedEventArgsRuntimeClassFactory</c>.
    /// </summary>
    public static ref readonly Guid IID_PropertyChangedEventArgsRuntimeClassFactory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_PropertyChangedEventArgsRuntimeClassFactory
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_PropertyChangedEventArgsRuntimeClassFactory;
    }

    /// <summary>
    /// Gets the IID for <c>PropertyChangedEventHandler</c>.
    /// </summary>
    public static ref readonly Guid IID_PropertyChangedEventHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_PropertyChangedEventHandler
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_PropertyChangedEventHandler;
    }

    /// <summary>
    /// Gets the IID for <c>IReferenceOfPropertyChangedEventHandler</c>.
    /// </summary>
    public static ref readonly Guid IID_IReferenceOfPropertyChangedEventHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_IReferenceOfPropertyChangedEventHandler
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_IReferenceOfPropertyChangedEventHandler;
    }

    /// <summary>
    /// Gets the IID for <c>IReferenceOfNotifyCollectionChangedEventHandler</c>.
    /// </summary>
    public static ref readonly Guid IID_IReferenceOfNotifyCollectionChangedEventHandler
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
            ? ref WellKnownWindowsInterfaceIIDs.IID_WUX_IReferenceOfNotifyCollectionChangedEventHandler
            : ref WellKnownWindowsInterfaceIIDs.IID_MUX_IReferenceOfNotifyCollectionChangedEventHandler;
    }
}