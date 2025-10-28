// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level helpers to call <c>IRestrictedErrorInfo</c> methods through a vtable pointer,
/// and to safely marshal returned BSTR values to managed strings.
/// </summary>
/// <remarks>
/// These helpers assume "thisPtr" param points to a valid COM object implementing
/// the <c>IRestrictedErrorInfo</c> interface and that HRESULTs are validated via the
/// <c>Assert()</c> helper on the returned status code. Any BSTRs returned by the COM methods
/// are converted to <see cref="string"/> and then released with <see cref="Marshal.FreeBSTR(System.IntPtr)"/>.
/// </remarks>
internal static class RestrictedErrorInfoMethods
{
    /// <summary>
    /// Retrieves error details from an <c>IRestrictedErrorInfo</c> instance.
    /// </summary>
    /// <param name="thisPtr">A raw pointer to the COM object implementing <c>IRestrictedErrorInfo</c>.</param>
    /// <param name="description">On return, receives the human-readable error description (may be empty).</param>
    /// <param name="error">On return, receives the HRESULT error code.</param>
    /// <param name="restrictedDescription">On return, receives the restricted error description (may be empty).</param>
    /// <param name="capabilitySid">On return, receives the capability SID (may be empty).</param>
    /// <remarks>
    /// All returned BSTRs are freed regardless of success or failure. Empty strings are returned if
    /// any corresponding native pointer is null.
    /// </remarks>
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
                ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetErrorDetails(
                    thisPtr,
                    &_description,
                    pError,
                    &_restrictedDescription,
                    &_capabilitySid).Assert();
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

    /// <summary>
    /// Retrieves only the HRESULT error code from an <c>IRestrictedErrorInfo</c> instance.
    /// </summary>
    /// <param name="thisPtr">A raw pointer to the COM object implementing <c>IRestrictedErrorInfo</c>.</param>
    /// <param name="error">On return, receives the HRESULT error code.</param>
    /// <remarks>
    /// Although this overload ignores textual outputs, the native method still returns BSTRs.
    /// Those are always freed to prevent leaks.
    /// </remarks>
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
                ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetErrorDetails(
                    thisPtr,
                    &_description,
                    pError,
                    &_restrictedDescription,
                    &_capabilitySid).Assert();
            }
        }
        finally
        {
            Marshal.FreeBSTR((nint)_description);
            Marshal.FreeBSTR((nint)_restrictedDescription);
            Marshal.FreeBSTR((nint)_capabilitySid);
        }
    }

    /// <summary>
    /// Gets the reference string (source) associated with the error from an <c>IRestrictedErrorInfo</c> instance.
    /// </summary>
    /// <param name="thisPtr">A raw pointer to the COM object implementing <c>IRestrictedErrorInfo</c>.</param>
    /// <returns>
    /// The reference string returned by the COM object, or <see cref="string.Empty"/> if the native pointer is null.
    /// </returns>
    /// <remarks>
    /// The returned BSTR is always released even if it is null or the call fails after validation.
    /// </remarks>
    public static unsafe string GetReference(void* thisPtr)
    {
        void* __retval = null;

        try
        {
            ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetReference(
                thisPtr,
                &__retval).Assert();
            return __retval != null ? Marshal.PtrToStringBSTR((nint)__retval) : string.Empty;
        }
        finally
        {
            Marshal.FreeBSTR((nint)__retval);
        }
    }
}
