// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.ProjectionGenerator.Errors;

/// <summary>
/// A custom <see cref="Exception"/> type for well-known projection generator errors.
/// </summary>
/// <param name="message">The error message for the exception.</param>
/// <param name="innerException">The optional inner exception.</param>
internal sealed class WellKnownProjectionException(string message, Exception? innerException = null) : Exception(message, innerException);
