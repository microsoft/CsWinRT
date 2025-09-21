// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// An unhandled exception for the interop generator.
/// </summary>
internal sealed class UnhandledInteropException : UnhandledGeneratorException
{
    /// <summary>
    /// Creates a new <see cref="UnhandledInteropException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="phase">The phase that failed.</param>
    /// <param name="exception">The inner exception.</param>
    public UnhandledInteropException(string phase, Exception exception)
        : base("interop", phase, exception)
    {
    }
}
