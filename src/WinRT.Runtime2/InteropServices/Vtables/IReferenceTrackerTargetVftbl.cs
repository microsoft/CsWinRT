// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IReferenceTrackerTarget</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetrackertarget"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IReferenceTrackerVftblTargetVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRefFromReferenceTracker;
    public delegate* unmanaged[MemberFunction]<void*, uint> ReleaseFromReferenceTracker;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Peg;
    public delegate* unmanaged[MemberFunction]<void*, HRESULT> Unpeg;

    /// <summary>
    /// Indicates that the reference tracker is returning the target XAML object(s) from previous calls
    /// to <see cref="IReferenceTrackerVftbl.FindTrackerTargets"/>. Note that the reference is held by the
    /// reference tracker object in lieu of <see cref="IUnknownVftbl.AddRef"/>.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>The reference count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AddRefFromReferenceTrackerUnsafe(void* thisPtr)
    {
        return ((IReferenceTrackerVftblTargetVftbl*)thisPtr)->AddRefFromReferenceTracker(thisPtr);
    }

    /// <summary>
    /// Releases the XAML object reference marked in a previous call to <see cref="AddRefFromReferenceTracker"/>.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>The reference count.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReleaseFromReferenceTrackerUnsafe(void* thisPtr)
    {
        return ((IReferenceTrackerVftblTargetVftbl*)thisPtr)->ReleaseFromReferenceTracker(thisPtr);
    }
}
