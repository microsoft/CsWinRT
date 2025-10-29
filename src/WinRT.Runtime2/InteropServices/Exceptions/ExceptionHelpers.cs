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
    /// <returns>
    /// A <see cref="WindowsRuntimeObjectReferenceValue"/> representing the restricted error info object,
    /// or <c>default</c> if none is available.
    /// </returns>
    public static WindowsRuntimeObjectReferenceValue BorrowRestrictedErrorInfo()
    {
        void* restrictedErrorInfoPtr;
        WindowsRuntimeImports.GetRestrictedErrorInfo(&restrictedErrorInfoPtr).Assert();

        if (restrictedErrorInfoPtr == null)
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
    /// <returns>
    /// The managed <see cref="Exception"/> if found; otherwise, <c>null</c>.
    /// </returns>
    public static Exception? GetLanguageException(void* languageErrorInfoPtr, HRESULT hresult)
    {
        // Check the error info first for the language exception.
        Exception? exception = GetLanguageExceptionInternal(languageErrorInfoPtr, hresult);
        if (exception is not null)
        {
            return exception;
        }

        // If propagated exceptions are supported, traverse it and check if any one of those is our exception to reuse.
        if (WellKnownErrorCodes.Succeeded(IUnknownVftbl.QueryInterfaceUnsafe(
                            languageErrorInfoPtr,
                            in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo2,
                            out void* languageErrorInfo2Ptr)))
        {
            void* currentLanguageExceptionErrorInfo2Ptr = null;

            // TODO: Potential double free currentLanguageExceptionErrorInfo2Ptr if GetPreviousLanguageExceptionErrorInfoUnsafe fails on. Find a better way to handle this.
            try
            {
                if (WellKnownErrorCodes.Succeeded(ILanguageExceptionErrorInfo2Vftbl.GetPropagationContextHeadUnsafe(languageErrorInfo2Ptr, &currentLanguageExceptionErrorInfo2Ptr)))
                {

                    while (currentLanguageExceptionErrorInfo2Ptr != null)
                    {
                        Exception? propagatedException = GetLanguageExceptionInternal(currentLanguageExceptionErrorInfo2Ptr, hresult);
                        if (propagatedException is not null)
                        {
                            return propagatedException;
                        }

                        void* previousLanguageExceptionErrorInfo2Ptr = currentLanguageExceptionErrorInfo2Ptr;
                        try
                        {
                            _ = ILanguageExceptionErrorInfo2Vftbl.GetPreviousLanguageExceptionErrorInfoUnsafe(currentLanguageExceptionErrorInfo2Ptr, &currentLanguageExceptionErrorInfo2Ptr);
                        }
                        finally
                        {
                            WindowsRuntimeObjectMarshaller.Free(previousLanguageExceptionErrorInfo2Ptr);
                        }
                    }
                }
            }
            finally
            {
                WindowsRuntimeObjectMarshaller.Free(currentLanguageExceptionErrorInfo2Ptr);
                WindowsRuntimeObjectMarshaller.Free(languageErrorInfo2Ptr);
            }
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
        if (languageErrorInfoPtr == null)
        {
            return null;
        }

        void* languageExceptionPtr = null;
        try
        {
            if (WellKnownErrorCodes.Succeeded(ILanguageExceptionErrorInfoVftbl.GetLanguageExceptionUnsafe(languageErrorInfoPtr, &languageExceptionPtr)))
            {
                if (WindowsRuntimeMarshal.TryGetManagedObject(languageExceptionPtr, out object? exceptionObject))
                {
                    Exception? exception = exceptionObject as Exception;
                    if (RestrictedErrorInfo.GetHRForException(exception) == hresult)
                    {
                        return exception;
                    }
                }
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

        if (exceptionData != null)
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
            if (description != null)
            {
                exceptionData[WellKnownExceptionDataKeys.Description] = description;
            }

            if (restrictedError != null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedDescription] = restrictedError;
            }

            if (restrictedErrorReference != null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedErrorReference] = restrictedErrorReference;
            }

            if (restrictedCapabilitySid != null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedCapabilitySid] = restrictedCapabilitySid;
            }

            // Propagate the 'IRestrictedErrorInfo' object reference, so we can restore it later to forward the exception info
            exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference] = restrictedErrorObject;
            exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject] = hasRestrictedLanguageErrorObject;

            if (internalGetGlobalErrorStateException != null)
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