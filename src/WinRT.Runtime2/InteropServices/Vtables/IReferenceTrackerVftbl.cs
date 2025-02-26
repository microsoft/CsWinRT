// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IReferenceTracker</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetracker"/>
internal unsafe struct IReferenceTrackerVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> ConnectFromTrackerSource;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> DisconnectFromTrackerSource;
    public delegate* unmanaged[MemberFunction]<void*, void*, HRESULT> FindTrackerTargets;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> GetReferenceTrackerManager;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> AddRefFromTrackerSource;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> ReleaseFromTrackerSource;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> PegFromTrackerSource;

    /// <summary>
    /// Indicates each time that a tracker source calls <c>IUnknown::AddRef</c> on the reference tracker; called after the <c>AddRef</c> call.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT AddRefFromTrackerSourceUnsafe(void* thisPtr)
    {
        return ((IReferenceTrackerVftbl*)thisPtr)->AddRefFromTrackerSource(thisPtr);
    }

    /// <summary>
    /// Indicates each time that a tracker source calls <c>IUnknown::Release</c> on the reference tracker; must be called before the <c>Release</c> call.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>If this method succeeds, it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ReleaseFromTrackerSourceUnsafe(void* thisPtr)
    {
        return ((IReferenceTrackerVftbl*)thisPtr)->ReleaseFromTrackerSource(thisPtr);
    }
}
