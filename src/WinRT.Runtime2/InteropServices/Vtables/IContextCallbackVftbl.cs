// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IContextCallback</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/ctxtcall/nn-ctxtcall-icontextcallback"/>
/// <seealso href="https://devblogs.microsoft.com/oldnewthing/20191128-00/?p=103157"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IContextCallbackVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, PFNCONTEXTCALL, ComCallData*, Guid*, int, void*, HRESULT> ContextCallback;

    /// <summary>
    /// Enters the object context, executes the specified function, and returns.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="pfnCallback">The function to be called inside the object context.</param>
    /// <param name="pParam">The data to be passed to the function when it is called in the context.</param>
    /// <param name="riid">The IID of the call that is being simulated.</param>
    /// <param name="iMethod">The method number of the call that is being simulated.</param>
    /// <param name="pUnk">This parameter is reserved and must be <see langword="null"/>.</param>
    /// <returns>A standard failure <c>HRESULT</c>, or the one returned by <paramref name="pfnCallback"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HRESULT ContextCallbackUnsafe(
        void* thisPtr,
        PFNCONTEXTCALL pfnCallback,
        ComCallData* pParam,
        Guid* riid,
        int iMethod,
        void* pUnk)
    {
        return ((IContextCallbackVftbl*)*(void***)thisPtr)->ContextCallback(thisPtr, pfnCallback, pParam, riid, iMethod, pUnk);
    }
}