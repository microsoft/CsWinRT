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
    extension(Exception exception)
    {
        /// <summary>
        /// Checks whether an exception is well known (and should therefore not be caught).
        /// </summary>
        /// <returns>Whether the exception is well known.</returns>
        public bool IsWellKnown => exception is OperationCanceledException or WellKnownInteropException;
    }
}
