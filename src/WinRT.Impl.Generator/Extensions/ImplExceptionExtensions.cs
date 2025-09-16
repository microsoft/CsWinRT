// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ImplGenerator.Errors;

namespace WindowsRuntime.ImplGenerator;

/// <summary>
/// Extensions for interop exceptions.
/// </summary>
internal static class ImplExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// Gets a value indicating whether an exception is well known (and should therefore not be caught).
        /// </summary>
        public bool IsWellKnown => exception is OperationCanceledException or WellKnownImplException;
    }
}
