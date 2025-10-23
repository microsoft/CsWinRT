// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

internal static class ILanguageExceptionErrorInfo2Methods
{
    public static unsafe void* GetPreviousLanguageExceptionErrorInfo(void* thisPtr)
    {
        void* __retval = default;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfo2*)*(void***)thisPtr)->GetPreviousLanguageExceptionErrorInfo(
            thisPtr,
            &__retval));
        return __retval;
    }

    public static unsafe void CapturePropagationContext(void* thisPtr, Exception ex)
    {
#if NET
        WindowsRuntimeObjectReferenceValue managedExceptionWrapper = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(ex);
        try
        {
            Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfo2*)*(void***)thisPtr)->CapturePropagationContext(
                thisPtr,
                managedExceptionWrapper.GetThisPtrUnsafe()));
        }
        finally
        {
            Marshal.Release((nint)managedExceptionWrapper.DetachThisPtrUnsafe());
        }
#else
            using var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex);
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)(*(void***)thisPtr)[5])(
                thisPtr,
                managedExceptionWrapper.ThisPtr));
#endif
    }

    public static unsafe void* GetPropagationContextHead(void* thisPtr)
    {
        void* __retval = default;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfo2*)*(void***)thisPtr)->GetPropagationContextHead(
            thisPtr,
            &__retval));
        return __retval;
    }
}
