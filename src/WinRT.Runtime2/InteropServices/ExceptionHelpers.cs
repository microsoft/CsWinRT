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
    internal const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);
    internal const int COR_E_OPERATIONCANCELED = unchecked((int)0x8013153b);
    internal const int COR_E_ARGUMENTOUTOFRANGE = unchecked((int)0x80131502);
    internal const int COR_E_INDEXOUTOFRANGE = unchecked((int)0x80131508);
    internal const int COR_E_TIMEOUT = unchecked((int)0x80131505);
    internal const int COR_E_INVALIDOPERATION = unchecked((int)0x80131509);
    internal const int RO_E_CLOSED = unchecked((int)0x80000013);
    internal const int E_BOUNDS = unchecked((int)0x8000000b);
    internal const int E_CHANGED_STATE = unchecked((int)0x8000000c);
    internal const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000d);
    internal const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000e);
    internal const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);
    internal const int APPMODEL_ERROR_NO_PACKAGE = unchecked((int)0x80073D54);
    internal const int E_XAMLPARSEFAILED = unchecked((int)0x802B000A);
    internal const int E_LAYOUTCYCLE = unchecked((int)0x802B0014);
    internal const int E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);
    internal const int E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);
    internal const int ERROR_INVALID_WINDOW_HANDLE = unchecked((int)0x80070578);
    internal const int E_POINTER = unchecked((int)0x80004003);
    internal const int E_NOTIMPL = unchecked((int)0x80004001);
    internal const int E_ACCESSDENIED = unchecked((int)0x80070005);
    internal const int E_INVALIDARG = unchecked((int)0x80070057);
    internal const int E_NOINTERFACE = unchecked((int)0x80004002);
    internal const int E_OUTOFMEMORY = unchecked((int)0x8007000e);
    internal const int E_NOTSUPPORTED = unchecked((int)0x80070032);
    internal const int ERROR_ARITHMETIC_OVERFLOW = unchecked((int)0x80070216);
    internal const int ERROR_FILENAME_EXCED_RANGE = unchecked((int)0x800700ce);
    internal const int ERROR_FILE_NOT_FOUND = unchecked((int)0x80070002);
    internal const int ERROR_HANDLE_EOF = unchecked((int)0x80070026);
    internal const int ERROR_PATH_NOT_FOUND = unchecked((int)0x80070003);
    internal const int ERROR_STACK_OVERFLOW = unchecked((int)0x800703e9);
    internal const int ERROR_BAD_FORMAT = unchecked((int)0x8007000b);
    internal const int ERROR_CANCELLED = unchecked((int)0x800704c7);
    internal const int ERROR_TIMEOUT = unchecked((int)0x800705b4);
    internal const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

    public static unsafe delegate* unmanaged[Stdcall]<void**, int> getRestrictedErrorInfo;
    public static unsafe delegate* unmanaged[Stdcall]<void*, int> setRestrictedErrorInfo;
    public static unsafe delegate* unmanaged[Stdcall]<int, void*, void*, int> roOriginateLanguageException;
    public static unsafe delegate* unmanaged[Stdcall]<void*, int> roReportUnhandledError;

    private static unsafe bool Initialize()
    {
        void* winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-1.dll", null, (uint)DllImportSearchPath.System32);
        if (winRTErrorModule != null)
        {
#if NET7_0_OR_GREATER || CsWinRT_LANG_11_FEATURES
            ReadOnlySpan<byte> langExceptionString = "RoOriginateLanguageException"u8;
            ReadOnlySpan<byte> reportUnhandledErrorString = "RoReportUnhandledError"u8;
#else
                ReadOnlySpan<byte> langExceptionString = Encoding.ASCII.GetBytes("RoOriginateLanguageException");
                ReadOnlySpan<byte> reportUnhandledErrorString = Encoding.ASCII.GetBytes("RoReportUnhandledError");
#endif

            roOriginateLanguageException = (delegate* unmanaged[Stdcall]<int, void*, void*, int>)Platform.GetProcAddress(winRTErrorModule, langExceptionString);
            roReportUnhandledError = (delegate* unmanaged[Stdcall]<void*, int>)Platform.GetProcAddress(winRTErrorModule, reportUnhandledErrorString);
        }
        else
        {
            winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-0.dll", null, (uint)DllImportSearchPath.System32);
        }

        if (winRTErrorModule != null)
        {
#if NET7_0_OR_GREATER || CsWinRT_LANG_11_FEATURES
            ReadOnlySpan<byte> getRestrictedErrorInfoFuncName = "GetRestrictedErrorInfo"u8;
            ReadOnlySpan<byte> setRestrictedErrorInfoFuncName = "SetRestrictedErrorInfo"u8;
#else
                ReadOnlySpan<byte> getRestrictedErrorInfoFuncName = Encoding.ASCII.GetBytes("GetRestrictedErrorInfo");
                ReadOnlySpan<byte> setRestrictedErrorInfoFuncName = Encoding.ASCII.GetBytes("SetRestrictedErrorInfo");
#endif
            getRestrictedErrorInfo = (delegate* unmanaged[Stdcall]<void**, int>)Platform.GetProcAddress(winRTErrorModule, getRestrictedErrorInfoFuncName);
            setRestrictedErrorInfo = (delegate* unmanaged[Stdcall]<void*, int>)Platform.GetProcAddress(winRTErrorModule, setRestrictedErrorInfoFuncName);
        }

        return true;
    }

    public static unsafe WindowsRuntimeObjectReferenceValue BorrowRestrictedErrorInfo()
    {
        if (getRestrictedErrorInfo == null)
        {
            return default;
        }

        void* restrictedErrorInfoPtr;
        Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(&restrictedErrorInfoPtr));
        if (restrictedErrorInfoPtr == null)
        {
            return default;
        }

        if (setRestrictedErrorInfo != null)
        {
            Marshal.ThrowExceptionForHR(setRestrictedErrorInfo(restrictedErrorInfoPtr));
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
                    = ILanguageExceptionErrorInfo2Methods.GetPropagationContextHead((void*)languageErrorInfo2Ptr);
                while (currentLanguageExceptionErrorInfo2Ptr != default)
                {
                    Exception? propagatedException = GetLanguageExceptionInternal(currentLanguageExceptionErrorInfo2Ptr, hr);
                    if (propagatedException is not null)
                    {
                        return propagatedException;
                    }

                    void* previousLanguageExceptionErrorInfo2Ptr = currentLanguageExceptionErrorInfo2Ptr;
                    currentLanguageExceptionErrorInfo2Ptr = ILanguageExceptionErrorInfo2Methods.GetPreviousLanguageExceptionErrorInfo(currentLanguageExceptionErrorInfo2Ptr);
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
        void* languageExceptionPtr = ILanguageExceptionErrorInfoMethods.GetLanguageException(languageErrorInfoPtr);
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
            COR_E_OBJECTDISPOSED => RO_E_CLOSED,
            COR_E_OPERATIONCANCELED => ERROR_CANCELLED,
            COR_E_ARGUMENTOUTOFRANGE or COR_E_INDEXOUTOFRANGE => E_BOUNDS,
            COR_E_TIMEOUT => ERROR_TIMEOUT,
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
#if NET
            dict["__RestrictedErrorObjectReference"] = restrictedErrorObject;
#else
                dict["__RestrictedErrorObjectReference"] = restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject);
#endif
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
#if NET
            dict["__RestrictedErrorObjectReference"] = restrictedErrorObject;
#else
                dict["__RestrictedErrorObjectReference"] = restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject);
