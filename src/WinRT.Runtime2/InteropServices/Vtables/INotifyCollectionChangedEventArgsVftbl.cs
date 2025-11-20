// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

#pragma warning disable IDE1006

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>INotifyCollectionChangedEventArgs</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.notifycollectionchangedeventargs"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct INotifyCollectionChangedEventArgsVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, NotifyCollectionChangedAction*, HRESULT> get_Action;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_NewItems;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> get_OldItems;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> get_NewStartingIndex;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> get_OldStartingIndex;

    /// <summary>
    /// Gets the description of the action that caused the event.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="action">The description of the action that caused the event, as a value of the enumeration.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_ActionUnsafe(void* thisPtr, NotifyCollectionChangedAction* action)
    {
        return ((INotifyCollectionChangedEventArgsVftbl*)*(void***)thisPtr)->get_Action(thisPtr, action);
    }

    /// <summary>
    /// Gets the items affected by an action.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="newItems">The bindable vector of items affected by an action.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_NewItemsUnsafe(void* thisPtr, void* newItems)
    {
        return ((INotifyCollectionChangedEventArgsVftbl*)*(void***)thisPtr)->get_NewItems(thisPtr, newItems);
    }

    /// <summary>
    /// Gets the item affected by a <c>Replace</c> or <c>Remove</c> action.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="oldItems">The bindable vector of items affected by a <c>Replace</c> or <c>Remove</c> action.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_OldItemsUnsafe(void* thisPtr, void* oldItems)
    {
        return ((INotifyCollectionChangedEventArgsVftbl*)*(void***)thisPtr)->get_OldItems(thisPtr, oldItems);
    }

    /// <summary>
    /// Gets the index at which the change occurred.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="newStartingIndex">The index at which the change occurred.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_NewStartingIndexUnsafe(void* thisPtr, int* newStartingIndex)
    {
        return ((INotifyCollectionChangedEventArgsVftbl*)*(void***)thisPtr)->get_NewStartingIndex(thisPtr, newStartingIndex);
    }

    /// <summary>
    /// Gets the starting index at which a <c>Move</c>, <c>Remove</c>, or <c>Replace</c> action occurred.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="oldStartingIndex">The zero-based index at which a <c>Move</c>, <c>Remove</c>, or <c>Replace</c> action occurred.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT get_OldStartingIndexUnsafe(void* thisPtr, int* oldStartingIndex)
    {
        return ((INotifyCollectionChangedEventArgsVftbl*)*(void***)thisPtr)->get_OldStartingIndex(thisPtr, oldStartingIndex);
    }
}