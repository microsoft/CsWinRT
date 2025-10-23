// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
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
            void* currentLanguageExceptionErrorInfo2Ptr = default;
            try
            {
                currentLanguageExceptionErrorInfo2Ptr
                    = ILanguageExceptionErrorInfo2Vftbl.GetPropagationContextHeadUnsafe((void*)languageErrorInfo2Ptr);
                while (currentLanguageExceptionErrorInfo2Ptr != default)
                {
                    Exception? propagatedException = GetLanguageExceptionInternal(currentLanguageExceptionErrorInfo2Ptr, hr);
                    if (propagatedException is not null)
                    {
                        return propagatedException;
                    }

                    void* previousLanguageExceptionErrorInfo2Ptr = currentLanguageExceptionErrorInfo2Ptr;
                    currentLanguageExceptionErrorInfo2Ptr = ILanguageExceptionErrorInfo2Vftbl.GetPreviousLanguageExceptionErrorInfoUnsafe(currentLanguageExceptionErrorInfo2Ptr);
                    _ = Marshal.Release((nint)previousLanguageExceptionErrorInfo2Ptr);
                }
            }
            finally
            {
                MarshalExtensions.ReleaseIfNotNull(currentLanguageExceptionErrorInfo2Ptr);
                _ = Marshal.Release(languageErrorInfo2Ptr);
            }
        }

        return null;
    }

    private static unsafe Exception? GetLanguageExceptionInternal(void* languageErrorInfoPtr, int hr)
    {
        void* languageExceptionPtr = ILanguageExceptionErrorInfoVftbl.GetLanguageExceptionUnsafe(languageErrorInfoPtr);
        if (languageExceptionPtr != default)
        {
            try
            {
                if (WindowsRuntimeMarshal.IsReferenceToManagedObject(languageExceptionPtr))
                {
                    Exception ex = ComInterfaceDispatch.GetInstance<Exception>((ComInterfaceDispatch*)languageExceptionPtr);
                    //var ex = ComWrappersSupport.FindObject<Exception>(languageExceptionPtr);
                    if (GetHRForException(ex) == hr)
                    {
                        return ex;
                    }
                }
            }
            finally
            {
                _ = Marshal.Release((nint)languageExceptionPtr);
            }
        }

        return null;
    }

    public static unsafe int GetHRForException(Exception ex)
    {
        int hr = ex.HResult;
        try
        {
            if (ex.TryGetRestrictedLanguageErrorInfo(out WindowsRuntimeObjectReference? restrictedErrorObject, out bool _))
            {
                if (restrictedErrorObject != null)
                {
                    IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorObject.GetThisPtrUnsafe(), out hr);
                    GC.KeepAlive(restrictedErrorObject);
                }
            }
        }
        catch (Exception e)
        {
            // If we fail to get the hresult from the error info, we fallback to the exception hresult.
            Debug.Assert(false, e.Message, e.StackTrace);
        }

        return hr switch
        {
            WellKnownErrorCodes.COR_E_OBJECTDISPOSED => WellKnownErrorCodes.RO_E_CLOSED,
            WellKnownErrorCodes.COR_E_OPERATIONCANCELED => WellKnownErrorCodes.ERROR_CANCELLED,
            WellKnownErrorCodes.COR_E_ARGUMENTOUTOFRANGE or WellKnownErrorCodes.COR_E_INDEXOUTOFRANGE => WellKnownErrorCodes.E_BOUNDS,
            WellKnownErrorCodes.COR_E_TIMEOUT => WellKnownErrorCodes.ERROR_TIMEOUT,
            _ => hr,
        };
    }

    public static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            WindowsRuntimeObjectReference restrictedErrorObject,
            bool hasRestrictedLanguageErrorObject = false,
            Exception? internalGetGlobalErrorStateException = null)
    {
        IDictionary dict = ex.Data;
        if (dict != null)
        {
            dict["Description"] = description;
            dict["RestrictedDescription"] = restrictedError;
            dict["RestrictedErrorReference"] = restrictedErrorReference;
            dict["RestrictedCapabilitySid"] = restrictedCapabilitySid;

            // Keep the error object alive so that user could retrieve error information
            // using Data["RestrictedErrorReference"]
            dict["__RestrictedErrorObjectReference"] = restrictedErrorObject;
            dict["__HasRestrictedLanguageErrorObject"] = hasRestrictedLanguageErrorObject;

            if (internalGetGlobalErrorStateException != null)
            {
                dict["_InternalCsWinRTException"] = internalGetGlobalErrorStateException;
            }
        }
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

    public static Exception GetExceptionForHR(this Exception innerException, int hresult, string messageResource)
    {
        Exception e;
        if (innerException != null)
        {
            string message = innerException.Message ?? messageResource;
            e = new Exception(message, innerException);
        }
        else
        {
            e = new Exception(messageResource);
        }
        e.SetHResult(hresult);
        return e;
    }
}

internal static class MarshalExtensions
{
    /// <summary>
    /// Releases a COM object, if not <see langword="null"/>.
    /// </summary>
    /// <param name="pUnk">The input COM object to release.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ReleaseIfNotNull(void* pUnk)
    {
        if (pUnk == null)
        {
            return;
        }

        _ = ((delegate* unmanaged[Stdcall]<void*, int>)(*(*(void***)pUnk + 2 /* IUnknown.Release slot */)))(pUnk);
    }

    public static void Dispose(this GCHandle handle)
    {
        if (handle.IsAllocated)
        {
            handle.Free();
        }
    }
}
