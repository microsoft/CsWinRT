// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

#pragma warning disable IDE0051 // TODO

/// <summary>
/// Provides helper methods for working with restricted error information and language-specific exceptions
/// in Windows Runtime interop scenarios.
/// </summary>
/// <remarks>
/// These methods manage COM pointers, propagate language exceptions, and attach restricted error info
/// to managed exceptions for diagnostic and interop purposes.
/// </remarks>
internal static unsafe class ExceptionHelpers
{
    /// <summary>
    /// Retrieves the current restricted error info object and sets it for propagation if available.
    /// </summary>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> representing the restricted error info object.</returns>
    public static WindowsRuntimeObjectReferenceValue BorrowRestrictedErrorInfo()
    {
        void* restrictedErrorInfoPtr;

        WindowsRuntimeImports.GetRestrictedErrorInfo(&restrictedErrorInfoPtr).Assert();

        if (restrictedErrorInfoPtr is null)
        {
            return default;
        }

        WindowsRuntimeImports.SetRestrictedErrorInfo(restrictedErrorInfoPtr).Assert();

        return new(restrictedErrorInfoPtr);
    }

    /// <summary>
    /// Attempts to retrieve a managed language exception from a restricted error info pointer.
    /// This is a helper method specifically to be used by exception propagation scenarios where we carefully
    /// manage the lifetime of the CCW for the exception object to avoid cycles and thereby leaking it.
    /// </summary>
    /// <param name="languageErrorInfoPtr">Pointer to the language error info COM object.</param>
    /// <param name="hresult">The HRESULT associated with the error.</param>
    /// <returns>The managed <see cref="Exception"/> if found, or <see langword="null"/>.</returns>
    public static Exception? GetLanguageException(void* languageErrorInfoPtr, HRESULT hresult)
    {
        // Check the error info first for the language exception
        if (GetLanguageExceptionInternal(languageErrorInfoPtr, hresult) is { } exception)
        {
            return exception;
        }

        // Check if propagated exceptions are supported, and stop if they're not
        if (IUnknownVftbl.QueryInterfaceUnsafe(
            thisPtr: languageErrorInfoPtr,
            iid: in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo2,
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
                if (GetLanguageExceptionInternal(currentLanguageExceptionErrorInfo2Ptr, hresult) is { } propagatedException)
                {
                    return propagatedException;
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
    /// Internal helper to retrieve a managed language exception from a COM pointer.
    /// </summary>
    /// <param name="languageErrorInfoPtr">Pointer to the language error info COM object.</param>
    /// <param name="hresult">The HRESULT associated with the error.</param>
    /// <returns>
    /// The managed <see cref="Exception"/> if found; otherwise, <c>null</c>.
    /// </returns>
    private static unsafe Exception? GetLanguageExceptionInternal(void* languageErrorInfoPtr, HRESULT hresult)
    {
        void* languageExceptionPtr;

        // If we fail to get the original language exception, stop here
        if (ILanguageExceptionErrorInfoVftbl.GetLanguageExceptionUnsafe(languageErrorInfoPtr, &languageExceptionPtr).Failed())
        {
            return null;
        }

        try
        {
            // Try to get the managed object for the language exception. This method assumes that the
            // object should be some CCW to an 'Exception' object we previously stored from somewhere.
            if (!WindowsRuntimeMarshal.TryGetManagedObject(languageExceptionPtr, out object? exceptionObject))
            {
                return null;
            }

            // The CCW we unwrapped should always be for some 'Exception' object
            Exception exception = (Exception)exceptionObject;

            // Make sure that the mapped 'HRESULT' value matches and is the one we're looking for
            if (RestrictedErrorInfo.GetHRForException(exception) == hresult)
            {
                return exception;
            }
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(languageExceptionPtr);
        }

        return null;
    }


    /// <summary>
    /// Adds restricted error info metadata to the <see cref="Exception.Data"/> dictionary.
    /// </summary>
    /// <param name="exception">The exception to augment.</param>
    /// <param name="restrictedErrorObject">The restricted error info object reference.</param>
    /// <param name="hasRestrictedLanguageErrorObject">Indicates whether a language-specific error object exists.</param>
    public static void AddExceptionDataForRestrictedErrorInfo(
        Exception exception,
        WindowsRuntimeObjectReference restrictedErrorObject,
        bool hasRestrictedLanguageErrorObject)
    {
        IDictionary exceptionData = exception.Data;

        if (exceptionData is not null)
        {
            // Keep the error object alive so that user could retrieve error information
            // using Data["RestrictedErrorReference"]
            exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference] = restrictedErrorObject;
            exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject] = hasRestrictedLanguageErrorObject;
        }
    }

    internal static void AddExceptionDataForRestrictedErrorInfo(
        Exception exception,
        string? description,
        string? restrictedError,
        string? restrictedErrorReference,
        string? restrictedCapabilitySid,
        WindowsRuntimeObjectReference? restrictedErrorObject,
        bool hasRestrictedLanguageErrorObject = false,
        Exception? internalGetGlobalErrorStateException = null)
    {
        IDictionary? exceptionData = exception.Data;

        if (exceptionData is not null)
        {
            if (description is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.Description] = description;
            }

            if (restrictedError is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedDescription] = restrictedError;
            }

            if (restrictedErrorReference is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedErrorReference] = restrictedErrorReference;
            }

            if (restrictedCapabilitySid is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedCapabilitySid] = restrictedCapabilitySid;
            }

            // Propagate the 'IRestrictedErrorInfo' object reference, so we can restore it later to forward the exception info
            exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference] = restrictedErrorObject;
            exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject] = hasRestrictedLanguageErrorObject;

            if (internalGetGlobalErrorStateException is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.InternalCsWinRTException] = internalGetGlobalErrorStateException;
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve restricted error info metadata from an exception.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <param name="restrictedErrorObject">On return, the restricted error info object reference if present.</param>
    /// <param name="isLanguageException">On return, indicates whether the error originated from a language exception.</param>
    /// <returns><c>true</c> if restricted error info was found; otherwise, <c>false</c>.</returns>
    public static bool TryGetRestrictedLanguageErrorInfo(
        Exception exception,
        out WindowsRuntimeObjectReference? restrictedErrorObject,
        out bool isLanguageException)
    {
        restrictedErrorObject = null;
        isLanguageException = false;

        IDictionary? exceptionData = exception.Data;

        if (exceptionData is not null)
        {
            if (exceptionData.Contains(WellKnownExceptionDataKeys.RestrictedErrorObjectReference))
            {
                restrictedErrorObject = exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference] as WindowsRuntimeObjectReference;
            }

            if (exceptionData.Contains(WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject))
            {
                isLanguageException = (bool)exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject]!;
            }

            return restrictedErrorObject is not null;
        }

        return false;
    }
}