#endif
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
#if NET
                restrictedErrorObject = dict["__RestrictedErrorObjectReference"] as WindowsRuntimeObjectReference;
#else
                restrictedErrorObject = ((__RestrictedErrorObject)dict["__RestrictedErrorObjectReference"])?.RealErrorObject;
#endif
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
#if !NET
            ex.GetType().GetProperty("HResult").SetValue(ex, value);
#else
        ex.HResult = value;
#endif
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

#if !NET
        public static unsafe ref readonly char GetPinnableReference(this string str)
        {
            fixed (char* p = str)
            {
                return ref *p;
            }
        }
#endif
}

// Handcrafted P/Invoke with TFM-specific handling, or thin high-level abstractions (eg. 'TryGetProcAddress'/'GetProcAddress')
internal partial class Platform
{
    public static unsafe void* TryGetProcAddress(void* moduleHandle, ReadOnlySpan<byte> functionName)
    {
        fixed (byte* lpFunctionName = functionName)
        {
#if NET8_0_OR_GREATER
            return LibraryImportStubs.GetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
#else
                return GetProcAddress(moduleHandle, (sbyte*)lpFunctionName);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern unsafe void* GetProcAddress(void* nativeModuleHandle, sbyte* nativeFunctionName);
#endif
        }
    }

    public static unsafe void* GetProcAddress(void* moduleHandle, ReadOnlySpan<byte> functionName)
    {
        void* functionPtr = TryGetProcAddress(moduleHandle, functionName);

        if (functionPtr == null)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        return functionPtr;
    }

    public static unsafe void* LoadLibraryExW(string fileName, void* fileHandle, uint flags)
    {
        fixed (char* lpFileName = fileName)
        {
#if NET8_0_OR_GREATER
            return LibraryImportStubs.LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);
#else
                return LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);

                [DllImport("kernel32.dll", SetLastError = true)]
                static unsafe extern void* LoadLibraryExW(ushort* fileName, void* fileHandle, uint flags);
#endif
        }
    }
}

#if NET8_0_OR_GREATER
// Marshalling stubs from [LibraryImport], which are used to get the same semantics (eg. for setting
// the last P/Invoke errors, etc.) on .NET 6 as well ([LibraryImport] was only introduced in .NET 7).
internal static class LibraryImportStubs
{
    public static unsafe void* GetProcAddress(void* moduleHandle, sbyte* functionName)
    {
        int lastError;
        void* returnValue;
        {
            Marshal.SetLastSystemError(0);
            returnValue = PInvoke(moduleHandle, functionName);
            lastError = Marshal.GetLastSystemError();
        }

        Marshal.SetLastPInvokeError(lastError);
        return returnValue;

        // Local P/Invoke
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
        static extern unsafe void* PInvoke(void* nativeModuleHandle, sbyte* nativeFunctionName);
    }

    public static unsafe void* LoadLibraryExW(ushort* fileName, void* fileHandle, uint flags)
    {
        int lastError;
        void* returnValue;
        {
            Marshal.SetLastSystemError(0);
            returnValue = PInvoke(fileName, fileHandle, flags);
            lastError = Marshal.GetLastSystemError();
        }

        Marshal.SetLastPInvokeError(lastError);
        return returnValue;

        // Local P/Invoke
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", ExactSpelling = true)]
        static extern unsafe void* PInvoke(ushort* nativeFileName, void* nativeFileHandle, uint nativeFlags);
    }
}
#endif