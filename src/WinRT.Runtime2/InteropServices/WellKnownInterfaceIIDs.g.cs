// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// IIDs for well known projected WinRT interfaces.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class WellKnownInterfaceIIDs
{
    /// <summary>The IID for <c>IUnknown</c>.</summary>
    public static ref readonly Guid IID_IUnknown
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IUnknown;
    }

    /// <summary>The IID for <c>IAgileObject</c>.</summary>
    public static ref readonly Guid IID_IAgileObject
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IAgileObject;
    }

    /// <summary>The IID for <c>IBufferByteAccess</c>.</summary>
    public static ref readonly Guid IID_IBufferByteAccess
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBufferByteAccess;
    }

    /// <summary>The IID for <c>IMarshal</c>.</summary>
    public static ref readonly Guid IID_IMarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IMarshal;
    }

    /// <summary>The IID for <c>IMemoryBufferByteAccess</c>.</summary>
    public static ref readonly Guid IID_IMemoryBufferByteAccess
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IMemoryBufferByteAccess;
    }

    /// <summary>The IID for <c>IInspectable</c>.</summary>
    public static ref readonly Guid IID_IInspectable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IInspectable;
    }

    /// <summary>The IID for <c>IStringable</c>.</summary>
    public static ref readonly Guid IID_IStringable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IStringable;
    }

    /// <summary>The IID for <c>IPropertyValue</c>.</summary>
    public static ref readonly Guid IID_IPropertyValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IPropertyValue;
    }

    /// <summary>The IID for <c>IWeakReferenceSource</c>.</summary>
    public static ref readonly Guid IID_IWeakReferenceSource
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource;
    }

    /// <summary>The IID for <c>Windows.UI.Xaml.Interop.IBindableIterable</c> (mapped to <see cref="global::System.Collections.IEnumerable"/>.</summary>
    public static ref readonly Guid IID_Windows_UI_Xaml_Interop_IBindableIterable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBindableIterable;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Interop.IBindableIterable</c> (mapped to <see cref="global::System.Collections.IEnumerable"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Interop_IBindableIterable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBindableIterable;
    }

    /// <summary>The IID for <c>Windows.UI.Xaml.Interop.IBindableIterator</c> (mapped to <see cref="global::System.Collections.IEnumerator"/>.</summary>
    public static ref readonly Guid IID_Windows_UI_Xaml_Interop_IBindableIterator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBindableIterator;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Interop.IBindableIterator</c> (mapped to <see cref="global::System.Collections.IEnumerator"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Interop_IBindableIterator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBindableIterator;
    }

    /// <summary>The IID for <c>Windows.UI.Xaml.Interop.IBindableVector</c> (mapped to <see cref="global::System.Collections.IList"/>.</summary>
    public static ref readonly Guid IID_Windows_UI_Xaml_Interop_IBindableVector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBindableVector;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Interop.IBindableVector</c> (mapped to <see cref="global::System.Collections.IList"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Interop_IBindableVector
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IBindableVector;
    }

    /// <summary>The IID for <c>Windows.UI.Xaml.Data.INotifyPropertyChanged</c> (mapped to <see cref="global::System.ComponentModel.INotifyPropertyChanged"/>.</summary>
    public static ref readonly Guid IID_Windows_UI_Xaml_Data_INotifyPropertyChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_WUX_INotifyPropertyChanged;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Data.INotifyPropertyChanged</c> (mapped to <see cref="global::System.ComponentModel.INotifyPropertyChanged"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Data_INotifyPropertyChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_MUX_INotifyPropertyChanged;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Data.INotifyDataErrorInfo</c> (mapped to <see cref="global::System.ComponentModel.INotifyDataErrorInfo"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Data_INotifyDataErrorInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_INotifyDataErrorInfo;
    }

    /// <summary>The IID for <c>Windows.UI.Xaml.Interop.INotifyCollectionChanged</c> (mapped to <see cref="global::System.Collections.Specialized.INotifyCollectionChanged"/>.</summary>
    public static ref readonly Guid IID_Windows_UI_Xaml_Interop_INotifyCollectionChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_WUX_INotifyCollectionChanged;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Interop.INotifyCollectionChanged</c> (mapped to <see cref="global::System.Collections.Specialized.INotifyCollectionChanged"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Interop_INotifyCollectionChanged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_MUX_INotifyCollectionChanged;
    }

    /// <summary>The IID for <c>Windows.Foundation.IClosable</c> (mapped to <see cref="global::System.IDisposable"/>.</summary>
    public static ref readonly Guid IID_Windows_Foundation_IClosable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IClosable;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.IXamlServiceProvider</c> (mapped to <see cref="global::System.IServiceProvider"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_IXamlServiceProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IXamlServiceProvider;
    }

    /// <summary>The IID for <c>Windows.UI.Xaml.Input.ICommand</c> (mapped to <see cref="global::System.Windows.Input.ICommand"/>.</summary>
    public static ref readonly Guid IID_Windows_UI_Xaml_Input_ICommand
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_ICommand;
    }

    /// <summary>The IID for <c>Microsoft.UI.Xaml.Input.ICommand</c> (mapped to <see cref="global::System.Windows.Input.ICommand"/>.</summary>
    public static ref readonly Guid IID_Microsoft_UI_Xaml_Input_ICommand
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_ICommand;
    }

    /// <summary>The IID for <c>Windows.Foundation.Collections.IVectorChangedEventArgs</c> (mapped to <see cref="global::Windows.Foundation.Collections.IVectorChangedEventArgs"/>.</summary>
    public static ref readonly Guid IID_Windows_Foundation_Collections_IVectorChangedEventArgs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IVectorChangedEventArgs;
    }

    /// <summary>The IID for <c>Windows.Foundation.IAsyncAction</c> (mapped to <see cref="global::Windows.Foundation.IAsyncAction"/>.</summary>
    public static ref readonly Guid IID_Windows_Foundation_IAsyncAction
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IAsyncAction;
    }

    /// <summary>The IID for <c>Windows.Foundation.IAsyncInfo</c> (mapped to <see cref="global::Windows.Foundation.IAsyncInfo"/>.</summary>
    public static ref readonly Guid IID_Windows_Foundation_IAsyncInfo
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get => ref WellKnownWindowsInterfaceIIDs.IID_IAsyncInfo;
    }
}