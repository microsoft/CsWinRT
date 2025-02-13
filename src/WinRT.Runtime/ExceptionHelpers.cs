// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using WinRT.Interop;

namespace WinRT
{

#if EMBED
    internal
#else 
    public
#endif
    static unsafe class ExceptionHelpers
    {
        private const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);
        private const int COR_E_OPERATIONCANCELED = unchecked((int)0x8013153b);
        private const int COR_E_ARGUMENTOUTOFRANGE = unchecked((int)0x80131502);
        private const int COR_E_INDEXOUTOFRANGE = unchecked((int)0x80131508);
        private const int COR_E_TIMEOUT = unchecked((int)0x80131505);
        private const int COR_E_INVALIDOPERATION = unchecked((int)0x80131509);
        private const int RO_E_CLOSED = unchecked((int)0x80000013);
        internal const int E_BOUNDS = unchecked((int)0x8000000b);
        internal const int E_CHANGED_STATE = unchecked((int)0x8000000c);
        private const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000d);
        private const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000e);
        private const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);
        private const int APPMODEL_ERROR_NO_PACKAGE = unchecked((int)0x80073D54);
        internal const int E_XAMLPARSEFAILED = unchecked((int)0x802B000A);
        internal const int E_LAYOUTCYCLE = unchecked((int)0x802B0014);
        internal const int E_ELEMENTNOTENABLED = unchecked((int)0x802B001E);
        internal const int E_ELEMENTNOTAVAILABLE = unchecked((int)0x802B001F);
        internal const int ERROR_INVALID_WINDOW_HANDLE = unchecked((int)0x80070578);
        private const int E_POINTER = unchecked((int)0x80004003);
        private const int E_NOTIMPL = unchecked((int)0x80004001);
        private const int E_ACCESSDENIED = unchecked((int)0x80070005);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
        internal const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int E_OUTOFMEMORY = unchecked((int)0x8007000e);
        private const int E_NOTSUPPORTED = unchecked((int)0x80070032);
        private const int ERROR_ARITHMETIC_OVERFLOW = unchecked((int)0x80070216);
        private const int ERROR_FILENAME_EXCED_RANGE = unchecked((int)0x800700ce);
        private const int ERROR_FILE_NOT_FOUND = unchecked((int)0x80070002);
        private const int ERROR_HANDLE_EOF = unchecked((int)0x80070026);
        private const int ERROR_PATH_NOT_FOUND = unchecked((int)0x80070003);
        private const int ERROR_STACK_OVERFLOW = unchecked((int)0x800703e9);
        private const int ERROR_BAD_FORMAT = unchecked((int)0x8007000b);
        private const int ERROR_CANCELLED = unchecked((int)0x800704c7);
        private const int ERROR_TIMEOUT = unchecked((int)0x800705b4);

        private static delegate* unmanaged[Stdcall]<IntPtr*, int> getRestrictedErrorInfo;
        private static delegate* unmanaged[Stdcall]<IntPtr, int> setRestrictedErrorInfo;
        private static delegate* unmanaged[Stdcall]<int, IntPtr, IntPtr, int> roOriginateLanguageException;
        private static delegate* unmanaged[Stdcall]<IntPtr, int> roReportUnhandledError;

        private static readonly bool initialized = Initialize();

        private static bool Initialize()
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

                roOriginateLanguageException = (delegate* unmanaged[Stdcall]<int, IntPtr, IntPtr, int>)Platform.GetProcAddress(winRTErrorModule, langExceptionString);
                roReportUnhandledError = (delegate* unmanaged[Stdcall]<IntPtr, int>)Platform.GetProcAddress(winRTErrorModule, reportUnhandledErrorString);
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
                getRestrictedErrorInfo = (delegate* unmanaged[Stdcall]<IntPtr*, int>)Platform.GetProcAddress(winRTErrorModule, getRestrictedErrorInfoFuncName);
                setRestrictedErrorInfo = (delegate* unmanaged[Stdcall]<IntPtr, int>)Platform.GetProcAddress(winRTErrorModule, setRestrictedErrorInfoFuncName);
            }

            return true;
        }

        public static void ThrowExceptionForHR(int hr)
        {
            if (hr < 0)
            {
                Throw(hr);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Throw(int hr)
            {
                Exception ex = GetExceptionForHR(hr, useGlobalErrorState: true, true, out bool restoredExceptionFromGlobalState);
                if (restoredExceptionFromGlobalState)
                {
                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
                else
                {
                    throw ex;
                }
            }
        }

        // Retrieve restricted error info from thread without removing it, for propagation and debugging (watch, locals, etc)
        private static ObjectReferenceValue BorrowRestrictedErrorInfo()
        {
            if (getRestrictedErrorInfo == null)
                return default;

            IntPtr restrictedErrorInfoPtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(&restrictedErrorInfoPtr));
            if (restrictedErrorInfoPtr == IntPtr.Zero)
                return default;

            if (setRestrictedErrorInfo != null)
            {
                setRestrictedErrorInfo(restrictedErrorInfoPtr);
            }

            return ObjectReferenceValue.Attach(ref restrictedErrorInfoPtr);
        }

        public static Exception GetExceptionForHR(int hr) => hr >= 0 ? null : GetExceptionForHR(hr, true, false, out _);

        private static Exception GetExceptionForHR(int hr, bool useGlobalErrorState, bool associateErrorInfo, out bool restoredExceptionFromGlobalState)
        {
            restoredExceptionFromGlobalState = false;

            ObjectReference<IUnknownVftbl> restrictedErrorInfoToSave = null;
            Exception ex;
            string description = null;
            string restrictedError = null;
            string restrictedErrorReference = null;
            string restrictedCapabilitySid = null;
            string errorMessage = string.Empty;
            Exception internalGetGlobalErrorStateException = null;

            if (useGlobalErrorState)
            {
                ObjectReferenceValue restrictedErrorInfoValue = default;
                try
                {
                    restrictedErrorInfoValue = BorrowRestrictedErrorInfo();
                    var restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetAbi();
                    if (restrictedErrorInfoValuePtr != default)
                    {
                        if (Marshal.QueryInterface(restrictedErrorInfoValuePtr, ref Unsafe.AsRef(in IID.IID_ILanguageExceptionErrorInfo), out var languageErrorInfoPtr) >= 0)
                        {
                            try
                            {
                                ex = GetLanguageException(languageErrorInfoPtr, hr);
                                if (ex is not null)
                                {
                                    restoredExceptionFromGlobalState = true;
                                    if (associateErrorInfo)
                                    {
                                        ex.AddExceptionDataForRestrictedErrorInfo(ObjectReference<IUnknownVftbl>.FromAbi(restrictedErrorInfoValuePtr, IID.IID_IRestrictedErrorInfo), true);
                                    }
                                    return ex;
                                }
                            }
                            finally
                            {
                                Marshal.Release(languageErrorInfoPtr);
                            }
                        }

                        restrictedErrorInfoToSave = ObjectReference<IUnknownVftbl>.FromAbi(restrictedErrorInfoValuePtr, IID.IID_IRestrictedErrorInfo);
                        ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorInfoValuePtr, out description, out int hrLocal, out restrictedError, out restrictedCapabilitySid);
                        restrictedErrorReference = ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetReference(restrictedErrorInfoValuePtr);
                        if (hr == hrLocal)
                        {
                            // For cross language WinRT exceptions, general information will be available in the description,
                            // which is populated from IRestrictedErrorInfo::GetErrorDetails and more specific information will be available
                            // in the restrictedError which also comes from IRestrictedErrorInfo::GetErrorDetails. If both are available, we

                            // need to concatinate them to produce the final exception message.
                            if (!string.IsNullOrEmpty(description))
                            {
                                errorMessage += description;

                                if (!string.IsNullOrEmpty(restrictedError))
                                {
                                    errorMessage += Environment.NewLine;
                                }
                            }

                            if (!string.IsNullOrEmpty(restrictedError))
                            {
                                errorMessage += restrictedError;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // If we fail to get the error info or the exception from it,
                    // we fallback to using the hresult to create the exception.
                    // But we do store it in the exception data for debugging purposes.
                    Debug.Assert(false, e.Message, e.StackTrace);
                    internalGetGlobalErrorStateException = e;
                }
                finally
                {
                    restrictedErrorInfoValue.Dispose();
                }
            }

            switch (hr)
            {
                case E_CHANGED_STATE:
                case E_ILLEGAL_STATE_CHANGE:
                case E_ILLEGAL_METHOD_CALL:
                case E_ILLEGAL_DELEGATE_ASSIGNMENT:
                case APPMODEL_ERROR_NO_PACKAGE:
                case COR_E_INVALIDOPERATION:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new InvalidOperationException(errorMessage) : new InvalidOperationException();
                    break;
                case E_XAMLPARSEFAILED:
#if NET
                    if (FeatureSwitches.UseWindowsUIXamlProjections)
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.Markup.XamlParseException(errorMessage) : new Windows.UI.Xaml.Markup.XamlParseException();
                    }
                    else
#endif
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.Markup.XamlParseException(errorMessage) : new Microsoft.UI.Xaml.Markup.XamlParseException();
                    }
                    break;
                case E_LAYOUTCYCLE:
#if NET
                    if (FeatureSwitches.UseWindowsUIXamlProjections)
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.LayoutCycleException(errorMessage) : new Windows.UI.Xaml.LayoutCycleException();
                    }
                    else
#endif
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.LayoutCycleException(errorMessage) : new Microsoft.UI.Xaml.LayoutCycleException();
                    }
                    break;
                case E_ELEMENTNOTAVAILABLE:
#if NET
                    if (FeatureSwitches.UseWindowsUIXamlProjections)
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.Automation.ElementNotAvailableException(errorMessage) : new Windows.UI.Xaml.Automation.ElementNotAvailableException();
                    }
                    else
