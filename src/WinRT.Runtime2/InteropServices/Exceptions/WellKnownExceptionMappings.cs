// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Well known mappings for exceptions and native error codes.
/// </summary>
/// <remarks>
/// These are shared and kept in sync with <c>!analyze</c> (WinDbg), to properly bucket crash reports from Windows Error Reporting.
/// </remarks>
internal static class WellKnownExceptionMappings
{
    /// <summary>
    /// Gets the <see cref="Exception"/> object for a given native <c>HRESULT</c> value.
    /// </summary>
    /// <param name="hresult">The input <c>HRESULT</c> value.</param>
    /// <param name="message">The exception message, if available.</param>
    /// <returns>An <see cref="Exception"/> instance that represents <paramref name="hresult"/>.</returns>
    public static Exception GetExceptionForHR(HRESULT hresult, string? message)
    {
        Exception exception = hresult switch
        {
            // 'InvalidOperationException'
            WellKnownErrorCodes.E_CHANGED_STATE or
            WellKnownErrorCodes.E_ILLEGAL_STATE_CHANGE or
            WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL or
            WellKnownErrorCodes.E_ILLEGAL_DELEGATE_ASSIGNMENT or
            WellKnownErrorCodes.APPMODEL_ERROR_NO_PACKAGE or
            WellKnownErrorCodes.COR_E_INVALIDOPERATION => !string.IsNullOrEmpty(message) ? new InvalidOperationException(message) : new InvalidOperationException(),

            // 'XamlParseException'
            WellKnownErrorCodes.E_XAMLPARSEFAILED => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(message) ? new Windows.UI.Xaml.XamlParseException(message) : new Windows.UI.Xaml.XamlParseException()
                : !string.IsNullOrEmpty(message) ? new Microsoft.UI.Xaml.XamlParseException(message) : new Microsoft.UI.Xaml.XamlParseException(),

            // 'LayoutCycleException'
            WellKnownErrorCodes.E_LAYOUTCYCLE => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(message) ? new Windows.UI.Xaml.LayoutCycleException(message) : new Windows.UI.Xaml.LayoutCycleException()
                : !string.IsNullOrEmpty(message) ? new Microsoft.UI.Xaml.LayoutCycleException(message) : new Microsoft.UI.Xaml.LayoutCycleException(),

            // 'ElementNotAvailableException'
            WellKnownErrorCodes.E_ELEMENTNOTAVAILABLE => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(message) ? new Windows.UI.Xaml.ElementNotAvailableException(message) : new Windows.UI.Xaml.ElementNotAvailableException()
                : !string.IsNullOrEmpty(message) ? new Microsoft.UI.Xaml.ElementNotAvailableException(message) : new Microsoft.UI.Xaml.ElementNotAvailableException(),

            // 'ElementNotEnabledException'
            WellKnownErrorCodes.E_ELEMENTNOTENABLED => WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
                ? !string.IsNullOrEmpty(message) ? new Windows.UI.Xaml.ElementNotEnabledException(message) : new Windows.UI.Xaml.ElementNotEnabledException()
                : !string.IsNullOrEmpty(message) ? new Microsoft.UI.Xaml.ElementNotEnabledException(message) : new Microsoft.UI.Xaml.ElementNotEnabledException(),

            // 'COMException' for invalid window handle with guidance
            WellKnownErrorCodes.ERROR_INVALID_WINDOW_HANDLE => new COMException(
                message: "Invalid window handle (0x80070578). Consider 'WindowNative', 'InitializeWithWindow'. See https://aka.ms/cswinrt/interop#windows-sdk.",
                errorCode: WellKnownErrorCodes.ERROR_INVALID_WINDOW_HANDLE),

            // Other common exceptions
            WellKnownErrorCodes.RO_E_CLOSED => !string.IsNullOrEmpty(message) ? new ObjectDisposedException(string.Empty, message) : new ObjectDisposedException(string.Empty),
            WellKnownErrorCodes.E_POINTER => !string.IsNullOrEmpty(message) ? new NullReferenceException(message) : new NullReferenceException(),
            WellKnownErrorCodes.E_NOTIMPL => !string.IsNullOrEmpty(message) ? new NotImplementedException(message) : new NotImplementedException(),
            WellKnownErrorCodes.E_ACCESSDENIED => !string.IsNullOrEmpty(message) ? new UnauthorizedAccessException(message) : new UnauthorizedAccessException(),
            WellKnownErrorCodes.E_INVALIDARG => !string.IsNullOrEmpty(message) ? new ArgumentException(message) : new ArgumentException(),
            WellKnownErrorCodes.E_NOINTERFACE => !string.IsNullOrEmpty(message) ? new InvalidCastException(message) : new InvalidCastException(),
            WellKnownErrorCodes.E_OUTOFMEMORY => !string.IsNullOrEmpty(message) ? new OutOfMemoryException(message) : new OutOfMemoryException(),
            WellKnownErrorCodes.E_BOUNDS => !string.IsNullOrEmpty(message) ? new ArgumentOutOfRangeException(message) : new ArgumentOutOfRangeException(),
            WellKnownErrorCodes.E_NOTSUPPORTED => !string.IsNullOrEmpty(message) ? new NotSupportedException(message) : new NotSupportedException(),
            WellKnownErrorCodes.ERROR_ARITHMETIC_OVERFLOW => !string.IsNullOrEmpty(message) ? new ArithmeticException(message) : new ArithmeticException(),
            WellKnownErrorCodes.ERROR_FILENAME_EXCED_RANGE => !string.IsNullOrEmpty(message) ? new PathTooLongException(message) : new PathTooLongException(),
            WellKnownErrorCodes.ERROR_FILE_NOT_FOUND => !string.IsNullOrEmpty(message) ? new FileNotFoundException(message) : new FileNotFoundException(),
            WellKnownErrorCodes.ERROR_HANDLE_EOF => !string.IsNullOrEmpty(message) ? new EndOfStreamException(message) : new EndOfStreamException(),
            WellKnownErrorCodes.ERROR_PATH_NOT_FOUND => !string.IsNullOrEmpty(message) ? new DirectoryNotFoundException(message) : new DirectoryNotFoundException(),
            WellKnownErrorCodes.ERROR_STACK_OVERFLOW => !string.IsNullOrEmpty(message) ? new StackOverflowException(message) : new StackOverflowException(),
            WellKnownErrorCodes.ERROR_BAD_FORMAT => !string.IsNullOrEmpty(message) ? new BadImageFormatException(message) : new BadImageFormatException(),
            WellKnownErrorCodes.ERROR_CANCELLED => !string.IsNullOrEmpty(message) ? new OperationCanceledException(message) : new OperationCanceledException(),
            WellKnownErrorCodes.ERROR_TIMEOUT => !string.IsNullOrEmpty(message) ? new TimeoutException(message) : new TimeoutException(),

            // Fallback to 'COMException'
            _ => !string.IsNullOrEmpty(message) ? new COMException(message, hresult) : new COMException($"0x{hresult:X8}", hresult),
        };

        // Ensure HResult matches.
        exception.HResult = hresult;

        return exception;
    }

