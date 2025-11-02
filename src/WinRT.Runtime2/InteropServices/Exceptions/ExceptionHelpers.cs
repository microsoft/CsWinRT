// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;

namespace WindowsRuntime.InteropServices;

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
            // Keep the error object alive so that users could retrieve error information later from the exception data
            exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference] = restrictedErrorObject;
            exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject] = hasRestrictedLanguageErrorObject;
        }
    }

    public static void AddExceptionDataForRestrictedErrorInfo(
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
    /// <param name="exception">The <see cref="Exception"/> instance to inspect.</param>
    /// <param name="restrictedErrorObject">On return, the restricted error info object reference if present.</param>
    /// <param name="isLanguageException">On return, indicates whether the error originated from a language exception.</param>
    /// <returns>Whether the restricted error info was found.</returns>
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