#endif
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.Automation.ElementNotAvailableException(errorMessage) : new Microsoft.UI.Xaml.Automation.ElementNotAvailableException();
                    }
                    break;
                case E_ELEMENTNOTENABLED:
#if NET
                    if (FeatureSwitches.UseWindowsUIXamlProjections)
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.Automation.ElementNotEnabledException(errorMessage) : new Windows.UI.Xaml.Automation.ElementNotEnabledException();
                    }
                    else
#endif
                    {
                        ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.Automation.ElementNotEnabledException(errorMessage) : new Microsoft.UI.Xaml.Automation.ElementNotEnabledException();
                    }
                    break;
                case ERROR_INVALID_WINDOW_HANDLE:
                    ex = new COMException(
@"Invalid window handle. (0x80070578)
Consider WindowNative, InitializeWithWindow
See https://aka.ms/cswinrt/interop#windows-sdk",
                        ERROR_INVALID_WINDOW_HANDLE);
                    break;
                case RO_E_CLOSED:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new ObjectDisposedException(string.Empty, errorMessage) : new ObjectDisposedException(string.Empty);
                    break;
                case E_POINTER:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new NullReferenceException(errorMessage) : new NullReferenceException();
                    break;
                case E_NOTIMPL:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new NotImplementedException(errorMessage) : new NotImplementedException();
                    break;
                case E_ACCESSDENIED:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new UnauthorizedAccessException(errorMessage) : new UnauthorizedAccessException();
                    break;
                case E_INVALIDARG:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new ArgumentException(errorMessage) : new ArgumentException();
                    break;
                case E_NOINTERFACE:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new InvalidCastException(errorMessage) : new InvalidCastException();
                    break;
                case E_OUTOFMEMORY:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new OutOfMemoryException(errorMessage) : new OutOfMemoryException();
                    break;
                case E_BOUNDS:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new ArgumentOutOfRangeException(errorMessage) : new ArgumentOutOfRangeException();
                    break;
                case E_NOTSUPPORTED:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new NotSupportedException(errorMessage) : new NotSupportedException();
                    break;
                case ERROR_ARITHMETIC_OVERFLOW:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new ArithmeticException(errorMessage) : new ArithmeticException();
                    break;
                case ERROR_FILENAME_EXCED_RANGE:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new PathTooLongException(errorMessage) : new PathTooLongException();
                    break;
                case ERROR_FILE_NOT_FOUND:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new FileNotFoundException(errorMessage) : new FileNotFoundException();
                    break;
                case ERROR_HANDLE_EOF:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new EndOfStreamException(errorMessage) : new EndOfStreamException();
                    break;
                case ERROR_PATH_NOT_FOUND:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new DirectoryNotFoundException(errorMessage) : new DirectoryNotFoundException();
                    break;
                case ERROR_STACK_OVERFLOW:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new StackOverflowException(errorMessage) : new StackOverflowException();
                    break;
                case ERROR_BAD_FORMAT:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new BadImageFormatException(errorMessage) : new BadImageFormatException();
                    break;
                case ERROR_CANCELLED:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new OperationCanceledException(errorMessage) : new OperationCanceledException();
                    break;
                case ERROR_TIMEOUT:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new TimeoutException(errorMessage) : new TimeoutException();
                    break;

                default:
                    ex = new COMException(errorMessage, hr);
                    break;
            }

            // Ensure HResult matches.
            ex.SetHResult(hr);

            if (useGlobalErrorState)
            {
                ex.AddExceptionDataForRestrictedErrorInfo(
                    description,
                    restrictedError,
                    restrictedErrorReference,
                    restrictedCapabilitySid,
                    restrictedErrorInfoToSave,
                    false,
                    internalGetGlobalErrorStateException);
            }

            return ex;
        }

        // This is a helper method specifically to be used by exception propagation scenarios where we carefully
        // manage the lifetime of the CCW for the exception object to avoid cycles and thereby leaking it.
        private static Exception GetLanguageException(IntPtr languageErrorInfoPtr, int hr)
        {
            // Check the error info first for the language exception.
            var exception = GetLanguageExceptionInternal(languageErrorInfoPtr, hr);
            if (exception is not null)
            {
                return exception;
            }

            // If propagated exceptions are supported, traverse it and check if any one of those is our exception to reuse.
            if (Marshal.QueryInterface(languageErrorInfoPtr, ref Unsafe.AsRef(in IID.IID_ILanguageExceptionErrorInfo2), out var languageErrorInfo2Ptr) >= 0)
            {
                IntPtr currentLanguageExceptionErrorInfo2Ptr = default;
                try
                {
                    currentLanguageExceptionErrorInfo2Ptr
                        = global::ABI.WinRT.Interop.ILanguageExceptionErrorInfo2Methods.GetPropagationContextHead(languageErrorInfo2Ptr);
                    while (currentLanguageExceptionErrorInfo2Ptr != default)
                    {
                        var propagatedException = GetLanguageExceptionInternal(currentLanguageExceptionErrorInfo2Ptr, hr);
                        if (propagatedException is not null)
                        {
                            return propagatedException;
                        }

                        var previousLanguageExceptionErrorInfo2Ptr = currentLanguageExceptionErrorInfo2Ptr;
                        currentLanguageExceptionErrorInfo2Ptr = global::ABI.WinRT.Interop.ILanguageExceptionErrorInfo2Methods.GetPreviousLanguageExceptionErrorInfo(currentLanguageExceptionErrorInfo2Ptr);
                        Marshal.Release(previousLanguageExceptionErrorInfo2Ptr);
                    }
                }
                finally
                {
                    MarshalExtensions.ReleaseIfNotNull(currentLanguageExceptionErrorInfo2Ptr);
                    Marshal.Release(languageErrorInfo2Ptr);
                }
            }

            return null;

            static Exception GetLanguageExceptionInternal(IntPtr languageErrorInfoPtr, int hr)
            {
                var languageExceptionPtr = ABI.WinRT.Interop.ILanguageExceptionErrorInfoMethods.GetLanguageException(languageErrorInfoPtr);
                if (languageExceptionPtr != default)
                {
                    try
                    {
                        if (IUnknownVftbl.IsReferenceToManagedObject(languageExceptionPtr))
                        {
                            var ex = ComWrappersSupport.FindObject<Exception>(languageExceptionPtr);
                            if (GetHRForException(ex) == hr)
                            {
                                return ex;
                            }
                        }
                    }
                    finally
                    {
                        Marshal.Release(languageExceptionPtr);
                    }
                }

                return null;
            }
        }

        public static unsafe void SetErrorInfo(Exception ex)
        {
            try
            {
                if (getRestrictedErrorInfo != null && setRestrictedErrorInfo != null && roOriginateLanguageException != null)
                {
                    // If the exception has an IRestrictedErrorInfo, use that as our error info
                    // to allow to propagate the original error through WinRT with the end to end information
                    // rather than losing that context.
                    if (ex.TryGetRestrictedLanguageErrorInfo(out var restrictedErrorObject, out var isLanguageException))
                    {
                        // Capture the C# language exception if it hasn't already been captured previously either during the throw or during a propagation.
                        // Given the C# exception itself captures propagation context on rethrow, we don't do it each time.
                        if (!isLanguageException &&
                            Marshal.QueryInterface(restrictedErrorObject.ThisPtr, ref Unsafe.AsRef(in IID.IID_ILanguageExceptionErrorInfo2), out var languageErrorInfo2Ptr) >= 0)
                        {
                            try
                            {
                                global::ABI.WinRT.Interop.ILanguageExceptionErrorInfo2Methods.CapturePropagationContext(languageErrorInfo2Ptr, ex);
                            }
                            finally
                            {
                                Marshal.Release(languageErrorInfo2Ptr);
                            }
                        }
                        else if (isLanguageException)
                        {
                            // Remove object reference to avoid cycles between error info holding exception
                            // and exception holding error info.  We currently can't avoid this cycle
                            // when the C# exception is caught on the C# side.
                            ex.Data.Remove("__RestrictedErrorObjectReference");
                            ex.Data.Remove("__HasRestrictedLanguageErrorObject");
                        }

                        setRestrictedErrorInfo(restrictedErrorObject.ThisPtr);

                        GC.KeepAlive(restrictedErrorObject);
                    }
                    else
                    {
                        string message = ex.Message;
                        if (string.IsNullOrEmpty(message))
                        {
                            message = ex.GetType().FullName;
                        }

                        MarshalString.HSTRING_HEADER header = default;
                        IntPtr hstring = default;

                        fixed (char* lpMessage = message)
                        {
                            if (Platform.WindowsCreateStringReference(
                                sourceString: (ushort*)lpMessage,
                                length: message.Length,
                                hstring_header: (IntPtr*)&header,
                                hstring: &hstring) != 0)
                            {
                                hstring = IntPtr.Zero;
                            }

#if NET
                        IntPtr managedExceptionWrapper = ComWrappersSupport.CreateCCWForObjectUnsafe(ex);

                        try
                        {
                            roOriginateLanguageException(GetHRForException(ex), hstring, managedExceptionWrapper);
                        }
                        finally
                        {
                            Marshal.Release(managedExceptionWrapper);
                        }
#else
                            using var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex);
                            roOriginateLanguageException(GetHRForException(ex), hstring, managedExceptionWrapper.ThisPtr);
#endif
                        }
                    }
                }
                else
                {
                    using var iErrorInfo = ComWrappersSupport.CreateCCWForObject(new ManagedExceptionErrorInfo(ex));
                    Platform.SetErrorInfo(0, iErrorInfo.ThisPtr);
                }
            }
            catch (Exception e)
            {
                // If we fail to set the error info, we continue on reporting the original exception.
                Debug.Assert(false, e.Message, e.StackTrace);
            }
        }

        public static void ReportUnhandledError(Exception ex)
        {
            SetErrorInfo(ex);
            if (roReportUnhandledError != null)
            {
                var restrictedErrorInfoValue = BorrowRestrictedErrorInfo();
                var restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetAbi();
                if (restrictedErrorInfoValuePtr != default)
                {
                    roReportUnhandledError(restrictedErrorInfoValuePtr);
                    restrictedErrorInfoValue.Dispose();
                }
            }
        }

        public static int GetHRForException(Exception ex)
        {
            int hr = ex.HResult;
            try
            {
                if (ex.TryGetRestrictedLanguageErrorInfo(out var restrictedErrorObject, out var _))
                {
                    ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorObject.ThisPtr, out hr);
                    GC.KeepAlive(restrictedErrorObject);
                }
            }
            catch (Exception e)
            {
                // If we fail to get the hresult from the error info, we fallback to the exception hresult.
                Debug.Assert(false, e.Message, e.StackTrace);
            }

            switch (hr)
            {
                case COR_E_OBJECTDISPOSED:
                    return RO_E_CLOSED;
                case COR_E_OPERATIONCANCELED:
                    return ERROR_CANCELLED;
                case COR_E_ARGUMENTOUTOFRANGE:
                case COR_E_INDEXOUTOFRANGE:
                    return E_BOUNDS;
                case COR_E_TIMEOUT:
                    return ERROR_TIMEOUT;

                default:
                    return hr;
            }
        }

