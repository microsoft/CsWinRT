// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.WinRT.Interop
{
    internal static class IRestrictedErrorInfoMethods
    {
        public static unsafe void GetErrorDetails(
            IntPtr thisPtr,
            out string description,
            out int error,
            out string restrictedDescription,
            out string capabilitySid)
        {
            IntPtr _description = IntPtr.Zero;
            IntPtr _restrictedDescription = IntPtr.Zero;
            IntPtr _capabilitySid = IntPtr.Zero;

            try
            {
                fixed (int* pError = &error)
                {
                    // GetErrorDetails
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, IntPtr*, IntPtr*, int>)(*(void***)thisPtr)[3])(
                        thisPtr,
                        &_description,
                        pError,
                        &_restrictedDescription,
                        &_capabilitySid));
                }

                description = _description != IntPtr.Zero ? Marshal.PtrToStringBSTR(_description) : string.Empty;
                restrictedDescription = _restrictedDescription != IntPtr.Zero ? Marshal.PtrToStringBSTR(_restrictedDescription) : string.Empty;
                capabilitySid = _capabilitySid != IntPtr.Zero ? Marshal.PtrToStringBSTR(_capabilitySid) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(_description);
                Marshal.FreeBSTR(_restrictedDescription);
                Marshal.FreeBSTR(_capabilitySid);
            }
        }

        public static unsafe void GetErrorDetails(
            IntPtr thisPtr,
            out int error)
        {
            IntPtr _description = IntPtr.Zero;
            IntPtr _restrictedDescription = IntPtr.Zero;
            IntPtr _capabilitySid = IntPtr.Zero;

            try
            {
                fixed (int* pError = &error)
                {
                    // GetErrorDetails
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, IntPtr*, IntPtr*, int>)(*(void***)thisPtr)[3])(
                        thisPtr,
                        &_description,
                        pError,
                        &_restrictedDescription,
                        &_capabilitySid));
                }
            }
            finally
            {
                Marshal.FreeBSTR(_description);
                Marshal.FreeBSTR(_restrictedDescription);
                Marshal.FreeBSTR(_capabilitySid);
            }
        }

        public static unsafe string GetReference(IntPtr thisPtr)
        {
            IntPtr __retval = default;

            try
            {
                // GetReference
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[4])(
                    thisPtr,
                    &__retval));
                return __retval != IntPtr.Zero ? Marshal.PtrToStringBSTR(__retval) : string.Empty;
            }
            finally
            {
                Marshal.FreeBSTR(__retval);
            }
        }
    }

    internal static class ILanguageExceptionErrorInfoMethods
    {
        public static unsafe IntPtr GetLanguageException(IntPtr thisPtr)
        {
            IntPtr __return_value__ = IntPtr.Zero;

            // GetLanguageException
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[3])(thisPtr, &__return_value__));
            return __return_value__;
        }
    }

    internal static class ILanguageExceptionErrorInfo2Methods
    {
        public static unsafe IntPtr GetPreviousLanguageExceptionErrorInfo(IntPtr thisPtr)
        {
            IntPtr __retval = default;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[4])(
                thisPtr,
                &__retval));
            return __retval;
        }

        public static unsafe void CapturePropagationContext(IntPtr thisPtr, Exception ex)
        {
#if NET
            IntPtr managedExceptionWrapper = ComWrappersSupport.CreateCCWForObjectUnsafe(ex);

            try
            {
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)(*(void***)thisPtr)[5])(
                    thisPtr,
                    managedExceptionWrapper));
            }
            finally
            {
                Marshal.Release(managedExceptionWrapper);
            }
#else
            using var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex);
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)(*(void***)thisPtr)[5])(
                thisPtr,
                managedExceptionWrapper.ThisPtr));
#endif
        }

        public static unsafe IntPtr GetPropagationContextHead(IntPtr thisPtr)
        {
            IntPtr __retval = default;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[6])(
                thisPtr,
                &__retval));
            return __retval;
        }
    }
}