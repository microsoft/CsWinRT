// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

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
    /// <returns>An <see cref="Exception"/> instance that represents the converted <c>HRESULT</c>.</returns>
    /// <remarks>
    /// This method differs from <see cref="Marshal.GetExceptionForHR(int)"/> in that it leverages
    /// the <c>IRestrictedErrorInfo</c> infrastructure to flow <see cref="Exception"/> objects through
    /// calls (both in managed and native code). This improves the debugging experience.
    /// </remarks>
    /// <seealso cref="Marshal.GetExceptionForHR(int)"/>
    public static Exception GetExceptionForHR(HRESULT errorCode)
    {
        return GetExceptionForHR(errorCode, out _);
    }

    /// <inheritdoc cref="GetExceptionForHR(int)"/>
    /// <param name="errorCode">The <c>HRESULT</c> to be converted.</param>
    /// <param name="restoredExceptionFromGlobalState">restoredExceptionFromGlobalState Out param.</param>
    private static Exception GetExceptionForHR(HRESULT errorCode, out bool restoredExceptionFromGlobalState)
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

            if (restrictedErrorInfoValuePtr is not null)
            {
                if (IUnknownVftbl.QueryInterfaceUnsafe(
                    thisPtr: restrictedErrorInfoValuePtr,
                    iid: in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo,
                    pvObject: out void* languageErrorInfoPtr).Succeeded())
                {
                    try
                    {
                        exception = ILanguageExceptionErrorInfoMethods.GetLanguageException(languageErrorInfoPtr, errorCode);

                        if (exception is not null)
                        {
                            restoredExceptionFromGlobalState = true;
                            WindowsRuntimeObjectReference? restrictedErrorInfo = WindowsRuntimeObjectReference.CreateUnsafe(restrictedErrorInfoValuePtr, WellKnownWindowsInterfaceIIDs.IID_IRestrictedErrorInfo);

                            if (restrictedErrorInfo is not null)
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

            if (WindowsRuntimeImports.FormatMessageW(
                dwFlags:
                    FORMAT.FORMAT_MESSAGE_ALLOCATE_BUFFER |
                    FORMAT.FORMAT_MESSAGE_FROM_SYSTEM |
                    FORMAT.FORMAT_MESSAGE_IGNORE_INSERTS |
                    FORMAT.FORMAT_MESSAGE_MAX_WIDTH_MASK,
                lpSource: null,
                dwMessageId: (uint)errorCode,
                dwLanguageId: 0,
                lpBuffer: &message,
                nSize: 0,
                pArguments: null) != 0)
            {
                errorMessage = $"{new string(message)}(0x{errorCode:X8})";

                // 'LocalHandle' isn't needed since 'FormatMessage' uses 'LMEM_FIXED'
                _ = WindowsRuntimeImports.LocalFree(message);
            }
        }

        exception = WellKnownExceptionMappings.GetExceptionForHR(errorCode, errorMessage);

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
                if (restrictedErrorObject is not null)
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

        return WellKnownExceptionMappings.GetHRForNativeOrManagedErrorCode(hresult);
    }

    /// <summary>
    /// Stores info on the input exception through the <c>IRestrictedErrorInfo</c> infrastructure, to retrieve it later.
    /// </summary>
    /// <param name="exception">The input <see cref="Exception"/> instance to store.</param>
    public static void SetErrorInfo(Exception exception)
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
                if (!isLanguageException && restrictedErrorObject is not null &&
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
                if (restrictedErrorObject is not null)
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

                    if (exceptionType is not null)
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
    public static void ReportUnhandledError(Exception exception)
    {
        SetErrorInfo(exception);

        using WindowsRuntimeObjectReferenceValue restrictedErrorInfoValue = ExceptionHelpers.BorrowRestrictedErrorInfo();

        void* restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetThisPtrUnsafe();

        if (restrictedErrorInfoValuePtr is not null)
        {
            _ = WindowsRuntimeImports.RoReportUnhandledError(restrictedErrorInfoValuePtr);
        }
    }
}