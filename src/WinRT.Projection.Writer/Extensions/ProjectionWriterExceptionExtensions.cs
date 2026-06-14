// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.ProjectionWriter.Errors;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="Exception"/>.
/// </summary>
internal static class ProjectionWriterExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// Gets a value indicating whether an exception is well known (and should therefore not be caught).
        /// </summary>
        public bool IsWellKnown => exception is OperationCanceledException or WellKnownProjectionWriterException;
    }
}
