// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// Extension methods for <see cref="Exception"/> types used in error handling.
/// </summary>
internal static class ExceptionExtensions
{
    /// <summary>
    /// Checks whether the given exception is a well-known projection generator exception.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>Whether the exception is a <see cref="WellKnownProjectionException"/>.</returns>
    public static bool IsWellKnown(this Exception exception)
    {
        return exception is WellKnownProjectionException;
    }
}
