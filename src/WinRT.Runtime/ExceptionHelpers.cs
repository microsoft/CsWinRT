// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
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
        private const int E_NOINTERFACE = unchecked((int)0x80004002);
        private const int E_OUTOFMEMORY = unchecked((int)0x8007000e);
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
                Exception ex = GetExceptionForHR(hr, useGlobalErrorState: true, out bool restoredExceptionFromGlobalState);
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
        private static ObjectReference<IUnknownVftbl> BorrowRestrictedErrorInfo()
        {
            if (getRestrictedErrorInfo == null)
                return null;

            IntPtr restrictedErrorInfoPtr = IntPtr.Zero;
            Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(&restrictedErrorInfoPtr));
            if (restrictedErrorInfoPtr == IntPtr.Zero)
                return null;

            if (setRestrictedErrorInfo != null)
            {
                setRestrictedErrorInfo(restrictedErrorInfoPtr);
                return ObjectReference<IUnknownVftbl>.FromAbi(restrictedErrorInfoPtr, IID.IID_IRestrictedErrorInfo);
            }

            return ObjectReference<IUnknownVftbl>.Attach(ref restrictedErrorInfoPtr, IID.IID_IRestrictedErrorInfo);
        }

        public static Exception GetExceptionForHR(int hr) => hr >= 0 ? null : GetExceptionForHR(hr, true, out _);

        private static Exception GetExceptionForHR(int hr, bool useGlobalErrorState, out bool restoredExceptionFromGlobalState)
        {
            restoredExceptionFromGlobalState = false;

            ObjectReference<IUnknownVftbl> restrictedErrorInfoToSave = null;
            Exception ex;
            string description = null;
            string restrictedError = null;
            string restrictedErrorReference = null;
            string restrictedCapabilitySid = null;
            bool hasOtherLanguageException = false;
            string errorMessage = string.Empty;

            if (useGlobalErrorState)
            {
                using var restrictedErrorInfoRef = BorrowRestrictedErrorInfo();
                if (restrictedErrorInfoRef != null)
                {
                    restrictedErrorInfoToSave = restrictedErrorInfoRef.As<IUnknownVftbl>(IID.IID_IRestrictedErrorInfo);
                    ABI.WinRT.Interop.IRestrictedErrorInfo restrictedErrorInfo = new(restrictedErrorInfoRef);
                    restrictedErrorInfo.GetErrorDetails(out description, out int hrLocal, out restrictedError, out restrictedCapabilitySid);
                    restrictedErrorReference = restrictedErrorInfo.GetReference();
                    if (restrictedErrorInfoRef.TryAs<IUnknownVftbl>(IID.IID_ILanguageExceptionErrorInfo, out var languageErrorInfoRef) >= 0)
                    {
#if NET
                        using IObjectReference languageException = ABI.WinRT.Interop.ILanguageExceptionErrorInfo.GetLanguageException(languageErrorInfoRef);
#else
                        ABI.WinRT.Interop.ILanguageExceptionErrorInfo languageErrorInfo = new(languageErrorInfoRef);
                        using IObjectReference languageException = languageErrorInfo.GetLanguageException();
#endif
                        if (languageException is object)
                        {
                            if (languageException.IsReferenceToManagedObject)
                            {
                                ex = ComWrappersSupport.FindObject<Exception>(languageException.ThisPtr);
                                if (GetHRForException(ex) == hr)
                                {
                                    restoredExceptionFromGlobalState = true;
                                    return ex;
                                }
                            }
                            else
                            {
                                // This could also be a proxy to a managed exception.
                                hasOtherLanguageException = true;
                            }
                        }
                    }

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

            switch (hr)
            {
                case E_CHANGED_STATE:
                case E_ILLEGAL_STATE_CHANGE:
                case E_ILLEGAL_METHOD_CALL:
                case E_ILLEGAL_DELEGATE_ASSIGNMENT:
                case APPMODEL_ERROR_NO_PACKAGE:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new InvalidOperationException(errorMessage) : new InvalidOperationException();
                    break;
                case E_XAMLPARSEFAILED:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.Markup.XamlParseException(errorMessage) : new Microsoft.UI.Xaml.Markup.XamlParseException();
                    break;
                case E_LAYOUTCYCLE:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.LayoutCycleException(errorMessage) : new Microsoft.UI.Xaml.LayoutCycleException();
                    break;
                case E_ELEMENTNOTAVAILABLE:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.Automation.ElementNotAvailableException(errorMessage) : new Microsoft.UI.Xaml.Automation.ElementNotAvailableException();
                    break;
                case E_ELEMENTNOTENABLED:
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.Automation.ElementNotEnabledException(errorMessage) : new Microsoft.UI.Xaml.Automation.ElementNotEnabledException();
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
                    hasOtherLanguageException);
            }

            return ex;
        }

        public static unsafe void SetErrorInfo(Exception ex)
        {
            if (getRestrictedErrorInfo != null && setRestrictedErrorInfo != null && roOriginateLanguageException != null)
            {
                // If the exception has information for an IRestrictedErrorInfo, use that
                // as our error so as to propagate the error through WinRT end-to-end.
                if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
                {
                    using (restrictedErrorObject)
                    {
                        setRestrictedErrorInfo(restrictedErrorObject.ThisPtr);
                    }
                }
                else
                {
                    string message = ex.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        message = ex.GetType().FullName;
                    }

                    IntPtr hstring;

                    fixed (char* lpMessage = message)
                    {
                        if (Platform.WindowsCreateString((ushort*)lpMessage, message.Length, &hstring) != 0)
                        {
                            hstring = IntPtr.Zero;
                        }
                    }

                    using var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex);
                    roOriginateLanguageException(GetHRForException(ex), hstring, managedExceptionWrapper.ThisPtr);
                }
            }
            else
            {
                using var iErrorInfo = ComWrappersSupport.CreateCCWForObject(new ManagedExceptionErrorInfo(ex));
                Platform.SetErrorInfo(0, iErrorInfo.ThisPtr);
            }
        }

        public static void ReportUnhandledError(Exception ex)
        {
            SetErrorInfo(ex);
            if (roReportUnhandledError != null)
            {
                var restrictedErrorInfoRef = BorrowRestrictedErrorInfo();
                if (restrictedErrorInfoRef != null)
                {
                    roReportUnhandledError(restrictedErrorInfoRef.ThisPtr);
                    GC.KeepAlive(restrictedErrorInfoRef);
                }
            }
        }

        public static int GetHRForException(Exception ex)
        {
            int hr = ex.HResult;
            if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
            {
                new ABI.WinRT.Interop.IRestrictedErrorInfo(restrictedErrorObject).GetErrorDetails(out _, out hr, out _, out _);
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

        internal static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            ObjectReference<IUnknownVftbl> restrictedErrorObject,
            bool hasRestrictedLanguageErrorObject = false)
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
            }
        }

        internal static bool TryGetRestrictedLanguageErrorObject(
            this Exception ex,
            out ObjectReference<IUnknownVftbl> restrictedErrorObject)
        {
            restrictedErrorObject = null;
            IDictionary dict = ex.Data;
            if (dict != null && dict.Contains("__HasRestrictedLanguageErrorObject"))
            {
                if (dict.Contains("__RestrictedErrorObjectReference"))
                {
                    restrictedErrorObject = (ObjectReference<IUnknownVftbl>)dict["__RestrictedErrorObjectReference"];
                }
                return (bool)dict["__HasRestrictedLanguageErrorObject"]!;
            }

            return false;
        }

        public static Exception AttachRestrictedErrorInfo(Exception e)
        {
            // If there is no exception, then the restricted error info doesn't apply to it
            if (e != null)
            {
                try
                {
                    // Get the restricted error info for this thread and see if it may correlate to the current
                    // exception object.  Note that in general the thread's IRestrictedErrorInfo is not meant for
                    // exceptions that are marshaled Windows.Foundation.HResults and instead are intended for
                    // HRESULT ABI return values.   However, in many cases async APIs will set the thread's restricted
                    // error info as a convention in order to provide extended debugging information for the ErrorCode
                    // property.
                    IntPtr restrictedErrorInfoPtr = IntPtr.Zero;
                    Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(&restrictedErrorInfoPtr));

                    if (restrictedErrorInfoPtr != IntPtr.Zero)
                    {
                        ObjectReference<IUnknownVftbl> restrictedErrorInfoRef = ObjectReference<IUnknownVftbl>.Attach(ref restrictedErrorInfoPtr, IID.IID_IRestrictedErrorInfo);

                        ABI.WinRT.Interop.IRestrictedErrorInfo restrictedErrorInfo = new ABI.WinRT.Interop.IRestrictedErrorInfo(restrictedErrorInfoRef);

                        restrictedErrorInfo.GetErrorDetails(out string description,
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
                            e.AddExceptionDataForRestrictedErrorInfo(description,
                                                                    restrictedDescription,
                                                                     restrictedErrorInfo.GetReference(),
                                                                     capabilitySid,
                                                                     restrictedErrorInfoRef.As<IUnknownVftbl>(IID.IID_IRestrictedErrorInfo));
                        }
                    }
                }
                catch
                {
                    // If we can't get the restricted error info, then proceed as if it isn't associated with this
                    // error.
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

namespace Microsoft.UI.Xaml
{
    namespace Automation
    {
#if EMBED
        internal
#else
        public
#endif
        class ElementNotAvailableException : Exception
        {
            public ElementNotAvailableException()
                : base("The element is not available.")
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            public ElementNotAvailableException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }

            public ElementNotAvailableException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTAVAILABLE;
            }
        }

#if EMBED
        internal
#else
        public
#endif
        class ElementNotEnabledException : Exception
        {
            public ElementNotEnabledException()
                : base("The element is not enabled.")
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }

            public ElementNotEnabledException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }

            public ElementNotEnabledException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_ELEMENTNOTENABLED;
            }
        }
    }
    namespace Markup
    {

#if EMBED
        internal
#else
        public
#endif
        class XamlParseException : Exception
        {
            public XamlParseException()
                : base("XAML parsing failed.")
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }

            public XamlParseException(string message)
                : base(message)
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }

            public XamlParseException(string message, Exception innerException)
                : base(message, innerException)
            {
                HResult = WinRT.ExceptionHelpers.E_XAMLPARSEFAILED;
            }
        }
    }

#if EMBED
    internal
#else
    public
#endif
    class LayoutCycleException : Exception
    {
        public LayoutCycleException()
            : base("A cycle occurred while laying out the GUI.")
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message)
            : base(message)
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }

        public LayoutCycleException(string message, Exception innerException)
            : base(message, innerException)
        {
            HResult = WinRT.ExceptionHelpers.E_LAYOUTCYCLE;
        }
    }
}
