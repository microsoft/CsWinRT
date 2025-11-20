// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IInspectable</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/inspectable/nn-inspectable-iinspectable"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IInspectableVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;

    /// <summary>
    /// Gets the interfaces that are implemented by the current Windows Runtime class.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="iidCount">
    /// The number of interfaces that are implemented by the current Windows Runtime
    /// object, excluding the <c>IUnknown</c> and <c>IInspectable</c> implementations.
    /// </param>
    /// <param name="iids">
    /// A pointer to an array that contains an IID for each interface implemented by the current
    /// Windows Runtime object. The <c>IUnknown</c> and <c>IInspectable</c> interfaces are excluded.
    /// </param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetIidsUnsafe(void* thisPtr, uint* iidCount, Guid** iids)
    {
        return ((IInspectableVftbl*)*(void***)thisPtr)->GetIids(thisPtr, iidCount, iids);
    }

    /// <summary>
    /// Gets the fully qualified name of the current Windows Runtime object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="className">The fully qualified name of the current Windows Runtime object.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetRuntimeClassNameUnsafe(void* thisPtr, HSTRING* className)
    {
        return ((IInspectableVftbl*)*(void***)thisPtr)->GetRuntimeClassName(thisPtr, className);
    }

    /// <summary>
    /// Gets the trust level of the current Windows Runtime object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="trustLevel">The trust level of the current Windows Runtime object. The default is <see cref="TrustLevel.BaseTrust"/>.</param>
    /// <returns>This method always returns <c>S_OK</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT GetTrustLevelUnsafe(void* thisPtr, TrustLevel* trustLevel)
    {
        return ((IInspectableVftbl*)*(void***)thisPtr)->GetTrustLevel(thisPtr, trustLevel);
    }
}