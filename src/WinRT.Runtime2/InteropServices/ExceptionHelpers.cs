// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

#pragma warning disable IDE0051 // TODO

internal class ExceptionHelpers
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
        IntPtr winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-1.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
        if (winRTErrorModule != IntPtr.Zero)
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
            winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-0.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
        }

        if (winRTErrorModule != IntPtr.Zero)
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
}

internal static class ExceptionExtensions
{
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

    public static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            WindowsRuntimeObject restrictedErrorObject,
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
        WindowsRuntimeObject restrictedErrorObject,
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

    internal static bool TryGetRestrictedLanguageErrorInfo(
        this Exception ex,
        out WindowsRuntimeObject? restrictedErrorObject,
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
                restrictedErrorObject = dict["__RestrictedErrorObjectReference"] as WindowsRuntimeObject;
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
}

internal static partial class Platform
{
    [DllImport("api-ms-win-core-com-l1-1-0.dll")]
    public static extern unsafe int CoCreateInstance(Guid* clsid, IntPtr outer, uint clsContext, Guid* iid, IntPtr* instance);

    [DllImport("api-ms-win-core-com-l1-1-0.dll")]
    public static extern int CoDecrementMTAUsage(IntPtr cookie);

    [DllImport("api-ms-win-core-com-l1-1-0.dll")]
    public static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

    [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
    public static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, Guid* iid, IntPtr* factory);

    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern unsafe int WindowsCreateString(ushort* sourceString, int length, IntPtr* hstring);

    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern unsafe int WindowsCreateStringReference(
        ushort* sourceString,
        int length,
        IntPtr* hstring_header,
        IntPtr* hstring);

    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern int WindowsDeleteString(IntPtr hstring);

    [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

    [DllImport("api-ms-win-core-com-l1-1-1.dll", CallingConvention = CallingConvention.StdCall)]
    public static extern unsafe int RoGetAgileReference(uint options, Guid* iid, IntPtr unknown, IntPtr* agileReference);

    [DllImport("api-ms-win-core-com-l1-1-0.dll")]
    public static extern unsafe int CoGetContextToken(IntPtr* contextToken);

    [DllImport("api-ms-win-core-com-l1-1-0.dll")]
    public static extern unsafe int CoGetObjectContext(Guid* riid, IntPtr* ppv);

    [DllImport("oleaut32.dll")]
    public static extern int SetErrorInfo(uint dwReserved, IntPtr perrinfo);

    [DllImport("api-ms-win-core-com-l1-1-0.dll")]
    public static extern unsafe int CoCreateFreeThreadedMarshaler(IntPtr outer, IntPtr* marshalerPtr);

    [DllImport("kernel32.dll")]
    public static extern unsafe uint FormatMessageW(uint dwFlags, void* lpSource, uint dwMessageId, uint dwLanguageId, char** lpBuffer, uint nSize, void* pArguments);

    [DllImport("kernel32.dll")]
    public static extern unsafe void* LocalFree(void* hMem);
}

// Handcrafted P/Invoke with TFM-specific handling, or thin high-level abstractions (eg. 'TryGetProcAddress'/'GetProcAddress')
internal partial class Platform
{
    public static bool FreeLibrary(IntPtr moduleHandle)
    {
#if NET8_0_OR_GREATER
        return LibraryImportStubs.FreeLibrary(moduleHandle);
#else
            return FreeLibrary(moduleHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool FreeLibrary(IntPtr moduleHandle);
#endif
    }

    public static unsafe IntPtr TryGetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
    {
        fixed (byte* lpFunctionName = functionName)
        {
#if NET8_0_OR_GREATER
            return LibraryImportStubs.GetProcAddress(moduleHandle, (sbyte*)lpFunctionName);
#else
                return GetProcAddress(moduleHandle, (sbyte*)lpFunctionName);

                [DllImport("kernel32.dll", SetLastError = true)]
                static extern unsafe IntPtr GetProcAddress(IntPtr nativeModuleHandle, sbyte* nativeFunctionName);
#endif
        }
    }

    public static IntPtr GetProcAddress(IntPtr moduleHandle, ReadOnlySpan<byte> functionName)
    {
        IntPtr functionPtr = TryGetProcAddress(moduleHandle, functionName);

        if (functionPtr == IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
        }

        return functionPtr;
    }

    public static unsafe IntPtr LoadLibraryExW(string fileName, IntPtr fileHandle, uint flags)
    {
        fixed (char* lpFileName = fileName)
        {
#if NET8_0_OR_GREATER
            return LibraryImportStubs.LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);
#else
                return LoadLibraryExW((ushort*)lpFileName, fileHandle, flags);

                [DllImport("kernel32.dll", SetLastError = true)]
                static unsafe extern IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags);
#endif
        }
    }
}

#if NET8_0_OR_GREATER
// Marshalling stubs from [LibraryImport], which are used to get the same semantics (eg. for setting
// the last P/Invoke errors, etc.) on .NET 6 as well ([LibraryImport] was only introduced in .NET 7).
internal static class LibraryImportStubs
{
    public static bool FreeLibrary(IntPtr moduleHandle)
    {
        int lastError;
        bool returnValue;
        int nativeReturnValue;
        {
            Marshal.SetLastSystemError(0);
            nativeReturnValue = PInvoke(moduleHandle);
            lastError = Marshal.GetLastSystemError();
        }

        // Unmarshal - Convert native data to managed data.
        returnValue = nativeReturnValue != 0;
        Marshal.SetLastPInvokeError(lastError);
        return returnValue;

        // Local P/Invoke
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", ExactSpelling = true)]
        static extern unsafe int PInvoke(IntPtr nativeModuleHandle);
    }

    public static unsafe IntPtr GetProcAddress(IntPtr moduleHandle, sbyte* functionName)
    {
        int lastError;
        IntPtr returnValue;
        {
            Marshal.SetLastSystemError(0);
            returnValue = PInvoke(moduleHandle, functionName);
            lastError = Marshal.GetLastSystemError();
        }

        Marshal.SetLastPInvokeError(lastError);
        return returnValue;

        // Local P/Invoke
        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true)]
        static extern unsafe IntPtr PInvoke(IntPtr nativeModuleHandle, sbyte* nativeFunctionName);
    }

    public static unsafe IntPtr LoadLibraryExW(ushort* fileName, IntPtr fileHandle, uint flags)
    {
        int lastError;
        IntPtr returnValue;
        {
            Marshal.SetLastSystemError(0);
            returnValue = PInvoke(fileName, fileHandle, flags);
            lastError = Marshal.GetLastSystemError();
        }

        Marshal.SetLastPInvokeError(lastError);
        return returnValue;

        // Local P/Invoke
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryExW", ExactSpelling = true)]
        static extern unsafe IntPtr PInvoke(ushort* nativeFileName, IntPtr nativeFileHandle, uint nativeFlags);
    }
}
#endif