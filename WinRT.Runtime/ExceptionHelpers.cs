using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    public static class ExceptionHelpers
    {
        private const int COR_E_OBJECTDISPOSED = unchecked((int)0x80131622);
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

        [DllImport("oleaut32.dll")]
        private static extern int SetErrorInfo(uint dwReserved, IntPtr perrinfo);

        internal delegate int GetRestrictedErrorInfo(out IntPtr ppRestrictedErrorInfo);
        private static GetRestrictedErrorInfo getRestrictedErrorInfo;

        internal delegate int SetRestrictedErrorInfo(IntPtr pRestrictedErrorInfo);
        private static SetRestrictedErrorInfo setRestrictedErrorInfo;

        internal delegate int RoOriginateLanguageException(int error, IntPtr message, IntPtr langaugeException);
        private static RoOriginateLanguageException roOriginateLanguageException;

        static ExceptionHelpers()
        {
            IntPtr winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-1.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
            if (winRTErrorModule != IntPtr.Zero)
            {
                getRestrictedErrorInfo = Platform.GetProcAddress<GetRestrictedErrorInfo>(winRTErrorModule);
                setRestrictedErrorInfo = Platform.GetProcAddress<SetRestrictedErrorInfo>(winRTErrorModule);
                roOriginateLanguageException = Platform.GetProcAddress<RoOriginateLanguageException>(winRTErrorModule);
            }
            else
            {
                winRTErrorModule = Platform.LoadLibraryExW("api-ms-win-core-winrt-error-l1-1-0.dll", IntPtr.Zero, (uint)DllImportSearchPath.System32);
                if (winRTErrorModule != IntPtr.Zero)
                {
                    getRestrictedErrorInfo = Platform.GetProcAddress<GetRestrictedErrorInfo>(winRTErrorModule);
                    setRestrictedErrorInfo = Platform.GetProcAddress<SetRestrictedErrorInfo>(winRTErrorModule);
                }
            }
        }

        public static void ThrowExceptionForHR(int hr)
        {
            Exception ex = GetExceptionForHR(hr, useGlobalErrorState: true, out bool restoredExceptionFromGlobalState);
            if (restoredExceptionFromGlobalState)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            else if (ex is object)
            {
                throw ex;
            }
        }

        public static Exception GetExceptionForHR(int hr) => GetExceptionForHR(hr, false, out _);

        private static Exception GetExceptionForHR(int hr, bool useGlobalErrorState, out bool restoredExceptionFromGlobalState)
        {
            restoredExceptionFromGlobalState = false;
            if (hr == 0)
            {
                return null;
            }

            IObjectReference iErrorInfo = null;
            IObjectReference restrictedErrorInfoToSave = null;
            Exception ex;
            string description = null;
            string restrictedError = null;
            string restrictedErrorReference = null;
            string restrictedCapabilitySid = null;
            bool hasOtherLanguageException = false;

            if (useGlobalErrorState && getRestrictedErrorInfo != null)
            {
                Marshal.ThrowExceptionForHR(getRestrictedErrorInfo(out IntPtr restrictedErrorInfoPtr));

                if (restrictedErrorInfoPtr != IntPtr.Zero)
                {
                    IObjectReference restrictedErrorInfoRef = ObjectReference<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>.Attach(ref restrictedErrorInfoPtr);
                    restrictedErrorInfoToSave = restrictedErrorInfoRef.As<ABI.WinRT.Interop.IRestrictedErrorInfo.Vftbl>();

                    ABI.WinRT.Interop.IRestrictedErrorInfo restrictedErrorInfo = new ABI.WinRT.Interop.IRestrictedErrorInfo(restrictedErrorInfoRef);
                    restrictedErrorInfo.GetErrorDetails(out description, out int hrLocal, out restrictedError, out restrictedCapabilitySid);
                    restrictedErrorReference = restrictedErrorInfo.GetReference();
                    try
                    {
                        ILanguageExceptionErrorInfo languageErrorInfo = restrictedErrorInfo.As<ABI.WinRT.Interop.ILanguageExceptionErrorInfo>();
                        using (IObjectReference languageException = languageErrorInfo.GetLanguageException())
                        {
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
                                    hasOtherLanguageException = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (hr == hrLocal)
                        {
                            try
                            {
                                iErrorInfo = restrictedErrorInfoRef.As<ABI.WinRT.Interop.IErrorInfo.Vftbl>();
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }

            using (iErrorInfo)
            {
                switch (hr)
                {
                    case E_ILLEGAL_STATE_CHANGE:
                    case E_ILLEGAL_METHOD_CALL:
                    case E_ILLEGAL_DELEGATE_ASSIGNMENT:
                    case APPMODEL_ERROR_NO_PACKAGE:
                        ex = new InvalidOperationException(description);
                        break;
                    case E_XAMLPARSEFAILED:
                        ex = new Windows.UI.Xaml.Markup.XamlParseException();
                        break;
                    case E_LAYOUTCYCLE:
                        ex = new Windows.UI.Xaml.LayoutCycleException();
                        break;
                    case E_ELEMENTNOTAVAILABLE:
                        ex = new Windows.UI.Xaml.Automation.ElementNotAvailableException();
                        break;
                    case E_ELEMENTNOTENABLED:
                        ex = new Windows.UI.Xaml.Automation.ElementNotEnabledException();
                        break;
                    default:
                        ex = Marshal.GetExceptionForHR(hr, iErrorInfo?.ThisPtr ?? (IntPtr)(-1));
                        break;
                }
            }

            ex.AddExceptionDataForRestrictedErrorInfo(
                description,
                restrictedError,
                restrictedErrorReference,
                restrictedCapabilitySid,
                restrictedErrorInfoToSave,
                hasOtherLanguageException);

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
                    setRestrictedErrorInfo(restrictedErrorObject.GetRef());
                }
                else
                {
                    string message = ex.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        message = ex.GetType().FullName;
                    }

                    IntPtr hstring;

                    if (Platform.WindowsCreateString(message, message.Length, &hstring) != 0)
                    {
                        hstring = IntPtr.Zero;
                    }

                    using (var managedExceptionWrapper = ComWrappersSupport.CreateCCWForObject(ex))
                    {
                        roOriginateLanguageException(GetHRForException(ex), hstring, managedExceptionWrapper.ThisPtr);
                    }
                }
            }
            else
            {
                using (var iErrorInfo = ComWrappersSupport.CreateCCWForObject(new ManagedExceptionErrorInfo(ex)))
                {
                    SetErrorInfo(0, iErrorInfo.ThisPtr);
                }
            }
        }

        public static int GetHRForException(Exception ex)
        {
            int hr = ex.HResult;
            if (ex.TryGetRestrictedLanguageErrorObject(out var restrictedErrorObject))
            {
                restrictedErrorObject.AsType<ABI.WinRT.Interop.IRestrictedErrorInfo>().GetErrorDetails(out _, out hr, out _, out _);
            }
            if (hr == COR_E_OBJECTDISPOSED)
            {
                return RO_E_CLOSED;
            }
            return hr;
        }

        //
        // Exception requires anything to be added into Data dictionary is serializable
        // This wrapper is made serializable to satisfy this requirement but does NOT serialize
        // the object and simply ignores it during serialization, because we only need
        // the exception instance in the app to hold the error object alive.
        //
        [Serializable]
        internal class __RestrictedErrorObject
        {
            // Hold the error object instance but don't serialize/deserialize it
            [NonSerialized]
            private readonly IObjectReference _realErrorObject;

            internal __RestrictedErrorObject(IObjectReference errorObject)
            {
                _realErrorObject = errorObject;
            }

            public IObjectReference RealErrorObject
            {
                get
                {
                    return _realErrorObject;
                }
            }
        }

        internal static void AddExceptionDataForRestrictedErrorInfo(
            this Exception ex,
            string description,
            string restrictedError,
            string restrictedErrorReference,
            string restrictedCapabilitySid,
            IObjectReference restrictedErrorObject,
            bool hasRestrictedLanguageErrorObject = false)
        {
            IDictionary dict = ex.Data;
            if (dict != null)
            {
                dict.Add("Description", description);
                dict.Add("RestrictedDescription", restrictedError);
                dict.Add("RestrictedErrorReference", restrictedErrorReference);
                dict.Add("RestrictedCapabilitySid", restrictedCapabilitySid);

                // Keep the error object alive so that user could retrieve error information
                // using Data["RestrictedErrorReference"]
                dict.Add("__RestrictedErrorObjectReference", restrictedErrorObject == null ? null : new __RestrictedErrorObject(restrictedErrorObject));
                dict.Add("__HasRestrictedLanguageErrorObject", hasRestrictedLanguageErrorObject);
            }
        }

        internal static bool TryGetRestrictedLanguageErrorObject(
            this Exception ex,
            out IObjectReference restrictedErrorObject)
        {
            restrictedErrorObject = null;
            IDictionary dict = ex.Data;
            if (dict != null && dict.Contains("__HasRestrictedLanguageErrorObject"))
            {
                if (dict.Contains("__RestrictedErrorObjectReference"))
                {
                    if (dict["__RestrictedErrorObjectReference"] is __RestrictedErrorObject restrictedObject)
                        restrictedErrorObject = restrictedObject.RealErrorObject;
                }
                return (bool)dict["__HasRestrictedLanguageErrorObject"]!;
            }

            return false;
        }
    }

}

namespace Windows.UI.Xaml
{
    using System.Runtime.Serialization;
    namespace Automation
    {
        [Serializable]
        public class ElementNotAvailableException : Exception
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

            protected ElementNotAvailableException(SerializationInfo serializationInfo, StreamingContext streamingContext)
                : base(serializationInfo, streamingContext)
            {
            }
        }

        public class ElementNotEnabledException : Exception
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
        public class XamlParseException : Exception
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
    [Serializable]
    public class LayoutCycleException : Exception
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

        protected LayoutCycleException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}