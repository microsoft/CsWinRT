// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using ABI.WinRT.Interop;
using WinRT.Interop;

namespace WinRT
{
    internal static partial class Context
    {
        public unsafe static IntPtr GetContextToken()
        {
            IntPtr contextToken;
            Marshal.ThrowExceptionForHR(Platform.CoGetContextToken(&contextToken));
            return contextToken;
        }

        public static unsafe IntPtr GetContextCallback()
        {
            Guid iid = IID.IID_IContextCallback;
            IntPtr contextCallbackPtr;
            Marshal.ThrowExceptionForHR(Platform.CoGetObjectContext(&iid, &contextCallbackPtr));
            return contextCallbackPtr;
        }

        // Calls the given callback in the right context.
        // On any exception, calls onFail callback if any set.
        // If not set, exception is handled due to today we don't
        // have any scenario to propagate it from here.
        public unsafe static void CallInContext(IntPtr contextCallbackPtr, IntPtr contextToken, Action<object> callback, Action<object> onFailCallback, object state)
        {
            // Check if we are already on the same context, if so we do not need to switch.
            if(contextCallbackPtr == IntPtr.Zero || GetContextToken() == contextToken)
            {
                callback(state);
                return;
            }

#if NET && CsWinRT_LANG_11_FEATURES
            IContextCallbackVftbl.ContextCallback(contextCallbackPtr, callback, onFailCallback, state);
#else
            ComCallData data = default;
            var contextCallback = new ABI.WinRT.Interop.IContextCallback(ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(contextCallbackPtr));

            try
            {
                contextCallback.ContextCallback(_ =>
                {
                    callback(state);
                    return 0;
                }, &data, IID.IID_ICallbackWithNoReentrancyToApplicationSTA, 5);
            } 
            catch (Exception)
            {
                onFailCallback?.Invoke(state);
            }
#endif
        }

        public static void DisposeContextCallback(IntPtr contextCallbackPtr)
        {
            MarshalInspectable<object>.DisposeAbi(contextCallbackPtr);
        }
    }
}
