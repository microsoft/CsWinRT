// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text;

namespace WindowsRuntime.InteropGenerator.Errors;

/// <summary>
/// A well known exception for the interop generator.
/// </summary>
internal sealed class WellKnownInteropException : Exception
{
    /// <summary>
    /// The outer exception to include in the output, if available.
    /// </summary>
    private WellKnownInteropException? _outerException;

    /// <summary>
    /// Creates a new <see cref="WellKnownInteropException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="id">The id of the exception.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WellKnownInteropException(string id, string message, Exception? innerException)
        : base(message, innerException)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the id of the exception.
    /// </summary>
    public string Id { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        // This method is only called when logging an exception message in case the
        // tool fails. In that case, performance doesn't matter, so we can just use
        // a normal 'StringBuilder' for convenience, no need to micro-optimize things.
        StringBuilder builder = new();

        // Always append the current exception info first
        _ = builder.Append($"""error {Id}: {Message}""");

        // If we have an inner (not well-known) exception, append its info.
        // We only do this if the inner exception is not the same as the
        // parent exception. That can happen when re-throwing exceptions.
        // In that case, we only show the parent exception info below.
        if (InnerException is Exception exception && exception != _outerException)
        {
            _ = builder.Append($""" Inner exception: '{exception.GetType().Name}': '{exception.Message}'.""");
        }

        // If we have a parent well-known exception, append its info next
        if (_outerException is not null)
        {
            _ = builder.Append($""" Outer exception: '{_outerException.Id}': '{_outerException.Message}'.""");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Throws this exception or attaches it as a parent to the specified exception before re-throwing it.
    /// </summary>
    /// <param name="exception">The exception to be re-thrown, if applicable.</param>
    [StackTraceHidden]
    [DoesNotReturn]
    public void ThrowOrAttach(Exception exception)
    {
        // For cancellation exceptions, we just always re-throw them as is
        if (exception is OperationCanceledException)
        {
            ExceptionDispatchInfo.Throw(exception);
        }

        // For well-known exceptions, we attach the current exception as the parent and
        // then re-throw them. This allows the original exception to be the one that
        // causes the failure, but with the additional context on the outer exception
        // scope from the current exception also being included in the exception message.
        if (exception is WellKnownInteropException originalException)
        {
            originalException._outerException = this;

            ExceptionDispatchInfo.Throw(exception);
        }

        // In all other cases, we just throw the current exception from this location.
        // We don't need to capture the input exception, as that would've already
        // been used as the inner exception by callers, when constructing this instance.
        throw this;
    }
}