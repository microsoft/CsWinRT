// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Text;

namespace WindowsRuntime.ProjectionWriter.Errors;

/// <summary>
/// A well-known exception for the projection writer.
/// </summary>
internal sealed class WellKnownProjectionWriterException : Exception
{
    /// <summary>
    /// The outer exception to include in the output, if available.
    /// </summary>
    private WellKnownProjectionWriterException? _outerException;

    /// <summary>
    /// Creates a new <see cref="WellKnownProjectionWriterException"/> instance with the specified parameters.
    /// </summary>
    /// <param name="id">The id of the exception.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception, if any.</param>
    public WellKnownProjectionWriterException(string id, string message, Exception? innerException)
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

        _ = builder.Append($"""error {Id}: {Message}""");

        if (InnerException is Exception exception && exception != _outerException)
        {
            _ = builder.Append($""" Inner exception: '{exception.GetType().Name}': '{exception.Message}'.""");
        }

        if (_outerException is not null)
        {
            _ = builder.Append($""" Outer exception: '{_outerException.Id}': '{_outerException.Message}'.""");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Throws this exception, or attaches it as the parent of <paramref name="exception"/> before re-throwing it.
    /// </summary>
    /// <param name="exception">The exception to be re-thrown, if applicable.</param>
    [StackTraceHidden]
    [DoesNotReturn]
    public void ThrowOrAttach(Exception exception)
    {
        // For cancellation exceptions, we just always re-throw them as is.
        if (exception is OperationCanceledException)
        {
            ExceptionDispatchInfo.Throw(exception);
        }

        // For well-known exceptions, attach the current exception as the parent and re-throw.
        // This allows the original exception to be the one that causes the failure, but with
        // the additional context on the outer exception scope from the current exception also
        // being included in the exception message.
        if (exception is WellKnownProjectionWriterException originalException)
        {
            originalException._outerException = this;
            ExceptionDispatchInfo.Throw(exception);
        }

        // In all other cases, just throw the current exception from this location.
        // No need to capture the input exception -- callers will have already used it
        // as the inner exception when constructing this instance.
        throw this;
    }
}
