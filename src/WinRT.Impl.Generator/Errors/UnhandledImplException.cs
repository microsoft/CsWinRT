// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.Generator.Errors;

namespace WindowsRuntime.ImplGenerator.Errors;

/// <summary>
/// An unhandled exception for the impl generator.
/// </summary>
internal sealed class UnhandledImplException : UnhandledGeneratorException
{
    /// <summary>
    /// Creates a new <see cref="UnhandledImplException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="phase">The phase that failed.</param>
    /// <param name="exception">The inner exception.</param>
    public UnhandledImplException(string phase, Exception exception)
        : base("impl", phase, exception)
    {
    }
}
