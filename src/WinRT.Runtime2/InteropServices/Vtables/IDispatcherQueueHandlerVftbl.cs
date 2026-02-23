// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IDispatcherQueueHandler</c> interface vtable.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueuehandler"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueuehandler"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IDispatcherQueueHandlerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Invoke;
}