// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides low-level helpers to call <c>ILanguageExceptionErrorInfo</c> methods through a vtable pointer.
/// </summary>
/// <remarks>
/// All methods assume "thisPtr" parameter points to a valid COM object implementing the <c>ILanguageExceptionErrorInfo</c> interface.
/// </remarks>
internal static unsafe class ILanguageExceptionErrorInfoMethods
{
    /// <summary>
    /// Attempts to retrieve a managed language exception from a restricted error info pointer.
    /// This is a helper method specifically to be used by exception propagation scenarios where we carefully
    /// manage the lifetime of the CCW for the exception object to avoid cycles and thereby leaking it.
    /// </summary>
    /// <param name="thisPtr">Pointer to the language error info COM object.</param>
    /// <param name="hresult">The HRESULT associated with the error.</param>
    /// <returns>The managed <see cref="Exception"/> if found, or <see langword="null"/>.</returns>
    /// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nf-restrictederrorinfo-ilanguageexceptionerrorinfo-getlanguageexception"/>
    public static Exception? GetLanguageException(void* thisPtr, HRESULT hresult)
    {
        // Check the error info first for the language exception
        if (TryGetLanguageException(thisPtr, hresult, out Exception? exception))
        {
            return exception;
        }

        // Check if propagated exceptions are supported, and stop if they're not
        if (IUnknownVftbl.QueryInterfaceUnsafe(
            thisPtr: thisPtr,
            iid: in WellKnownInterfaceIds.IID_ILanguageExceptionErrorInfo2,
            pvObject: out void* languageErrorInfo2Ptr).Failed())
        {
            return null;
        }

        void* currentLanguageExceptionErrorInfo2Ptr;

        // If we can't get the propagation context head, stop immediately
        if (ILanguageExceptionErrorInfo2Vftbl.GetPropagationContextHeadUnsafe(languageErrorInfo2Ptr, &currentLanguageExceptionErrorInfo2Ptr).Failed())
        {
            return null;
        }

        // We can release the exception info, now that we have a reference to the language exception interface
        _ = IUnknownVftbl.ReleaseUnsafe(languageErrorInfo2Ptr);

        try
        {
            // Traverse the propagated exceptions and check if any one of those is our exception to reuse
            while (currentLanguageExceptionErrorInfo2Ptr is not null)
            {
                // Try to retrieve the propagated exception from the current error info
                if (TryGetLanguageException(currentLanguageExceptionErrorInfo2Ptr, hresult, out exception))
                {
                    return exception;
                }

                void* previousLanguageExceptionErrorInfo2Ptr;

                // Try to get the previous language exception in the propagation chain
                if (ILanguageExceptionErrorInfo2Vftbl.GetPreviousLanguageExceptionErrorInfoUnsafe(currentLanguageExceptionErrorInfo2Ptr, &previousLanguageExceptionErrorInfo2Ptr).Failed())
                {
                    return null;
                }

                // We are about to start iterating again with the previous exception info.
                // The previous one will become the current one, and the one we had before
                // can be released. So we first release that (as it's the loop variable).
                _ = IUnknownVftbl.ReleaseUnsafe(currentLanguageExceptionErrorInfo2Ptr);

                // We can now safely replace it with the newly retrieved error info and loop again
                currentLanguageExceptionErrorInfo2Ptr = previousLanguageExceptionErrorInfo2Ptr;
            }
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(currentLanguageExceptionErrorInfo2Ptr);
        }

        return null;
    }

    /// <summary>
    /// Tries to retrieve a managed language exception from a COM pointer, if it matches a provided <c>HRESULT</c> value.
    /// </summary>
    /// <param name="thisPtr">Pointer to the language error info COM object.</param>
    /// <param name="hresult">The HRESULT associated with the error.</param>
    /// <param name="exception">The managed <see cref="Exception"/> if found, or <see langword="null"/>.</param>
    /// <returns>Whether <paramref name="exception"/> was successfully retrieved.</returns>
    private static bool TryGetLanguageException(void* thisPtr, HRESULT hresult, [NotNullWhen(true)] out Exception? exception)
    {
        void* languageExceptionPtr;

        // If we fail to get the original language exception, stop here
        if (ILanguageExceptionErrorInfoVftbl.GetLanguageExceptionUnsafe(thisPtr, &languageExceptionPtr).Failed())
        {
            exception = null;

            return false;
        }

        try
        {
            // Try to get the managed object for the language exception. This method assumes that the
            // object should be some CCW to an 'Exception' object we previously stored from somewhere.
            if (!WindowsRuntimeMarshal.TryGetManagedObject(languageExceptionPtr, out object? exceptionObject))
            {
                exception = null;

                return false;
            }

            // The CCW we unwrapped should always be for some 'Exception' object
            Exception retrievedException = (Exception)exceptionObject;

            // Make sure that the mapped 'HRESULT' value matches and is the one we're looking for
            if (RestrictedErrorInfo.GetHRForException(retrievedException) == hresult)
            {
                exception = retrievedException;

                return true;
            }
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(languageExceptionPtr);
        }

        exception = null;

        return false;
    }
}