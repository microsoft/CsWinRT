// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

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
internal static class IRestrictedErrorInfoMethods
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
        out string? description,
        out int error,
        out string? restrictedDescription,
        out string? capabilitySid)
    {
        char* _description = null;
        char* _restrictedDescription = null;
        char* _capabilitySid = null;

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

            description = BStrStringMarshaller.ConvertToManaged((ushort*)_description);
            restrictedDescription = BStrStringMarshaller.ConvertToManaged((ushort*)_restrictedDescription);
            capabilitySid = BStrStringMarshaller.ConvertToManaged((ushort*)_capabilitySid);
        }
        finally
        {
            BStrStringMarshaller.Free((ushort*)_description);
            BStrStringMarshaller.Free((ushort*)_restrictedDescription);
            BStrStringMarshaller.Free((ushort*)_capabilitySid);
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
        char* _description = null;
        char* _restrictedDescription = null;
        char* _capabilitySid = null;

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
            BStrStringMarshaller.Free((ushort*)_description);
            BStrStringMarshaller.Free((ushort*)_restrictedDescription);
            BStrStringMarshaller.Free((ushort*)_capabilitySid);
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
        char* _retval = null;

        try
        {
            ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetReference(
                thisPtr,
                &_retval).Assert();

            string? description = BStrStringMarshaller.ConvertToManaged((ushort*)_retval);

            return description ?? string.Empty;
        }
        finally
        {
            BStrStringMarshaller.Free((ushort*)_retval);
        }
    }
}
