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
        /// Gets a value indicating whether an exception is well known (and should therefore not be caught).
        /// </summary>
        public bool IsWellKnown => exception is OperationCanceledException or WellKnownInteropException;
    }
}