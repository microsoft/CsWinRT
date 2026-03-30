// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using WindowsRuntime;

namespace Windows.Storage.Streams;

/// <summary>
/// Helpers for working with Windows Runtime I/O operations.
/// </summary>
public static class WindowsRuntimeIOHelpers
{
    /// <summary>
    /// The default buffer size for stream adapters (16 KB).
    /// </summary>
    public const int DefaultBufferSize = 16384;

    /// <summary>
    /// Gets the best possible <see cref="ExceptionDispatchInfo"/> info from a given I/O exception.
    /// </summary>
    /// <param name="exception">The input exception to use as source.</param>
    /// <returns>The resulting <see cref="ExceptionDispatchInfo"/> instance.</returns>
    public static ExceptionDispatchInfo GetExceptionDispatchInfo(Exception exception)
    {
        Debug.Assert(exception is not null);

        // If the interop layer gave us a specific exception type, we assume it knew what it was doing.
        // If it instead it just gave us a generic 'Exception', we assume that it hit a general or
        // unknown case, and wrap it into an 'IOException', as this is what 'Stream' users expect.
        // 
        // We will return a captured 'ExceptionDispatchInfo' object that users can invoke 'Throw()' on.
        if (exception.GetType() != typeof(Exception))
        {
            return ExceptionDispatchInfo.Capture(exception);
        }

        string message = exception.Message;

        // If we do not have a meaningful message, we use a general IO error message
        if (string.IsNullOrWhiteSpace(message))
        {
            message = WindowsRuntimeExceptionMessages.IO_General;
        }

        // Manually create an 'IOException' with the original exception as inner exception
        return ExceptionDispatchInfo.Capture(new IOException(message, exception));
    }

    /// <summary>
    /// Ensures that the data from a given result buffer is present in the user buffer, copying it over if necessary.
    /// </summary>
    /// <param name="userBuffer">The input user buffer.</param>
    /// <param name="resultBuffer">The input result buffer to potentially copy to <paramref name="userBuffer"/>.</param>
    public static void EnsureResultsInUserBuffer(IBuffer userBuffer, IBuffer resultBuffer)
    {
        Debug.Assert(userBuffer is not null);
        Debug.Assert(resultBuffer is not null);

        // If the two buffers represent the same data, then we can skip the copy operation entirely
        if (resultBuffer.IsSameData(userBuffer))
        {
            return;
        }

        resultBuffer.CopyTo(userBuffer);

        // 'CopyTo' will only update the 'Length' property if the copy operation results in
        // more data being copied than the original value of the 'Length' property indicated.
        // However here we always want to ensure that 'Length' exactly reflects the results.
        userBuffer.Length = resultBuffer.Length;
    }
}
#endif