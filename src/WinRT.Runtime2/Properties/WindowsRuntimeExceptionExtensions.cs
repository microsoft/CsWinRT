// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// Exception extensions for Windows Runtime exception checks using <see cref="WindowsRuntimeExceptionMessages"/> values.
/// </summary>
internal static class WindowsRuntimeExceptionExtensions
{
    extension(NullReferenceException)
    {
        /// <summary>
        /// Throws a <see cref="NullReferenceException"/> if <paramref name="task"/> is <see langword="null"/>.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to check for <see langword="null"/>.</param>
        /// <exception cref="NullReferenceException">Thrown if <paramref name="task"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTaskProviderReturnedNull([NotNull] Task? task)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNullReferenceException()
                => throw new NullReferenceException(WindowsRuntimeExceptionMessages.NullReference_TaskProviderReturnedNull);

            if (task is null)
            {
                ThrowNullReferenceException();
            }
        }
    }

    extension(InvalidOperationException)
    {
        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the specified <paramref name="task"/> has not been started.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="task"/> has a status of <see cref="TaskStatus.Created"/>.</exception>
        /// <remarks>
        /// This method is used when the task is created by a task provider delegate.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTaskProviderReturnedUnstartedTask(Task task)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowInvalidOperationException()
                => throw new InvalidOperationException(WindowsRuntimeExceptionMessages.InvalidOperation_TaskProviderReturnedUnstartedTask);

            if (task.Status == TaskStatus.Created)
            {
                ThrowInvalidOperationException();
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the specified <paramref name="task"/> has not been started.
        /// </summary>
        /// <param name="task">The <see cref="Task"/> to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="task"/> has a status of <see cref="TaskStatus.Created"/>.</exception>
        /// <remarks>
        /// This method is used when the task is directly provided (as opposed to being created by a task provider delegate).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfUnstartedTaskSpecified(Task task)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowInvalidOperationException()
                => throw new InvalidOperationException(WindowsRuntimeExceptionMessages.InvalidOperation_UnstartedTaskSpecified);

            if (task.Status == TaskStatus.Created)
            {
                ThrowInvalidOperationException();
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> indicating that an illegal state change was attempted.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown with an HResult of <c>E_ILLEGAL_STATE_CHANGE</c>.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowIllegalStateChange()
        {
            InvalidOperationException exception = new(WindowsRuntimeExceptionMessages.InvalidOperation_IllegalStateChange)
            {
                HResult = WellKnownErrorCodes.E_ILLEGAL_STATE_CHANGE
            };

            throw exception;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> indicating that the completion handler has already been set.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown with an HResult of <c>E_ILLEGAL_DELEGATE_ASSIGNMENT</c>.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowCannotSetCompletionHandlerMoreThanOnce()
        {
            InvalidOperationException exception = new(WindowsRuntimeExceptionMessages.InvalidOperation_CannotSetCompletionHanlderMoreThanOnce)
            {
                HResult = WellKnownErrorCodes.E_ILLEGAL_DELEGATE_ASSIGNMENT
            };

            throw exception;
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> indicating that results cannot be obtained from an incomplete operation.
        /// </summary>
        /// <param name="innerException">The inner exception, if available.</param>
        /// <exception cref="InvalidOperationException">Always thrown with an HResult of <c>E_ILLEGAL_METHOD_CALL</c>.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowCannotGetResultsFromIncompleteOperation(Exception? innerException = null)
        {
            throw GetCannotGetResultsFromIncompleteOperationException(innerException);
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> indicating that results cannot be obtained from an incomplete operation.
        /// </summary>
        /// <param name="innerException">The inner exception, if available.</param>
        /// <returns>The resulting <see cref="InvalidOperationException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetCannotGetResultsFromIncompleteOperationException(Exception? innerException = null)
        {
            InvalidOperationException exception = innerException is null
                ? new InvalidOperationException(WindowsRuntimeExceptionMessages.InvalidOperation_CannotGetResultsFromIncompleteOperation)
                : new InvalidOperationException(WindowsRuntimeExceptionMessages.InvalidOperation_CannotGetResultsFromIncompleteOperation, innerException);

            exception.HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL;

            return exception;
        }
    }

    extension(ObjectDisposedException)
    {
        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the async info is in the closed state.
        /// </summary>
        /// <param name="isClosed">Whether the async info is in the closed state.</param>
        /// <exception cref="ObjectDisposedException">Thrown if <paramref name="isClosed"/> is <see langword="true"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfAsyncInfoIsClosed(bool isClosed)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowObjectDisposedException()
            {
                ObjectDisposedException exception = new(WindowsRuntimeExceptionMessages.ObjectDisposed_AsyncInfoIsClosed)
                {
                    HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL
                };

                throw exception;
            }

            if (isClosed)
            {
                ThrowObjectDisposedException();
            }
        }
    }

    extension(ArgumentOutOfRangeException)
    {
        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="length"/> exceeds <paramref name="capacity"/>.
        /// </summary>
        /// <param name="length">The specified buffer length.</param>
        /// <param name="capacity">The maximum buffer capacity.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length"/> exceeds <paramref name="capacity"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfBufferLengthExceedsCapacity(uint length, uint capacity, [CallerArgumentExpression(nameof(length))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName)
            {
                ArgumentOutOfRangeException exception = new(paramName, WindowsRuntimeExceptionMessages.Argument_BufferLengthExceedsCapacity)
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };

                throw exception;
            }

            if (length > capacity)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }
    }
}
