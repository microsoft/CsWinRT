// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

#pragma warning disable IDE0051 // TODO

internal static unsafe class ExceptionHelpers
{
    public static unsafe WindowsRuntimeObjectReferenceValue BorrowRestrictedErrorInfo()
    {
        if (WindowsRuntimeImports.GetRestrictedErrorInfo == null)
        {
            return default;
        }

        void* restrictedErrorInfoPtr;
        Marshal.ThrowExceptionForHR(WindowsRuntimeImports.GetRestrictedErrorInfo(&restrictedErrorInfoPtr));
        if (restrictedErrorInfoPtr == null)
        {
            return default;
        }

        if (WindowsRuntimeImports.SetRestrictedErrorInfo != null)
        {
            Marshal.ThrowExceptionForHR(WindowsRuntimeImports.SetRestrictedErrorInfo(restrictedErrorInfoPtr));
        }

        return new WindowsRuntimeObjectReferenceValue(restrictedErrorInfoPtr);
    }

    // This is a helper method specifically to be used by exception propagation scenarios where we carefully
    // manage the lifetime of the CCW for the exception object to avoid cycles and thereby leaking it.
    internal static unsafe Exception? GetLanguageException(void* languageErrorInfoPtr, int hr)
    {
        // Check the error info first for the language exception.
        Exception? exception = GetLanguageExceptionInternal(languageErrorInfoPtr, hr);
        if (exception is not null)
        {
            return exception;
        }

        // If propagated exceptions are supported, traverse it and check if any one of those is our exception to reuse.
        if (Marshal.QueryInterface((nint)languageErrorInfoPtr, WellKnownInterfaceIds.IID_ILanguageExceptionErrorInfo2, out nint languageErrorInfo2Ptr) >= 0)
        {
            void* currentLanguageExceptionErrorInfo2Ptr = null;
            try
            {
                if (WellKnownErrorCodes.Succeeded(ILanguageExceptionErrorInfo2Vftbl.GetPropagationContextHeadUnsafe((void*)languageErrorInfo2Ptr, &currentLanguageExceptionErrorInfo2Ptr)))
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
                            _ = Marshal.Release((nint)previousLanguageExceptionErrorInfo2Ptr);
                        }
                    }
                }
            }
            finally
            {
                _ = Marshal.Release((nint)currentLanguageExceptionErrorInfo2Ptr);
                _ = Marshal.Release(languageErrorInfo2Ptr);
            }
        }

        return null;
    }

    private static unsafe Exception? GetLanguageExceptionInternal(void* languageErrorInfoPtr, int hr)
    {
        if (languageErrorInfoPtr == null)
        {
            return null;
        }
        void* languageExceptionPtr = null;
        try
        {
            if (WellKnownErrorCodes.Succeeded(ILanguageExceptionErrorInfoVftbl.GetLanguageExceptionUnsafe(languageErrorInfoPtr, &languageExceptionPtr)) && languageExceptionPtr != null)
            {
                if (WindowsRuntimeMarshal.IsReferenceToManagedObject(languageExceptionPtr))
                {
                    Exception exception = ComInterfaceDispatch.GetInstance<Exception>((ComInterfaceDispatch*)languageExceptionPtr);
                    if (RestrictedErrorInfo.GetHRForException(exception) == hr)
                    {
                        return exception;
                    }
                }
            }
        }
        finally
        {
            if (languageExceptionPtr != null)
            {
                _ = Marshal.Release((nint)languageExceptionPtr);
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
}

internal static class ExceptionExtensions
{
    public static bool TryGetRestrictedLanguageErrorInfo(
        this Exception ex,
        out WindowsRuntimeObjectReference? restrictedErrorObject,
        out bool isLanguageException)
    {
        restrictedErrorObject = null;
        isLanguageException = false;

        IDictionary dict = ex.Data;
        if (dict != null)
        {
            if (dict.Contains("__RestrictedErrorObjectReference"))
            {
                restrictedErrorObject = dict["__RestrictedErrorObjectReference"] as WindowsRuntimeObjectReference;
            }

            if (dict.Contains("__HasRestrictedLanguageErrorObject"))
            {
                isLanguageException = (bool)dict["__HasRestrictedLanguageErrorObject"]!;
            }

            return restrictedErrorObject is not null;
        }

        return false;
    }

    public static void SetHResult(this Exception ex, int value)
    {
        ex.HResult = value;
    }
}
