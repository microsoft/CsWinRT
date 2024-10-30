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
            global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> obj,
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
                    IntPtr thisPtr = obj.ThisPtr;

                    // GetErrorDetails
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, IntPtr*, IntPtr*, int>)(*(void***)thisPtr)[3])(
                        thisPtr,
                        &_description,
                        pError,
                        &_restrictedDescription,
                        &_capabilitySid));

                    GC.KeepAlive(obj);
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

        public static unsafe string GetReference(global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> obj)
        {
            IntPtr __retval = default;

            try
            {
                IntPtr thisPtr = obj.ThisPtr;

                // GetReference
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[4])(
                    thisPtr,
                    &__retval));

                GC.KeepAlive(obj);

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
        public static unsafe global::WinRT.IObjectReference GetLanguageException(global::WinRT.ObjectReference<IUnknownVftbl> obj)
        {
            IntPtr __return_value__ = IntPtr.Zero;

            try
            {
                IntPtr thisPtr = obj.ThisPtr;

                // GetLanguageException
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[3])(thisPtr, &__return_value__));

                GC.KeepAlive(obj);

                return global::WinRT.ObjectReference<IUnknownVftbl>.Attach(ref __return_value__, IID.IID_IUnknown);
            }
            finally
            {
                MarshalExtensions.ReleaseIfNotNull(__return_value__);
            }
        }
    }

    internal static class ILanguageExceptionErrorInfo2Methods
    {
        public static unsafe global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> GetPreviousLanguageExceptionErrorInfo(global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> obj)
        {
            IntPtr __retval = default;

            try
            {
                IntPtr thisPtr = obj.ThisPtr;

                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[4])(
                    thisPtr,
                    &__retval));

                GC.KeepAlive(obj);

                return global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl>.Attach(ref __retval, IID.IID_ILanguageExceptionErrorInfo2);
            }
            finally
            {
                MarshalExtensions.ReleaseIfNotNull(__retval);
            }
        }

        public static unsafe void CapturePropagationContext(global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> obj, Exception ex)
        {
            IntPtr thisPtr = obj.ThisPtr;

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

            GC.KeepAlive(obj);
        }

        public static unsafe global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> GetPropagationContextHead(global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl> obj)
        {
            IntPtr __retval = default;

            try
            {
                IntPtr thisPtr = obj.ThisPtr;

                Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)(*(void***)thisPtr)[6])(
                    thisPtr,
                    &__retval));

                GC.KeepAlive(obj);

                return global::WinRT.ObjectReference<global::WinRT.Interop.IUnknownVftbl>.Attach(ref __retval, IID.IID_ILanguageExceptionErrorInfo2);
            }
            finally
            {
                MarshalExtensions.ReleaseIfNotNull(__retval);
            }
        }
    }
}