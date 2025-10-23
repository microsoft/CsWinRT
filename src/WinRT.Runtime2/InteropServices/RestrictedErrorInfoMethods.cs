// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

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