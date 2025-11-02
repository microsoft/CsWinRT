// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides helper methods for working with restricted error information and language-specific exceptions in Windows Runtime interop scenarios.
/// </summary>
/// <remarks>
/// These methods manage COM pointers, propagate language exceptions, and attach
/// restricted error info to managed exceptions for diagnostic and interop purposes.
/// </remarks>
internal static unsafe class RestrictedErrorInfoHelpers
{
    /// <summary>
    /// Retrieves the current restricted error info object and sets it for propagation if available.
    /// </summary>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> representing the restricted error info object.</returns>
    public static WindowsRuntimeObjectReferenceValue BorrowErrorInfo()
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
    /// <param name="exception">The <see cref="Exception"/> instance to augment.</param>
    /// <param name="restrictedErrorObject">The restricted error info object reference.</param>
    /// <param name="hasRestrictedLanguageErrorObject">Indicates whether a language-specific error object exists.</param>
    public static void AddExceptionData(
        Exception exception,
        WindowsRuntimeObjectReference restrictedErrorObject,
        bool hasRestrictedLanguageErrorObject)
    {
        IDictionary? exceptionData = exception.Data;

        if (exceptionData is not null)
        {
            // Keep the error object alive so that users could retrieve error information later from the exception data
            exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference] = restrictedErrorObject;
            exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject] = hasRestrictedLanguageErrorObject;
        }
    }

    /// <summary>
    /// Adds restricted error info metadata to the <see cref="Exception.Data"/> dictionary.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> instance to augment.</param>
    /// <param name="description">The human-readable error description (may be empty).</param>
    /// <param name="restrictedDescription">The restricted error description (may be empty).</param>
    /// <param name="restrictedErrorReference">The reference string (source) associated with the error from an <c>IRestrictedErrorInfo</c> instance.</param>
    /// <param name="restrictedCapabilitySid">The capability SID (may be empty).</param>
    /// <param name="restrictedErrorObject">The restricted error info object reference.</param>
    /// <param name="hasRestrictedLanguageErrorObject">Indicates whether a language-specific error object exists.</param>
    /// <param name="internalGetGlobalErrorStateException">The internal exception (if any) that was thrown while trying to retrieve the global exception.</param>
    public static void AddExceptionData(
        Exception exception,
        string? description,
        string? restrictedDescription,
        string? restrictedErrorReference,
        string? restrictedCapabilitySid,
        WindowsRuntimeObjectReference? restrictedErrorObject,
        bool hasRestrictedLanguageErrorObject,
        Exception? internalGetGlobalErrorStateException)
    {
        IDictionary? exceptionData = exception.Data;

        if (exceptionData is not null)
        {
            if (description is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.Description] = description;
            }

            if (restrictedDescription is not null)
            {
                exceptionData[WellKnownExceptionDataKeys.RestrictedDescription] = restrictedDescription;
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
    public static bool TryGetErrorInfo(
        Exception exception,
        [NotNullWhen(true)] out WindowsRuntimeObjectReference? restrictedErrorObject,
        out bool isLanguageException)
    {
        IDictionary? exceptionData = exception.Data;

        if (exceptionData is not null)
        {
            restrictedErrorObject = exceptionData.Contains(WellKnownExceptionDataKeys.RestrictedErrorObjectReference)
                ? (WindowsRuntimeObjectReference?)exceptionData[WellKnownExceptionDataKeys.RestrictedErrorObjectReference]
                : null;

            isLanguageException =
                exceptionData.Contains(WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject) &&
                (bool)exceptionData[WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject]!;

            return restrictedErrorObject is not null;
        }

        restrictedErrorObject = null;
        isLanguageException = false;

        return false;
    }
}