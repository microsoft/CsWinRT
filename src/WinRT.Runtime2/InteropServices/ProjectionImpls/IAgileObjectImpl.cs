﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IAgileObject</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/objidlbase/nn-objidlbase-iagileobject"/>
public static unsafe class IAgileObjectImpl
{
    /// <summary>
    /// Gets the IID for the <c>IAgileObject</c> interface.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IAgileObject;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IAgileObject</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        // The 'IAgileObject' interface is a marker interface, so we can reuse 'IUnknown'
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IUnknownImpl.Vtable;
    }
}
