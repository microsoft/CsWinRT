// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.GeneratorCli.Errors;

/// <summary>
/// Shared extensions for CsWinRT CLI generator exceptions.
/// </summary>
internal static class GeneratorExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// Gets a value indicating whether an exception is well known (and should therefore not be caught).
        /// </summary>
        public bool IsWellKnown => exception is OperationCanceledException or WellKnownGeneratorException;
    }
}
