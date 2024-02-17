// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using ABI.WinRT.Interop;

namespace WinRT
{
    static partial class Context
    {
        // Calls the given callback in the right context.
        // On any exception, calls onFail callback if any set.
        // If not set, exception is handled due to today we don't
        // have any scenario to propagate it from here.
        public unsafe static void CallInContext(IntPtr contextCallbackPtr, IntPtr contextToken, Action callback, Action onFailCallback)
        {
            // Check if we are already on the same context, if so we do not need to switch.
            if(contextCallbackPtr == IntPtr.Zero || GetContextToken() == contextToken)
            {
                callback();
                return;
            }

#if NET && CsWinRT_LANG_11_FEATURES
            IContextCallbackVftbl.ContextCallback(contextCallbackPtr, callback, onFailCallback);
#else
            ComCallData data = default;
            var contextCallback = new ABI.WinRT.Interop.IContextCallback(ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(contextCallbackPtr));

            try
            {
                contextCallback.ContextCallback(_ =>
                {
                    callback();
                    return 0;
                }, &data, InterfaceIIDs.ICallbackWithNoReentrancyToApplicationSTA_IID, 5);
            } 
            catch(Exception)
            {
                onFailCallback?.Invoke();
            }
#endif
        }
    }
}
