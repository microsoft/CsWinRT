// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using ABI.WinRT.Interop;

namespace WinRT
{
    internal static partial class Context
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern unsafe int CoGetContextToken(IntPtr* contextToken);

        public unsafe static IntPtr GetContextToken()
        {
            IntPtr contextToken;
            Marshal.ThrowExceptionForHR(CoGetContextToken(&contextToken));
            return contextToken;
        }

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern unsafe int CoGetObjectContext(Guid* riid, IntPtr* ppv);

        public static unsafe IntPtr GetContextCallback()
        {
            Guid iid = InterfaceIIDs.IContextCallback_IID;
            IntPtr contextCallbackPtr;
            Marshal.ThrowExceptionForHR(CoGetObjectContext(&iid, &contextCallbackPtr));
            return contextCallbackPtr;
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

            (*(IContextCallbackVftbl**)contextCallbackPtr)->ContextCallback(contextCallbackPtr, callback, onFailCallback);
        }

        public static void DisposeContextCallback(IntPtr contextCallbackPtr)
        {
            MarshalInspectable<object>.DisposeAbi(contextCallbackPtr);
        }
    }
}
