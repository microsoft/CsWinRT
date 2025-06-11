// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IAgileReference</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/activation/nn-activation-iactivationfactory"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IActivationFactoryVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> ActivateInstance;

    /// <summary>
    /// Creates a new instance of the Windows Runtime class that is associated with the current activation factory.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="instance">A pointer to a new instance of the class that is associated with the current activation factory.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ActivateInstanceUnsafe(void* thisPtr, void** instance)
    {
        return ((IActivationFactoryVftbl*)*(void***)thisPtr)->ActivateInstance(thisPtr, instance);
    }

    /// <param name="param0">An additional <c>HSTRING</c> parameter.</param>
    /// <inheritdoc cref="ActivateInstanceUnsafe(void*, void**)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ActivateInstanceUnsafe(
        void* thisPtr,
        HSTRING param0,
        void** instance)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, HSTRING, void**, HRESULT>)((IActivationFactoryVftbl*)*(void***)thisPtr)->ActivateInstance)(
            thisPtr,
            param0,
            instance);
    }

    /// <param name="baseInterface">The controlling <c>IInspectable</c> object.</param>
    /// <param name="innerInterface">The resulting non-delegating <c>IInspectable</c> object.</param>
    /// <inheritdoc cref="ActivateInstanceUnsafe(void*, void**)"/>"/>
    /// <remarks>
    /// This overload should only be used to activate composable types (both when aggregating or not).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ActivateInstanceUnsafe(
        void* thisPtr,
        void* baseInterface,
        void** innerInterface,
        void** instance)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, void*, void**, void**, HRESULT>)((IActivationFactoryVftbl*)*(void***)thisPtr)->ActivateInstance)(
            thisPtr,
            baseInterface,
            innerInterface,
            instance);
    }

    /// <param name="param0">An additional <c>HSTRING</c> parameter.</param>
    /// <inheritdoc cref="ActivateInstanceUnsafe(void*, void*, void**, void**)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ActivateInstanceUnsafe(
        void* thisPtr,
        HSTRING param0,
        void* baseInterface,
        void** innerInterface,
        void** instance)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, HSTRING, void*, void**, void**, HRESULT>)((IActivationFactoryVftbl*)*(void***)thisPtr)->ActivateInstance)(
            thisPtr,
            param0,
            baseInterface,
            innerInterface,
            instance);
    }

    /// <param name="param0">An additional <see cref="NotifyCollectionChangedAction"/> parameter.</param>
    /// <param name="param1">An additional <see cref="object"/> parameter.</param>
    /// <param name="param2">An additional <see cref="object"/> parameter.</param>
    /// <param name="param3">An additional <see cref="int"/> parameter.</param>
    /// <param name="param4">An additional <see cref="int"/> parameter.</param>
    /// <inheritdoc cref="ActivateInstanceUnsafe(void*, void*, void**, void**)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ActivateInstanceUnsafe(
        void* thisPtr,
        NotifyCollectionChangedAction param0,
        void* param1,
        void* param2,
        int param3,
        int param4,
        void* baseInterface,
        void** innerInterface,
        void** instance)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, NotifyCollectionChangedAction, void*, void*, int, int, void*, void**, void**, HRESULT>)((IActivationFactoryVftbl*)*(void***)thisPtr)->ActivateInstance)(
            thisPtr,
            param0,
            param1,
            param2,
            param3,
            param4,
            baseInterface,
            innerInterface,
            instance);
    }
}
