// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Helpers for working with context callbacks.
/// </summary>
internal static unsafe class ContextCallback
{
    /// <summary>
    /// Calls the given callback in the right context, and returns the result of that invocation.
    /// </summary>
    /// <param name="contextCallbackPtr">The context callback instance.</param>
    /// <param name="contextToken">The context token for the original context.</param>
    /// <param name="callback">The callback to invoke.</param>
    /// <param name="state">The state to pass to the callback.</param>
    /// <returns>The <c>HRESULT</c> for the operation.</returns>
    /// <remarks>
    /// If <paramref name="contextCallbackPtr"/> is <see langword="null"/>, or if <paramref name="contextToken"/> matches
    /// the current context token, then <paramref name="callback"/> will be immediately executed in the current context.
    /// Any exceptions in this case will be thrown directly. Otherwise, the <c>HRESULT</c> from marshalling is returned.
    /// </remarks>
    public static HRESULT CallInContextUnsafe(
        void* contextCallbackPtr,
        nuint contextToken,
        delegate*<object, void> callback,
        object state)
    {
        // Check if we are already on the same context, if so we do not need to switch
        if (contextCallbackPtr == null || WindowsRuntimeImports.CoGetContextToken() == contextToken)
        {
            callback(state);

            return WellKnownErrorCodes.S_OK;
        }

        ComCallData comCallData;
        comCallData.dwDispid = 0;
        comCallData.dwReserved = 0;

        CallbackData callbackData;
        callbackData.Callback = callback;
        callbackData.State = state;

        // We can just store a pointer to the callback to invoke in the context,
        // so we don't need to allocate another closure or anything. The callback
        // will be kept alive automatically, because 'comCallData' is address exposed.
        // We only do this if we can use C# 11, and if we're on modern .NET, to be safe.
        // In the callback below, we can then just retrieve the Action again to invoke it.
        comCallData.pUserDefined = &callbackData;

        // Stub to invoke on the target context
        [UnmanagedCallersOnly]
        static int InvokeCallback(ComCallData* comCallData)
        {
            try
            {
                CallbackData* callbackData = (CallbackData*)comCallData->pUserDefined;

                callbackData->Callback(callbackData->State);

                return WellKnownErrorCodes.S_OK;
            }
            catch (Exception e)
            {
                return e.HResult;
            }
        }

        HRESULT hresult;

        fixed (Guid* riid = &WellKnownWindowsInterfaceIIDs.IID_ICallbackWithNoReentrancyToApplicationSTA)
        {
            // Marshal the supplied callback on the target context
            hresult = IContextCallbackVftbl.ContextCallbackUnsafe(
                thisPtr: contextCallbackPtr,
                pfnCallback: (delegate* unmanaged<ComCallData*, int>)&InvokeCallback,
                pParam: &comCallData,
                riid: riid,
                iMethod: 5,
                pUnk: null);
        }

        return hresult;
    }

    /// <summary>
    /// Additional data for <see cref="CallInContextUnsafe"/>
    /// </summary>
    private struct CallbackData
    {
        /// <summary>
        /// The callback to invoke on the target context.
        /// </summary>
        public delegate*<object, void> Callback;

        /// <summary>
        /// The additional argument to supply to <see cref="Callback"/>.
        /// </summary>
        public object State;
    }
}