    /// <summary>
    /// Gets the mapped <c>HRESULT</c> for a given native or managed error code.
    /// </summary>
    /// <param name="hresult">The input <c>HRESULT</c> value to map.</param>
    /// <returns>The mapped <c>HRESULT</c> value.</returns>
    /// <remarks>
    /// This method is used to map some well-known managed error code (i.e. thrown by CoreCLR or Native AOT) to
    /// <c>HRESULT</c> values that will be recognized by native code, when passed through the ABI. It is important
    /// that the Windows Error Reporting infrastructure also recognizes these mapped values, as they are needed
    /// for proper bucketing of crash reports.
    /// </remarks>
    public static HRESULT GetHRForNativeOrManagedErrorCode(HRESULT hresult)
    {
        return hresult switch
        {
            WellKnownErrorCodes.COR_E_OBJECTDISPOSED => WellKnownErrorCodes.RO_E_CLOSED,
            WellKnownErrorCodes.COR_E_OPERATIONCANCELED => WellKnownErrorCodes.ERROR_CANCELLED,
            WellKnownErrorCodes.COR_E_ARGUMENTOUTOFRANGE or
            WellKnownErrorCodes.COR_E_INDEXOUTOFRANGE => WellKnownErrorCodes.E_BOUNDS,
            WellKnownErrorCodes.COR_E_TIMEOUT => WellKnownErrorCodes.ERROR_TIMEOUT,
            _ => hresult,
        };
    }
}