// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

internal static class IRestrictedErrorInfoMethods
{
    public static unsafe void GetErrorDetails(
        void* thisPtr,
        out string description,
        out int error,
        out string restrictedDescription,
        out string capabilitySid)
    {
        void* _description = null;
        void* _restrictedDescription = null;
        void* _capabilitySid = null;

        try
        {
            fixed (int* pError = &error)
            {
                Marshal.ThrowExceptionForHR(((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetErrorDetails(
                    thisPtr,
                    &_description,
                    pError,
                    &_restrictedDescription,
                    &_capabilitySid));
            }

            description = _description != null ? Marshal.PtrToStringBSTR((nint)_description) : string.Empty;
            restrictedDescription = _restrictedDescription != null ? Marshal.PtrToStringBSTR((nint)_restrictedDescription) : string.Empty;
            capabilitySid = _capabilitySid != null ? Marshal.PtrToStringBSTR((nint)_capabilitySid) : string.Empty;
        }
        finally
        {
            Marshal.FreeBSTR((nint)_description);
            Marshal.FreeBSTR((nint)_restrictedDescription);
            Marshal.FreeBSTR((nint)_capabilitySid);
        }
    }

    public static unsafe void GetErrorDetails(
        void* thisPtr,
        out int error)
    {
        void* _description = null;
        void* _restrictedDescription = null;
        void* _capabilitySid = null;

        try
        {
            fixed (int* pError = &error)
            {
                Marshal.ThrowExceptionForHR(((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetErrorDetails(
                    thisPtr,
                    &_description,
                    pError,
                    &_restrictedDescription,
                    &_capabilitySid));
            }
        }
        finally
        {
            Marshal.FreeBSTR((nint)_description);
            Marshal.FreeBSTR((nint)_restrictedDescription);
            Marshal.FreeBSTR((nint)_capabilitySid);
        }
    }

    public static unsafe string GetReference(void* thisPtr)
    {
        void* __retval = default;

        try
        {
            Marshal.ThrowExceptionForHR(((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetReference(
                thisPtr,
                &__retval));
            return __retval != null ? Marshal.PtrToStringBSTR((nint)__retval) : string.Empty;
        }
        finally
        {
            Marshal.FreeBSTR((nint)__retval);
        }
    }
}

internal static class ILanguageExceptionErrorInfoMethods
{
    public static unsafe void* GetLanguageException(void* thisPtr)
    {
        void* __return_value__ = null;
        Marshal.ThrowExceptionForHR(((ILanguageExceptionErrorInfoVftbl*)*(void***)thisPtr)->GetLanguageException(
            thisPtr,
            &__return_value__));
        return __return_value__;
    }
}

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
