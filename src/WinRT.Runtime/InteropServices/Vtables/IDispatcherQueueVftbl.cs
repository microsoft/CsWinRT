// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IDispatcherQueue</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueue"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueue"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IDispatcherQueueVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> CreateTimer;
    public delegate* unmanaged[MemberFunction]<void*, void*, bool*, HRESULT> TryEnqueue;

    // Ignoring the other vtable slots, as they would depend on additional types, plus we don't need them

    /// <summary>
    /// Adds a task to the dispatcher queue which will be executed on the thread associated with it.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="callback">The task to execute.</param>
    /// <param name="result">Whether the task was added to the queue.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT TryEnqueueUnsafe(void* thisPtr, void* callback, bool* result)
    {
        return ((IDispatcherQueueVftbl*)*(void***)thisPtr)->TryEnqueue(thisPtr, callback, result);
    }
}