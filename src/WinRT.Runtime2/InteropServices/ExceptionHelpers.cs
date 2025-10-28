// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

#pragma warning disable IDE0051 // TODO

internal static unsafe class ExceptionHelpers
{
    internal static unsafe WindowsRuntimeObjectReferenceValue BorrowRestrictedErrorInfo()
    {
        if (WindowsRuntimeImports.GetRestrictedErrorInfo == null)
        {
            return default;
        }

        void* restrictedErrorInfoPtr;
        WindowsRuntimeImports.GetRestrictedErrorInfo(&restrictedErrorInfoPtr).Assert();
        if (restrictedErrorInfoPtr == null)
        {
            return default;
        }

        if (WindowsRuntimeImports.SetRestrictedErrorInfo != null)
        {
            WindowsRuntimeImports.SetRestrictedErrorInfo(restrictedErrorInfoPtr).Assert();
        }

        return new(restrictedErrorInfoPtr);
    }

    // This is a helper method specifically to be used by exception propagation scenarios where we carefully
    // manage the lifetime of the CCW for the exception object to avoid cycles and thereby leaking it.
    internal static unsafe Exception? GetLanguageException(void* languageErrorInfoPtr, HRESULT hr)
    {
        // Check the error info first for the language exception.
        Exception? exception = GetLanguageExceptionInternal(languageErrorInfoPtr, hr);
        if (exception is not null)
        {
            return exception;
        }

        // If propagated exceptions are supported, traverse it and check if any one of those is our exception to reuse.
        if (WellKnownErrorCodes.Succeeded(IUnknownVftbl.QueryInterfaceUnsafe(
                            languageErrorInfoPtr,
                            in WellKnownInterfaceIds.IID_ILanguageExceptionErrorInfo2,
                            out void* languageErrorInfo2Ptr)))
        {
            void* currentLanguageExceptionErrorInfo2Ptr = null;
            try
            {
                if (WellKnownErrorCodes.Succeeded(ILanguageExceptionErrorInfo2Vftbl.GetPropagationContextHeadUnsafe(languageErrorInfo2Ptr, &currentLanguageExceptionErrorInfo2Ptr)))
                {
                    while (currentLanguageExceptionErrorInfo2Ptr != null)
                    {
                        Exception? propagatedException = GetLanguageExceptionInternal(currentLanguageExceptionErrorInfo2Ptr, hr);
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

    internal static unsafe Exception? GetLanguageExceptionInternal(void* languageErrorInfoPtr, HRESULT hr)
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
                if (WindowsRuntimeMarshal.TryGetManagedObject(languageExceptionPtr, out object? exception))
                {
                    if (RestrictedErrorInfo.GetHRForException((Exception)exception) == hr)
                    {
                        return (Exception)exception;
                    }
                }
            }
        }
        finally
        {
            if (languageExceptionPtr != null)
            {
                WindowsRuntimeObjectMarshaller.Free(languageExceptionPtr);
            }
        }
        return null;
    }

    internal static void AddExceptionDataForRestrictedErrorInfo(
        this Exception ex,
        WindowsRuntimeObjectReference restrictedErrorObject,
        bool hasRestrictedLanguageErrorObject)
    {
        IDictionary dict = ex.Data;
        if (dict != null)
        {
            // Keep the error object alive so that user could retrieve error information
            // using Data["RestrictedErrorReference"]
            dict["__RestrictedErrorObjectReference"] = restrictedErrorObject;
            dict["__HasRestrictedLanguageErrorObject"] = hasRestrictedLanguageErrorObject;
        }
    }

    internal static bool TryGetRestrictedLanguageErrorInfo(
        this Exception exception,
        out WindowsRuntimeObjectReference? restrictedErrorObject,
        out bool isLanguageException)
    {
        restrictedErrorObject = null;
        isLanguageException = false;

        IDictionary dictionary = exception.Data;
        if (dictionary != null)
        {
            if (dictionary.Contains("__RestrictedErrorObjectReference"))
            {
                restrictedErrorObject = dictionary["__RestrictedErrorObjectReference"] as WindowsRuntimeObjectReference;
            }

            if (dictionary.Contains("__HasRestrictedLanguageErrorObject"))
            {
                isLanguageException = (bool)dictionary["__HasRestrictedLanguageErrorObject"]!;
            }

            return restrictedErrorObject is not null;
        }

        return false;
    }
}
