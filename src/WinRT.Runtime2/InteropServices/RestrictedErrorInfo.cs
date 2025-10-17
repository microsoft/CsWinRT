// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IDE0060 // TODO

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Handles the <c>IRestrictedErrorInfo</c> infrastructure for .NET exceptions.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/restrictederrorinfo/nn-restrictederrorinfo-irestrictederrorinfo"/>.
public static unsafe class RestrictedErrorInfo
{
    /// <summary>
    /// Converts an <c>HRESULT</c> error code to a corresponding <see cref="Exception"/> object.
    /// </summary>
    /// <param name="errorCode">The <c>HRESULT</c> to be converted.</param>
    /// <param name="restoredExceptionFromGlobalState">restoredExceptionFromGlobalState Out param.</param>
    /// <returns>
    /// An <see cref="Exception"/> instance that represents the converted <c>HRESULT</c>,
    /// or <see langword="null"/> if the HRESULT value doesn't represent an error code.
    /// </returns>
    /// <remarks>
    /// This method differs from <see cref="Marshal.GetExceptionForHR(int)"/> in that it leverages
    /// the <c>IRestrictedErrorInfo</c> infrastructure to flow <see cref="Exception"/> objects through
    /// calls (both in managed and native code). This improves the debugging experience.
    /// </remarks>
    /// <seealso cref="Marshal.GetExceptionForHR(int)"/>
    public static Exception GetExceptionForHR(HRESULT errorCode, out bool restoredExceptionFromGlobalState)
    {
        restoredExceptionFromGlobalState = false;
        Exception? ex;
        string description = string.Empty;
        string restrictedError = string.Empty;
        string restrictedErrorReference = string.Empty;
        string restrictedCapabilitySid = string.Empty;
        string errorMessage = string.Empty;
        Exception internalGetGlobalErrorStateException;

        WindowsRuntimeObjectReference? restrictedErrorInfoToSave = default;
        WindowsRuntimeObjectReferenceValue restrictedErrorInfoValue = ExceptionHelpers.BorrowRestrictedErrorInfo();

        try
        {
            void* restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetThisPtrUnsafe();
            if (restrictedErrorInfoValuePtr != default)
            {
                if (Marshal.QueryInterface((IntPtr)restrictedErrorInfoValuePtr, WellKnownInterfaceIds.IID_ILanguageExceptionErrorInfo, out nint languageErrorInfoPtr) >= 0)
                {
                    try
                    {
                        ex = ExceptionHelpers.GetLanguageException((void*)languageErrorInfoPtr, errorCode);
                        if (ex is not null)
                        {
                            restoredExceptionFromGlobalState = true;
                            WindowsRuntimeObjectReference? errRef = WindowsRuntimeObjectReference.Create(restrictedErrorInfoValuePtr, WellKnownInterfaceIds.IID_IRestrictedErrorInfo);
                            if (errRef != null)
                            {
                                ex.AddExceptionDataForRestrictedErrorInfo(errRef, true);
                            }
                            return ex;
                        }
                    }
                    finally
                    {
                        Marshal.Release(languageErrorInfoPtr);
                    }
                }

                restrictedErrorInfoToSave = WindowsRuntimeObjectReference.Create(restrictedErrorInfoValuePtr, WellKnownInterfaceIds.IID_IRestrictedErrorInfo);
                ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorInfoValuePtr, out description, out int hrLocal, out restrictedError, out restrictedCapabilitySid);
                restrictedErrorReference = ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetReference(restrictedErrorInfoValuePtr);
                if (errorCode == hrLocal)
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

        switch (errorCode)
        {
            case ExceptionHelpers.E_CHANGED_STATE:
            case ExceptionHelpers.E_ILLEGAL_STATE_CHANGE:
            case ExceptionHelpers.E_ILLEGAL_METHOD_CALL:
            case ExceptionHelpers.E_ILLEGAL_DELEGATE_ASSIGNMENT:
            case ExceptionHelpers.APPMODEL_ERROR_NO_PACKAGE:
            case ExceptionHelpers.COR_E_INVALIDOPERATION:
                ex = !string.IsNullOrEmpty(errorMessage) ? new InvalidOperationException(errorMessage) : new InvalidOperationException();
                break;
            case ExceptionHelpers.E_XAMLPARSEFAILED:
#if NET
                if (WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections)
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.XamlParseException(errorMessage) : new Windows.UI.Xaml.XamlParseException();
                }
                else
#endif
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.XamlParseException(errorMessage) : new Microsoft.UI.Xaml.XamlParseException();
                }
                break;
            case ExceptionHelpers.E_LAYOUTCYCLE:
#if NET
                if (WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections)
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.LayoutCycleException(errorMessage) : new Windows.UI.Xaml.LayoutCycleException();
                }
                else
#endif
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.LayoutCycleException(errorMessage) : new Microsoft.UI.Xaml.LayoutCycleException();
                }
                break;
            case ExceptionHelpers.E_ELEMENTNOTAVAILABLE:
