// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level helpers to call <c>IRestrictedErrorInfo</c> methods through a vtable pointer,
/// and to safely marshal returned <c>BSTR</c> values to managed strings.
/// </summary>
/// <remarks>
/// All methods assume "thisPtr" parameter points to a valid COM object implementing the <c>IRestrictedErrorInfo</c> interface.
/// </remarks>
internal static unsafe class IRestrictedErrorInfoMethods
{
    /// <summary>
    /// Retrieves error details from an <c>IRestrictedErrorInfo</c> instance.
    /// </summary>
    /// <param name="thisPtr">A raw pointer to the COM object implementing <c>IRestrictedErrorInfo</c>.</param>
    /// <param name="description">On return, receives the human-readable error description (may be empty).</param>
    /// <param name="error">On return, receives the <c>HRESULT</c> error code.</param>
    /// <param name="restrictedDescription">On return, receives the restricted error description (may be empty).</param>
    /// <param name="capabilitySid">On return, receives the capability SID (may be empty).</param>
    /// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nf-restrictederrorinfo-irestrictederrorinfo-geterrordetails"/>
    public static void GetErrorDetails(
        void* thisPtr,
        out string? description,
        out int error,
        out string? restrictedDescription,
        out string? capabilitySid)
    {
        char* descriptionPtr = null;
        char* restrictedDescriptionPtr = null;
        char* capabilitySidPtr = null;

        try
        {
            fixed (int* errorPtr = &error)
            {
                ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetErrorDetails(
                    thisPtr,
                    &descriptionPtr,
                    errorPtr,
                    &restrictedDescriptionPtr,
                    &capabilitySidPtr).Assert();
            }

            description = BStrStringMarshaller.ConvertToManaged((ushort*)descriptionPtr);
            restrictedDescription = BStrStringMarshaller.ConvertToManaged((ushort*)restrictedDescriptionPtr);
            capabilitySid = BStrStringMarshaller.ConvertToManaged((ushort*)capabilitySidPtr);
        }
        finally
        {
            BStrStringMarshaller.Free((ushort*)descriptionPtr);
            BStrStringMarshaller.Free((ushort*)restrictedDescriptionPtr);
            BStrStringMarshaller.Free((ushort*)capabilitySidPtr);
        }
    }

    /// <summary>
    /// Retrieves only the <c>HRESULT</c> error code from an <c>IRestrictedErrorInfo</c> instance.
    /// </summary>
    /// <param name="thisPtr">A raw pointer to the COM object implementing <c>IRestrictedErrorInfo</c>.</param>
    /// <param name="error">On return, receives the HRESULT error code.</param>
    /// <remarks>
    /// Although this overload ignores textual outputs, the native method still returns <c>BSTR</c>-ss.
    /// Those are always freed to prevent leaks.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nf-restrictederrorinfo-irestrictederrorinfo-geterrordetails"/>
    public static void GetErrorDetails(void* thisPtr, out int error)
    {
        char* descriptionPtr = null;
        char* restrictedDescriptionPtr = null;
        char* capabilitySidPtr = null;

        try
        {
            fixed (int* errorPtr = &error)
            {
                ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetErrorDetails(
                    thisPtr,
                    &descriptionPtr,
                    errorPtr,
                    &restrictedDescriptionPtr,
                    &capabilitySidPtr).Assert();
            }
        }
        finally
        {
            BStrStringMarshaller.Free((ushort*)descriptionPtr);
            BStrStringMarshaller.Free((ushort*)restrictedDescriptionPtr);
            BStrStringMarshaller.Free((ushort*)capabilitySidPtr);
        }
    }

    /// <summary>
    /// Gets the reference string (source) associated with the error from an <c>IRestrictedErrorInfo</c> instance.
    /// </summary>
    /// <param name="thisPtr">A raw pointer to the COM object implementing <c>IRestrictedErrorInfo</c>.</param>
    /// <returns>
    /// The reference string returned by the COM object, or <see cref="string.Empty"/> if the native pointer is <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// The returned <c>BSTR</c> is always released even if it is <see langword="null"/> or the call fails after validation.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nf-restrictederrorinfo-irestrictederrorinfo-getreference"/>
    public static string? GetReference(void* thisPtr)
    {
        char* resultPtr = null;

        try
        {
            ((IRestrictedErrorInfoVftbl*)*(void***)thisPtr)->GetReference(thisPtr, &resultPtr).Assert();

            return BStrStringMarshaller.ConvertToManaged((ushort*)resultPtr);
        }
        finally
        {
            BStrStringMarshaller.Free((ushort*)resultPtr);
        }
    }
}