#if !NET
        //
        // Exception requires anything to be added into Data dictionary is serializable
        // This wrapper is made serializable to satisfy this requirement but does NOT serialize
        // the object and simply ignores it during serialization, because we only need
        // the exception instance in the app to hold the error object alive.
        //
        [Serializable]
        internal sealed class __RestrictedErrorObject
        {
            // Hold the error object instance but don't serialize/deserialize it
            [NonSerialized]
            private readonly ObjectReference<IUnknownVftbl> _realErrorObject;
            internal __RestrictedErrorObject(ObjectReference<IUnknownVftbl> errorObject)
            {
                _realErrorObject = errorObject;
            }
            public ObjectReference<IUnknownVftbl> RealErrorObject
            {
                get
                {
                    return _realErrorObject;
                }
            }
        }
#endif

        internal static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            ObjectReference<IUnknownVftbl> restrictedErrorObject,
            bool hasRestrictedLanguageErrorObject = false,
            Exception internalGetGlobalErrorStateException = null)
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
            ObjectReference<IUnknownVftbl> restrictedErrorObject,
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
            out ObjectReference<IUnknownVftbl> restrictedErrorObject,
            out bool isLanguageException)
        {
            restrictedErrorObject = null;
            isLanguageException = false;

            IDictionary dict = ex.Data;
            if (dict != null )
            {
                if (dict.Contains("__RestrictedErrorObjectReference"))
                {
#if NET
                    restrictedErrorObject = (ObjectReference<IUnknownVftbl>)dict["__RestrictedErrorObjectReference"];
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

        public static Exception AttachRestrictedErrorInfo(Exception e)
        {
            // If there is no exception, then the restricted error info doesn't apply to it
            if (e != null)
            {
                IntPtr restrictedErrorInfoPtr = IntPtr.Zero;

                try
                {
                    // Get the restricted error info for this thread and see if it may correlate to the current
                    // exception object.  Note that in general the thread's IRestrictedErrorInfo is not meant for
                    // exceptions that are marshaled Windows.Foundation.HResults and instead are intended for
                    // HRESULT ABI return values.   However, in many cases async APIs will set the thread's restricted
                    // error info as a convention in order to provide extended debugging information for the ErrorCode
                    // property.
                    Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(&restrictedErrorInfoPtr));

                    if (restrictedErrorInfoPtr != IntPtr.Zero)
                    {
                        ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetErrorDetails(
                            restrictedErrorInfoPtr,
                            out string description,
                            out int restrictedErrorInfoHResult,
                            out string restrictedDescription,
                            out string capabilitySid);

                        // Since this is a special case where by convention there may be a correlation, there is not a
                        // guarantee that the restricted error info does belong to the async error code.  In order to
                        // reduce the risk that we associate incorrect information with the exception object, we need
                        // to apply a heuristic where we attempt to match the current exception's HRESULT with the
                        // HRESULT the IRestrictedErrorInfo belongs to.  If it is a match we will assume association
                        // for the IAsyncInfo case.
                        if (e.HResult == restrictedErrorInfoHResult)
                        {
                            string reference = ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetReference(restrictedErrorInfoPtr);
                            e.AddExceptionDataForRestrictedErrorInfo(description,
                                                                     restrictedDescription,
                                                                     reference,
                                                                     capabilitySid,
                                                                     ObjectReference<IUnknownVftbl>.Attach(ref restrictedErrorInfoPtr, IID.IID_IRestrictedErrorInfo));
                        }
                    }
                }
                catch
                {
                    // If we can't get the restricted error info, then proceed as if it isn't associated with this
                    // error.
                }
                finally
                {
                    MarshalExtensions.ReleaseIfNotNull(restrictedErrorInfoPtr);
                }
            }

            return e;
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class ExceptionExtensions
    {
        public static void SetHResult(this Exception ex, int value)
        {
#if !NET
            ex.GetType().GetProperty("HResult").SetValue(ex, value);
#else
            ex.HResult = value;
#endif
        }

        internal static Exception GetExceptionForHR(this Exception innerException, int hresult, string messageResource)
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
}