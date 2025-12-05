// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator;

/// <summary>
/// Extensions for interop exceptions.
/// </summary>
internal static class ProjectionGeneratorExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// Gets a value indicating whether an exception is well known (and should therefore not be caught).
        /// </summary>
        public bool IsWellKnown => exception is OperationCanceledException or WellKnownProjectionGeneratorException;
    }
}
