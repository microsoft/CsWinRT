// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    static class Context
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern unsafe int CoGetContextToken(IntPtr* contextToken);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern int CoGetObjectContext(ref Guid riid, out IntPtr ppv);

        private static readonly Guid IID_ICallbackWithNoReentrancyToApplicationSTA = new(0x0A299774, 0x3E4E, 0xFC42, 0x1D, 0x9D, 0x72, 0xCE, 0xE1, 0x05, 0xCA, 0x57);

        public static IntPtr GetContextCallback()
        {
            Guid riid = ABI.WinRT.Interop.IContextCallback.IID;
            Marshal.ThrowExceptionForHR(CoGetObjectContext(ref riid, out IntPtr contextCallbackPtr));
            return contextCallbackPtr;
        }

        public unsafe static IntPtr GetContextToken()
        {
            IntPtr contextToken;
            Marshal.ThrowExceptionForHR(CoGetContextToken(&contextToken));
            return contextToken;
        }

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

            ComCallData data = default;
            var contextCallback = new ABI.WinRT.Interop.IContextCallback(ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.FromAbi(contextCallbackPtr));

            try
            {
                contextCallback.ContextCallback(_ =>
                {
                    callback();
                    return 0;
                }, &data, IID_ICallbackWithNoReentrancyToApplicationSTA, 5);
            } 
            catch(Exception)
            {
                onFailCallback?.Invoke();
            }
        }

        public static void DisposeContextCallback(IntPtr contextCallbackPtr)
        {
            MarshalInspectable<object>.DisposeAbi(contextCallbackPtr);
        }
    }
}
