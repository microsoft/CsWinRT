// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace ABI;

/// <summary>
/// IIDs for common WinRT interfaces.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class InterfaceIIDs
{
    /// <summary>The IID for <see cref="global::System.Collections.IEnumerable"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_Collections_IBindableIterable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IBindableIterable;
    }

    /// <summary>The IID for <see cref="global::System.Collections.IEnumerator"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_Collections_IBindableIterator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IBindableIterator;
    }

    /// <summary>The IID for <see cref="global::System.Collections.IList"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_Collections_IBindableVector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IBindableVector;
    }

    /// <summary>The IID for <see cref="global::System.Collections.Specialized.INotifyCollectionChanged"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_Collections_Specialized_INotifyCollectionChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownXamlInterfaceIds.IID_INotifyCollectionChanged;
    }

    /// <summary>The IID for <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_ComponentModel_INotifyDataErrorInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_INotifyDataErrorInfo;
    }

    /// <summary>The IID for <see cref="global::System.ComponentModel.INotifyPropertyChanged"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_ComponentModel_INotifyPropertyChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownXamlInterfaceIds.IID_INotifyPropertyChanged;
    }

    /// <summary>The IID for <see cref="global::System.IDisposable"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_IDisposable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IDisposable;
    }

    /// <summary>The IID for <see cref="global::System.IServiceProvider"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_IServiceProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IServiceProvider;
    }

    /// <summary>The IID for <see cref="global::System.Windows.Input.ICommand"/>.</summary>
    public static ref readonly Guid IID_global__ABI_System_Windows_Input_ICommand
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_ICommand;
    }

    /// <summary>The IID for <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/>.</summary>
    public static ref readonly Guid IID_global__ABI_Windows_Foundation_Collections_IVectorChangedEventArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IVectorChangedEventArgs;
    }

    /// <summary>The IID for <see cref="global::Windows.Foundation.IAsyncAction"/>.</summary>
    public static ref readonly Guid IID_global__ABI_Windows_Foundation_IAsyncAction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IAsyncAction;
    }

    /// <summary>The IID for <see cref="global::Windows.Foundation.IAsyncInfo"/>.</summary>
    public static ref readonly Guid IID_global__ABI_Windows_Foundation_IAsyncInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownInterfaceIds.IID_IAsyncInfo;
    }
}