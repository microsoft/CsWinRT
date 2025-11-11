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
    public static Exception GetExceptionForHR(HRESULT errorCode, out bool restoredExceptionFromGlobalState)
    {
        return GetExceptionForHR(errorCode, out _);
    }

    /// <inheritdoc cref="GetExceptionForHR(int)"/>
    /// <param name="errorCode">The <c>HRESULT</c> to be converted.</param>
    /// <param name="restoredExceptionFromGlobalState">restoredExceptionFromGlobalState Out param.</param>
    private static Exception? GetExceptionForHR(HRESULT errorCode, out bool restoredExceptionFromGlobalState)
    {
        // If the 'HRESULT' indicates success, there is no exception to return
        if (errorCode.Succeeded())
        {
            restoredExceptionFromGlobalState = false;

            return null;
        }

        string? description = null;
        string? restrictedDescription = null;
        string? restrictedErrorReference = null;
        string? restrictedCapabilitySid = null;
        string? errorMessage = null;
        Exception? exception;
        Exception? internalGetGlobalErrorStateException = null;
        WindowsRuntimeObjectReference? restrictedErrorInfoToSave = null;

        // First, try to get the current 'IRestrictedErrorInfo' object from the global state.
        // This can be present if some other failure path before has already set it. Note that
        // this might be from both some other C# code, or from some native code path as well.
        using (WindowsRuntimeObjectReferenceValue restrictedErrorInfoValue = RestrictedErrorInfoHelpers.BorrowErrorInfo())
        {
            void* restrictedErrorInfoPtr = restrictedErrorInfoValue.GetThisPtrUnsafe();

            // If there is no global 'IRestrictedErrorInfo' object, we'll just create a new local exception
            if (restrictedErrorInfoPtr is null)
            {
                goto GetLocalExceptionForHR;
            }

            // We never want to throw if anything goes wrong when recovering the previously stored exception.
            // So if any exceptions are thrown internally here, we'll continue and just forward those too.
            try
            {
                // Initialize an object reference for the global 'IRestrictedErrorInfo' object we retrieved.
                // We will always need this, regardless of whether we can restore the global exception or not.
                restrictedErrorInfoToSave = WindowsRuntimeObjectReference.CreateUnsafe(
                    thisPtr: restrictedErrorInfoPtr,
                    iid: WellKnownWindowsInterfaceIIDs.IID_IRestrictedErrorInfo)!;

                // Check if the stored exception is a language exception, which we can return directly
                if (IUnknownVftbl.QueryInterfaceUnsafe(
                    thisPtr: restrictedErrorInfoPtr,
                    iid: in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo,
                    pvObject: out void* languageErrorInfoPtr).Succeeded())
                {
                    try
                    {
                        // If we can restore the original language exception, we populate its exception data
                        // with a reference to the global restricted error info object, and return it. We
                        // only do this if the 'HRESULT' for the recovered exception matches the input one.
                        if (ILanguageExceptionErrorInfoMethods.TryGetLanguageException(languageErrorInfoPtr, errorCode, out exception))
                        {
                            restoredExceptionFromGlobalState = true;

                            RestrictedErrorInfoHelpers.AddExceptionData(exception, restrictedErrorInfoToSave, true);

                            return exception;
                        }
                    }
                    finally
                    {
                        WindowsRuntimeUnknownMarshaller.Free(languageErrorInfoPtr);
                    }
                }

                // We're going to create a new exception, so first we extract all the available info
                // from the global 'IRestrictedErrorInfo' object, to attach it to the managed exception.
                IRestrictedErrorInfoMethods.GetErrorDetails(
                    thisPtr: restrictedErrorInfoPtr,
                    description: out description,
                    error: out HRESULT restrictedError,
                    restrictedDescription: out restrictedDescription,
                    capabilitySid: out restrictedCapabilitySid);

                restrictedErrorReference = IRestrictedErrorInfoMethods.GetReference(restrictedErrorInfoPtr);

                // For cross language Windows Runtime exceptions, general information will be available in the description,
                // which is populated from 'IRestrictedErrorInfo.GetErrorDetails', and more specific information will be
                // available in 'restrictedError', which also comes from 'IRestrictedErrorInfo.GetErrorDetails'. If both are
                // available, we need to concatinate them to produce the final exception message.
                if (errorCode == restrictedError)
                {
                    // Append the description first
                    if (!string.IsNullOrEmpty(description))
                    {
                        errorMessage += description;

                        if (!string.IsNullOrEmpty(restrictedDescription))
                        {
                            errorMessage += " ";
                        }
                    }

                    // Next append the restricted description. If we have both, we insert a space to separate
                    // the two. We're not using a newline, as exception messages should always be a single line.
                    if (!string.IsNullOrEmpty(restrictedDescription))
                    {
                        errorMessage += restrictedDescription;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message, e.StackTrace);

                // If we fail to get the error info or the exception from it, we fallback
                // to using the 'HRESULT' value to create the exception. In this case we will
                // also store the caught exception, for debugging purposes (and bug reports).
                internalGetGlobalErrorStateException = e;
            }
        }

    GetLocalExceptionForHR:

        // If we still have no available error message, try to get a system-provided one
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            errorMessage = SystemErrorInfoHelpers.GetSystemErrorMessageForHR(errorCode);
        }

        // Create a new local exception using the 'HRESULT' mapping
        exception = WellKnownExceptionMappings.GetExceptionForHR(errorCode, errorMessage);

        // Append all information we retrieved above to the new exception object
        RestrictedErrorInfoHelpers.AddExceptionData(
            exception,
            description,
            restrictedDescription,
            restrictedErrorReference,
            restrictedCapabilitySid,
            restrictedErrorInfoToSave,
            false,
            internalGetGlobalErrorStateException);

        restoredExceptionFromGlobalState = false;

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
        static void Throw(HRESULT errorCode)
        {
            // The 'HRESULT' is guaranteed to be a failure, so the exception will never be 'null'
            Exception exception = GetExceptionForHR(errorCode, out bool restoredExceptionFromGlobalState)!;

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
        // If the input exception is 'null', we always just map to 'S_OK'
        if (exception is null)
        {
            return WellKnownErrorCodes.S_OK;
        }

        HRESULT hresult = exception.HResult;

        try
        {
            if (RestrictedErrorInfoHelpers.TryGetErrorInfo(exception, out WindowsRuntimeObjectReference? restrictedErrorObject, out _))
            {
                using WindowsRuntimeObjectReferenceValue restrictedErrorObjectValue = restrictedErrorObject.AsValue();

                IRestrictedErrorInfoMethods.GetErrorDetails(restrictedErrorObjectValue.GetThisPtrUnsafe(), out hresult);
            }
        }
        catch (Exception e)
        {
            // If we fail to get the hresult from the error info, we fallback to the exception 'HRESULT' value
            Debug.Assert(false, e.Message, e.StackTrace);
        }

        return WellKnownExceptionMappings.GetHRForNativeOrManagedErrorCode(hresult);
    }

    /// <summary>
    /// Stores info on the input exception through the <c>IRestrictedErrorInfo</c> infrastructure, to retrieve it later.
    /// </summary>
    /// <param name="exception">The input <see cref="Exception"/> instance to store.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static void SetErrorInfo(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            // If the exception has an 'IRestrictedErrorInfo' value, use that as our error info to allow to propagate
            // the original error through WinRT with the end to end information rather than losing that context.
            if (RestrictedErrorInfoHelpers.TryGetErrorInfo(exception, out WindowsRuntimeObjectReference? restrictedErrorObject, out bool isLanguageException))
            {
                // Capture the C# language exception if it hasn't already been captured previously either during the
                // throw or during a propagation. Given the C# exception itself captures propagation context on rethrow,
                // we don't do it each time.
                if (!isLanguageException)
                {
                    if (IUnknownVftbl.QueryInterfaceUnsafe(
                        thisPtr: restrictedErrorObject.GetThisPtrUnsafe(),
                        iid: in WellKnownWindowsInterfaceIIDs.IID_ILanguageExceptionErrorInfo2,
                        pvObject: out void* languageErrorInfo2Ptr).Succeeded())
                    {
                        // If the error object supports 'ILanguageExceptionErrorInfo2', marshal the exception and pass it to 'CapturePropagationContext'
                        try
                        {
                            using WindowsRuntimeObjectReferenceValue exceptionValue = WindowsRuntimeUnknownMarshaller.ConvertToUnmanaged(exception);

                            ((ILanguageExceptionErrorInfo2Vftbl*)*(void***)languageErrorInfo2Ptr)->CapturePropagationContext(
                                languageErrorInfo2Ptr,
                                exceptionValue.GetThisPtrUnsafe()).Assert();
                        }
                        finally
                        {
                            _ = IUnknownVftbl.ReleaseUnsafe(languageErrorInfo2Ptr);
                        }
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

                using WindowsRuntimeObjectReferenceValue restrictedErrorObjectValue = restrictedErrorObject.AsValue();

                _ = WindowsRuntimeImports.SetRestrictedErrorInfo(restrictedErrorObjectValue.GetThisPtrUnsafe());
            }
            else
            {
                string message = exception.Message;

                // If the exception has no message, we fallback to the exception type name
                if (string.IsNullOrEmpty(message))
                {
                    message = exception.GetType().Name;
                }

                // In this case we have no existing 'IRestrictedErrorInfo' value, so we capture a new C# language
                // exception from this location, for the current exception, to flow the error info from now on.
                fixed (char* lpMessage = message)
                {
                    HStringMarshaller.ConvertToUnmanagedUnsafe(lpMessage, message.Length, out HStringReference hstring);

                    using WindowsRuntimeObjectReferenceValue exceptionValue = WindowsRuntimeUnknownMarshaller.ConvertToUnmanaged(exception);

                    _ = WindowsRuntimeImports.RoOriginateLanguageException(
                        error: GetHRForException(exception),
                        message: hstring.HString,
                        languageException: exceptionValue.GetThisPtrUnsafe());
                }
            }
        }
        catch (Exception e)
        {
            // If we fail to set the error info, we continue on reporting the original exception
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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is <see langword="null"/>.</exception>
    /// <see href="https://learn.microsoft.com/windows/win32/api/roerrorapi/nf-roerrorapi-roreportunhandlederror"/>
    /// <remarks>
    /// This method should only be called from custom dispatchers and other top-level delegate invokers that should
    /// trigger an application-wide crash in case of failures. For better error reporting, especially in XAML scenarios,
    /// it is recommended to also call <see cref="ExceptionHandling.RaiseAppDomainUnhandledExceptionEvent"/> right before
    /// calling this method, so that the correct stacktrace will also be captured in crash reports for later inspection.
    /// </remarks>
    public static void ReportUnhandledError(Exception exception)
    {
        SetErrorInfo(exception);

        using WindowsRuntimeObjectReferenceValue restrictedErrorInfoValue = RestrictedErrorInfoHelpers.BorrowErrorInfo();

        void* restrictedErrorInfoValuePtr = restrictedErrorInfoValue.GetThisPtrUnsafe();

        if (restrictedErrorInfoValuePtr is not null)
        {
            _ = WindowsRuntimeImports.RoReportUnhandledError(restrictedErrorInfoValuePtr);
        }
    }
}