// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for interop exceptions.
/// </summary>
internal static class InteropExceptionExtensions
{
    /// <summary>
    /// Checks whether an exception is well known (and should therefore not be caught).
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>Whether <paramref name="exception"/> is well known.</returns>
    public static bool IsWellKnown(this Exception exception)
    {
        return exception is OperationCanceledException or WellKnownInteropException;
    }
}
