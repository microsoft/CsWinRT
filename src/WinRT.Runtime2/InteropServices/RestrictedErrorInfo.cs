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
        string? description = null;
        string? restrictedError = null;
        string? restrictedErrorReference = null;
        string? restrictedCapabilitySid = null;
        string? errorMessage = null;
        Exception? exception;
        Exception? internalGetGlobalErrorStateException = null;
        WindowsRuntimeObjectReference? restrictedErrorInfoToSave = null;


        using WindowsRuntimeObjectReferenceValue restrictedErrorInfoValue = ExceptionHelpers.BorrowRestrictedErrorInfo();

        try
        {
            void* restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetThisPtrUnsafe();

            if (restrictedErrorInfoValuePtr != null)
            {
                if (IUnknownVftbl.QueryInterfaceUnsafe(
                    thisPtr: restrictedErrorInfoValuePtr,
                    iid: in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo,
                    pvObject: out void* languageErrorInfoPtr).Succeeded())
                {
                    try
                    {
                        exception = ExceptionHelpers.GetLanguageException(languageErrorInfoPtr, errorCode);

                        if (exception is not null)
                        {
                            restoredExceptionFromGlobalState = true;
                            WindowsRuntimeObjectReference? restrictedErrorInfo = WindowsRuntimeObjectReference.CreateUnsafe(restrictedErrorInfoValuePtr, WellKnownWindowsInterfaceIIDs.IID_IRestrictedErrorInfo);

                            if (restrictedErrorInfo != null)
                            {
                                ExceptionHelpers.AddExceptionDataForRestrictedErrorInfo(exception, restrictedErrorInfo, true);
                            }

                            return exception;
                        }
                    }
                    finally
                    {
                        WindowsRuntimeObjectMarshaller.Free(languageErrorInfoPtr);
                    }
                }

                restrictedErrorInfoToSave = WindowsRuntimeObjectReference.CreateUnsafe(restrictedErrorInfoValuePtr, WellKnownWindowsInterfaceIIDs.IID_IRestrictedErrorInfo);

                IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorInfoValuePtr, out description, out HRESULT hrLocal, out restrictedError, out restrictedCapabilitySid);

                restrictedErrorReference = IRestrictedErrorInfoMethods.GetReference(restrictedErrorInfoValuePtr);

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

        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            char* message = null;

            if (WindowsRuntimeImports.FormatMessageW(0x13FF /* FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_MAX_WIDTH_MASK */,
                                        null,
                                        (uint)errorCode,
                                        0,
                                        &message,
                                        0,
                                        null) > 0)
            {
                errorMessage = $"{new string(message)}(0x{errorCode:X8})";

                // LocalHandle isn't needed since FormatMessage uses LMEM_FIXED,
                // and while we can use Marshal.FreeHGlobal since it uses LocalFree internally,
                // it's not guranteed that this behavior stays the same in the future,
                // especially considering the method's name, so it's safer to use LocalFree directly.
                _ = WindowsRuntimeImports.LocalFree(message);
            }
        }


        exception = errorCode switch
        {
            // InvalidOperationException 
            WellKnownErrorCodes.E_CHANGED_STATE or
            WellKnownErrorCodes.E_ILLEGAL_STATE_CHANGE or
            WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL or
            WellKnownErrorCodes.E_ILLEGAL_DELEGATE_ASSIGNMENT or
            WellKnownErrorCodes.APPMODEL_ERROR_NO_PACKAGE or
            WellKnownErrorCodes.COR_E_INVALIDOPERATION => !string.IsNullOrEmpty(errorMessage) ? new InvalidOperationException(errorMessage) : new InvalidOperationException(),

            // XamlParseException
            WellKnownErrorCodes.E_XAMLPARSEFAILED => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.XamlParseException(errorMessage) : new Windows.UI.Xaml.XamlParseException()
                : !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.XamlParseException(errorMessage) : new Microsoft.UI.Xaml.XamlParseException(),

            // LayoutCycleException
            WellKnownErrorCodes.E_LAYOUTCYCLE => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.LayoutCycleException(errorMessage) : new Windows.UI.Xaml.LayoutCycleException()
                : !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.LayoutCycleException(errorMessage) : new Microsoft.UI.Xaml.LayoutCycleException(),

            // ElementNotAvailableException
            WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.ElementNotAvailableException(errorMessage) : new Windows.UI.Xaml.ElementNotAvailableException()
                : !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.ElementNotAvailableException(errorMessage) : new Microsoft.UI.Xaml.ElementNotAvailableException(),

            // ElementNotEnabledException
            WellKnownErrorCodes.E_ELEMENTNOTENABLED => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(errorMessage) ? new Windows.UI.Xaml.ElementNotEnabledException(errorMessage) : new Windows.UI.Xaml.ElementNotEnabledException()
                : !string.IsNullOrEmpty(errorMessage) ? new Microsoft.UI.Xaml.ElementNotEnabledException(errorMessage) : new Microsoft.UI.Xaml.ElementNotEnabledException(),

            // COMException for invalid window handle with guidance
            WellKnownErrorCodes.ERROR_INVALID_WINDOW_HANDLE => new COMException(
                "Invalid window handle (0x80070578). Consider WindowNative, InitializeWithWindow. See https://aka.ms/cswinrt/interop#windows-sdk.",
                WellKnownErrorCodes.ERROR_INVALID_WINDOW_HANDLE),

            // Other common exceptions
            WellKnownErrorCodes.RO_E_CLOSED => !string.IsNullOrEmpty(errorMessage) ? new ObjectDisposedException(string.Empty, errorMessage) : new ObjectDisposedException(string.Empty),
            WellKnownErrorCodes.E_POINTER => !string.IsNullOrEmpty(errorMessage) ? new NullReferenceException(errorMessage) : new NullReferenceException(),
            WellKnownErrorCodes.E_NOTIMPL => !string.IsNullOrEmpty(errorMessage) ? new NotImplementedException(errorMessage) : new NotImplementedException(),
            WellKnownErrorCodes.E_ACCESSDENIED => !string.IsNullOrEmpty(errorMessage) ? new UnauthorizedAccessException(errorMessage) : new UnauthorizedAccessException(),
            WellKnownErrorCodes.E_INVALIDARG => !string.IsNullOrEmpty(errorMessage) ? new ArgumentException(errorMessage) : new ArgumentException(),
            WellKnownErrorCodes.E_NOINTERFACE => !string.IsNullOrEmpty(errorMessage) ? new InvalidCastException(errorMessage) : new InvalidCastException(),
            WellKnownErrorCodes.E_OUTOFMEMORY => !string.IsNullOrEmpty(errorMessage) ? new OutOfMemoryException(errorMessage) : new OutOfMemoryException(),
            WellKnownErrorCodes.E_BOUNDS => !string.IsNullOrEmpty(errorMessage) ? new ArgumentOutOfRangeException(errorMessage) : new ArgumentOutOfRangeException(),
            WellKnownErrorCodes.E_NOTSUPPORTED => !string.IsNullOrEmpty(errorMessage) ? new NotSupportedException(errorMessage) : new NotSupportedException(),
            WellKnownErrorCodes.ERROR_ARITHMETIC_OVERFLOW => !string.IsNullOrEmpty(errorMessage) ? new ArithmeticException(errorMessage) : new ArithmeticException(),
            WellKnownErrorCodes.ERROR_FILENAME_EXCED_RANGE => !string.IsNullOrEmpty(errorMessage) ? new PathTooLongException(errorMessage) : new PathTooLongException(),
            WellKnownErrorCodes.ERROR_FILE_NOT_FOUND => !string.IsNullOrEmpty(errorMessage) ? new FileNotFoundException(errorMessage) : new FileNotFoundException(),
            WellKnownErrorCodes.ERROR_HANDLE_EOF => !string.IsNullOrEmpty(errorMessage) ? new EndOfStreamException(errorMessage) : new EndOfStreamException(),
            WellKnownErrorCodes.ERROR_PATH_NOT_FOUND => !string.IsNullOrEmpty(errorMessage) ? new DirectoryNotFoundException(errorMessage) : new DirectoryNotFoundException(),
            WellKnownErrorCodes.ERROR_STACK_OVERFLOW => !string.IsNullOrEmpty(errorMessage) ? new StackOverflowException(errorMessage) : new StackOverflowException(),
            WellKnownErrorCodes.ERROR_BAD_FORMAT => !string.IsNullOrEmpty(errorMessage) ? new BadImageFormatException(errorMessage) : new BadImageFormatException(),
            WellKnownErrorCodes.ERROR_CANCELLED => !string.IsNullOrEmpty(errorMessage) ? new OperationCanceledException(errorMessage) : new OperationCanceledException(),
            WellKnownErrorCodes.ERROR_TIMEOUT => !string.IsNullOrEmpty(errorMessage) ? new TimeoutException(errorMessage) : new TimeoutException(),

            // Fallback to COMException
            _ => !string.IsNullOrEmpty(errorMessage) ? new COMException(errorMessage, errorCode) : new COMException($"0x{errorCode:X8}", errorCode),
        };

        // Ensure HResult matches.
        exception.HResult = errorCode;

        ExceptionHelpers.AddExceptionDataForRestrictedErrorInfo(
            exception,
            description,
            restrictedError,
            restrictedErrorReference,
            restrictedCapabilitySid,
            restrictedErrorInfoToSave,
            false,
            internalGetGlobalErrorStateException);

        return exception;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowExceptionForHR(HRESULT errorCode)
    {
        if (errorCode.Failed())
        {
            Throw(errorCode);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Throw(int errorCode)
        {
            Exception? exception = GetExceptionForHR(errorCode, out bool restoredExceptionFromGlobalState);

            if (restoredExceptionFromGlobalState)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            else
            {
                throw exception;
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
        ArgumentNullException.ThrowIfNull(exception);

        HRESULT hresult = exception.HResult;

        try
        {
            if (ExceptionHelpers.TryGetRestrictedLanguageErrorInfo(exception, out WindowsRuntimeObjectReference? restrictedErrorObject, out _))
            {
                if (restrictedErrorObject != null)
                {
                    using WindowsRuntimeObjectReferenceValue restrictedErrorObjectValue = restrictedErrorObject.AsValue();
                    IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorObjectValue.GetThisPtrUnsafe(), out hresult);
                }
            }
        }
        catch (Exception e)
        {
            // If we fail to get the hresult from the error info, we fallback to the exception hresult.
            Debug.Assert(false, e.Message, e.StackTrace);
        }

        return hresult switch
        {
            WellKnownErrorCodes.COR_E_OBJECTDISPOSED => WellKnownErrorCodes.RO_E_CLOSED,
            WellKnownErrorCodes.COR_E_OPERATIONCANCELED => WellKnownErrorCodes.ERROR_CANCELLED,
            WellKnownErrorCodes.COR_E_ARGUMENTOUTOFRANGE or WellKnownErrorCodes.COR_E_INDEXOUTOFRANGE => WellKnownErrorCodes.E_BOUNDS,
            WellKnownErrorCodes.COR_E_TIMEOUT => WellKnownErrorCodes.ERROR_TIMEOUT,
            _ => hresult,
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
            // If the exception has an IRestrictedErrorInfo, use that as our error info
            // to allow to propagate the original error through WinRT with the end to end information
            // rather than losing that context.
            if (ExceptionHelpers.TryGetRestrictedLanguageErrorInfo(exception, out WindowsRuntimeObjectReference? restrictedErrorObject, out bool isLanguageException))
            {
                // Capture the C# language exception if it hasn't already been captured previously either during the throw or during a propagation.
                // Given the C# exception itself captures propagation context on rethrow, we don't do it each time.
                if (!isLanguageException && restrictedErrorObject != null &&
                    IUnknownVftbl.QueryInterfaceUnsafe(
                        thisPtr: restrictedErrorObject.GetThisPtrUnsafe(),
                        iid: in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo2,
                        pvObject: out void* languageErrorInfo2Ptr).Succeeded())
                {
                    try
                    {
                        using WindowsRuntimeObjectReferenceValue managedExceptionWrapper = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(exception);

                        ((ILanguageExceptionErrorInfo2Vftbl*)*(void***)languageErrorInfo2Ptr)->CapturePropagationContext(
                            languageErrorInfo2Ptr,
                            managedExceptionWrapper.GetThisPtrUnsafe()).Assert();
                    }
                    finally
                    {
                        WindowsRuntimeObjectMarshaller.Free(languageErrorInfo2Ptr);
                    }
                }
                else if (isLanguageException)
                {
                    // Remove object reference to avoid cycles between error info holding exception
                    // and exception holding error info. We currently can't avoid this cycle
                    // when the C# exception is caught on the C# side.
                    exception.Data.Remove(WellKnownExceptionDataKeys.RestrictedErrorObjectReference);
                    exception.Data.Remove(WellKnownExceptionDataKeys.HasRestrictedLanguageErrorObject);
                }
                if (restrictedErrorObject != null)
                {
                    using WindowsRuntimeObjectReferenceValue restrictedErrorObjectValue = restrictedErrorObject.AsValue();

                    _ = WindowsRuntimeImports.SetRestrictedErrorInfo(restrictedErrorObjectValue.GetThisPtrUnsafe());
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

                fixed (char* lpMessage = message)
                {
                    HStringMarshaller.ConvertToUnmanagedUnsafe(lpMessage, message.Length, out HStringReference hstring);

                    WindowsRuntimeObjectReferenceValue managedExceptionWrapper = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(exception);

                    try
                    {
                        _ = WindowsRuntimeImports.RoOriginateLanguageException(GetHRForException(exception), hstring.HString, managedExceptionWrapper.GetThisPtrUnsafe());
                    }
                    finally
                    {
                        _ = Marshal.Release((nint)managedExceptionWrapper.DetachThisPtrUnsafe());
                    }

                }
            }
        }
        catch (Exception e)
        {
            // If we fail to set the error info, we continue on reporting the original exception.
            Debug.Assert(false, e.Message, e.StackTrace);
        }
    }

    /// <summary>
    /// Attaches the error info stored by the <c>IRestrictedErrorInfo</c> infrastructure to the input exception.
    /// </summary>
    /// <param name="exception">The input <see cref="Exception"/> instance to attach the error info to.</param>
    /// <returns>The input <see cref="Exception"/> instance with attached error info.</returns>
    public static Exception AttachErrorInfo(Exception exception)
    {
        // TODO
        return exception;
    }

    /// <summary>
    /// Triggers the global error handler when an unhandled exception occurs.
    /// </summary>
    /// <param name="exception">The input <see cref="Exception"/> instance to flow to the global error handler.</param>
    /// <see href="https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-roreportunhandlederror"/>
    public static unsafe void ReportUnhandledError(Exception exception)
    {
        SetErrorInfo(exception);

        using WindowsRuntimeObjectReferenceValue restrictedErrorInfoValue = ExceptionHelpers.BorrowRestrictedErrorInfo();

        void* restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetThisPtrUnsafe();

        if (restrictedErrorInfoValuePtr != null)
        {
            _ = WindowsRuntimeImports.RoReportUnhandledError(restrictedErrorInfoValuePtr);
        }
    }
}