#if NET
                if (WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections)
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.ElementNotAvailableException(errorMessage) : new Windows.UI.Xaml.ElementNotAvailableException();
                }
                else
#endif
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.ElementNotAvailableException(errorMessage) : new Microsoft.UI.Xaml.ElementNotAvailableException();
                }
                break;
            case ExceptionHelpers.E_ELEMENTNOTENABLED:
#if NET
                if (WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections)
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.ElementNotEnabledException(errorMessage) : new Windows.UI.Xaml.ElementNotEnabledException();
                }
                else
#endif
                {
                    ex = !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.ElementNotEnabledException(errorMessage) : new Microsoft.UI.Xaml.ElementNotEnabledException();
                }
                break;
            case ExceptionHelpers.ERROR_INVALID_WINDOW_HANDLE:
                ex = new COMException(
@"Invalid window handle. (0x80070578)
Consider WindowNative, InitializeWithWindow
See https://aka.ms/cswinrt/interop#windows-sdk",
                    ExceptionHelpers.ERROR_INVALID_WINDOW_HANDLE);
                break;
            case ExceptionHelpers.RO_E_CLOSED:
                ex = !string.IsNullOrEmpty(errorMessage) ? new ObjectDisposedException(string.Empty, errorMessage) : new ObjectDisposedException(string.Empty);
                break;
            case ExceptionHelpers.E_POINTER:
                ex = !string.IsNullOrEmpty(errorMessage) ? new NullReferenceException(errorMessage) : new NullReferenceException();
                break;
            case ExceptionHelpers.E_NOTIMPL:
                ex = !string.IsNullOrEmpty(errorMessage) ? new NotImplementedException(errorMessage) : new NotImplementedException();
                break;
            case ExceptionHelpers.E_ACCESSDENIED:
                ex = !string.IsNullOrEmpty(errorMessage) ? new UnauthorizedAccessException(errorMessage) : new UnauthorizedAccessException();
                break;
            case ExceptionHelpers.E_INVALIDARG:
                ex = !string.IsNullOrEmpty(errorMessage) ? new ArgumentException(errorMessage) : new ArgumentException();
                break;
            case ExceptionHelpers.E_NOINTERFACE:
                ex = !string.IsNullOrEmpty(errorMessage) ? new InvalidCastException(errorMessage) : new InvalidCastException();
                break;
            case ExceptionHelpers.E_OUTOFMEMORY:
                ex = !string.IsNullOrEmpty(errorMessage) ? new OutOfMemoryException(errorMessage) : new OutOfMemoryException();
                break;
            case ExceptionHelpers.E_BOUNDS:
                ex = !string.IsNullOrEmpty(errorMessage) ? new ArgumentOutOfRangeException(errorMessage) : new ArgumentOutOfRangeException();
                break;
            case ExceptionHelpers.E_NOTSUPPORTED:
                ex = !string.IsNullOrEmpty(errorMessage) ? new NotSupportedException(errorMessage) : new NotSupportedException();
                break;
            case ExceptionHelpers.ERROR_ARITHMETIC_OVERFLOW:
                ex = !string.IsNullOrEmpty(errorMessage) ? new ArithmeticException(errorMessage) : new ArithmeticException();
                break;
            case ExceptionHelpers.ERROR_FILENAME_EXCED_RANGE:
                ex = !string.IsNullOrEmpty(errorMessage) ? new PathTooLongException(errorMessage) : new PathTooLongException();
                break;
            case ExceptionHelpers.ERROR_FILE_NOT_FOUND:
                ex = !string.IsNullOrEmpty(errorMessage) ? new FileNotFoundException(errorMessage) : new FileNotFoundException();
                break;
            case ExceptionHelpers.ERROR_HANDLE_EOF:
                ex = !string.IsNullOrEmpty(errorMessage) ? new EndOfStreamException(errorMessage) : new EndOfStreamException();
                break;
            case ExceptionHelpers.ERROR_PATH_NOT_FOUND:
                ex = !string.IsNullOrEmpty(errorMessage) ? new DirectoryNotFoundException(errorMessage) : new DirectoryNotFoundException();
                break;
            case ExceptionHelpers.ERROR_STACK_OVERFLOW:
                ex = !string.IsNullOrEmpty(errorMessage) ? new StackOverflowException(errorMessage) : new StackOverflowException();
                break;
            case ExceptionHelpers.ERROR_BAD_FORMAT:
                ex = !string.IsNullOrEmpty(errorMessage) ? new BadImageFormatException(errorMessage) : new BadImageFormatException();
                break;
            case ExceptionHelpers.ERROR_CANCELLED:
                ex = !string.IsNullOrEmpty(errorMessage) ? new OperationCanceledException(errorMessage) : new OperationCanceledException();
                break;
            case ExceptionHelpers.ERROR_TIMEOUT:
                ex = !string.IsNullOrEmpty(errorMessage) ? new TimeoutException(errorMessage) : new TimeoutException();
                break;

            default:
                ex = !string.IsNullOrEmpty(errorMessage) ? new COMException(errorMessage, errorCode) : new COMException($"0x{errorCode:X8}", errorCode);
                break;
        }

        // Ensure HResult matches.
        ex.SetHResult(errorCode);

        return ex;
    }

    /// <summary>
    /// Throws the appropriate exception for an <c>HRESULT</c> error code, if it represents a failure.
    /// </summary>
    /// <param name="errorCode">The <c>HRESULT</c> to check.</param>
    /// <remarks>
    /// This method differs from <see cref="Marshal.ThrowExceptionForHR(int)"/> in that it leverages
    /// the <c>IRestrictedErrorInfo</c> infrastructure to flow <see cref="Exception"/> objects through
    /// calls (both in managed and native code). This improves the debugging experience.
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="errorCode"/> represents a failure.</exception>
    /// <seealso cref="Marshal.ThrowExceptionForHR(int)"/>
    public static void ThrowExceptionForHR(HRESULT errorCode)
    {
        if (errorCode < 0)
        {
            Throw(errorCode);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Throw(int errorCode)
        {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            Exception? ex = GetExceptionForHR(errorCode, out bool restoredExceptionFromGlobalState);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
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

    /// <summary>
    /// Converts the specified <see cref="Exception"/> instance to an <c>HRESULT</c>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> instance to convert to an <c>HRESULT</c>.</param>
    /// <returns>The <c>HRESULT</c> mapped to the supplied exception.</returns>
    /// <remarks>
    /// This method differs from <see cref="Marshal.GetHRForException(Exception?)"/> in that it leverages
    /// the <c>IRestrictedErrorInfo</c> infrastructure to better retrieve the resulting <c>HRESULT</c> value.
    /// </remarks>
    /// <seealso cref="Marshal.GetExceptionForHR(int)"/>
    public static HRESULT GetHRForException(Exception? exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException("exception");
        }
        int hr = exception.HResult;
        try
        {
            if (exception.TryGetRestrictedLanguageErrorInfo(out WindowsRuntimeObjectReference? restrictedErrorObject, out bool _))
            {
                if (restrictedErrorObject != null)
                {
                    ABI.WinRT.Interop.IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorObject.GetThisPtr(), out hr);
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
            ExceptionHelpers.COR_E_OBJECTDISPOSED => ExceptionHelpers.RO_E_CLOSED,
            ExceptionHelpers.COR_E_OPERATIONCANCELED => ExceptionHelpers.ERROR_CANCELLED,
            ExceptionHelpers.COR_E_ARGUMENTOUTOFRANGE or ExceptionHelpers.COR_E_INDEXOUTOFRANGE => ExceptionHelpers.E_BOUNDS,
            ExceptionHelpers.COR_E_TIMEOUT => ExceptionHelpers.ERROR_TIMEOUT,
            _ => hr,
        };
    }

    /// <summary>
    /// Stores info on the input exception through the <c>IRestrictedErrorInfo</c> infrastructure, to retrieve it later.
    /// </summary>
    /// <param name="exception">The input <see cref="Exception"/> instance to store.</param>
    public static unsafe void SetErrorInfo(Exception exception)
    {
        try
        {
            if (ExceptionHelpers.getRestrictedErrorInfo != null && ExceptionHelpers.setRestrictedErrorInfo != null && ExceptionHelpers.roOriginateLanguageException != null)
            {
                // If the exception has an IRestrictedErrorInfo, use that as our error info
                // to allow to propagate the original error through WinRT with the end to end information
                // rather than losing that context.
                if (exception.TryGetRestrictedLanguageErrorInfo(out WindowsRuntimeObjectReference? restrictedErrorObject, out bool isLanguageException))
                {
                    // Capture the C# language exception if it hasn't already been captured previously either during the throw or during a propagation.
                    // Given the C# exception itself captures propagation context on rethrow, we don't do it each time.
                    if (!isLanguageException && restrictedErrorObject != null &&
                        Marshal.QueryInterface((nint)restrictedErrorObject.GetThisPtrUnsafe(), WellKnownInterfaceIds.IID_ILanguageExceptionErrorInfo2, out nint languageErrorInfo2Ptr) >= 0)
                    {
                        try
                        {
                            global::ABI.WinRT.Interop.ILanguageExceptionErrorInfo2Methods.CapturePropagationContext((void*)languageErrorInfo2Ptr, exception);
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
                        exception.Data.Remove("__RestrictedErrorObjectReference");
                        exception.Data.Remove("__HasRestrictedLanguageErrorObject");
                    }
                    if (restrictedErrorObject != null)
                    {
                        ExceptionHelpers.setRestrictedErrorInfo(restrictedErrorObject.GetThisPtrUnsafe());
                        GC.KeepAlive(restrictedErrorObject);
                    }
                }
                else
                {
                    string message = exception.Message;
                    if (string.IsNullOrEmpty(message))
                    {
                        Type exceptionType = exception.GetType();
                        if (exceptionType != null)
                        {
                            message = exceptionType.Name;
                        }
                    }

                    HSTRING_HEADER* header = default;
                    HSTRING* hstring = default;

                    fixed (char* lpMessage = message)
                    {
                        if (WindowsRuntimeImports.WindowsCreateStringReference(
                            sourceString: lpMessage,
                            length: (uint)message.Length,
                            hstringHeader: header,
                            @string: hstring) != 0)
                        {
                            hstring = null;
                        }

#if NET
                        WindowsRuntimeObjectReferenceValue managedExceptionWrapper = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(exception);
                        try
                        {
                            ExceptionHelpers.roOriginateLanguageException(GetHRForException(exception), hstring, managedExceptionWrapper.GetThisPtrUnsafe());
                        }
                        finally
                        {
                            Marshal.Release((nint)managedExceptionWrapper.DetachThisPtrUnsafe());
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
                ManagedExceptionErrorInfo iErrorInfo = new(exception);
                WindowsRuntimeObjectReferenceValue managedExceptionWrapper = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(new ManagedExceptionErrorInfo(exception));
                WindowsRuntimeImports.SetErrorInfo(0, managedExceptionWrapper.GetThisPtrUnsafe());
            }
        }
        catch (Exception e)
        {
            // If we fail to set the error info, we continue on reporting the original exception.
            Debug.Assert(false, e.Message, e.StackTrace);
        }
    }

    /// <summary>
    /// Triggers the global error handler when an unhandled exception occurs.
    /// </summary>
    /// <param name="exception">The input <see cref="Exception"/> instance to flow to the global error handler.</param>
    /// <see href="https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-roreportunhandlederror"/>
    public static void ReportUnhandledError(Exception exception)
    {
        // TODO
    }
}