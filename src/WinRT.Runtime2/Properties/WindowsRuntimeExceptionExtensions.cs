// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Windows.Storage.Streams;
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
        [StackTraceHidden]
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
        [StackTraceHidden]
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
        [StackTraceHidden]
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
            InvalidOperationException exception = new(WindowsRuntimeExceptionMessages.InvalidOperation_CannotSetCompletionHandlerMoreThanOnce)
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

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the specified collection size exceeds <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="size">The collection size to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="size"/> exceeds <see cref="int.MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfCollectionBackingListTooLarge(uint size)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowInvalidOperationException()
                => throw new InvalidOperationException(WindowsRuntimeExceptionMessages.InvalidOperation_CollectionBackingListTooLarge);

            if (size > int.MaxValue)
            {
                ThrowInvalidOperationException();
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the specified <paramref name="count"/> is zero.
        /// </summary>
        /// <param name="count">The collection count to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="count"/> is zero, with an HResult of <c>E_BOUNDS</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfCannotRemoveLastFromEmptyCollection(int count)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowInvalidOperationException()
            {
                InvalidOperationException exception = new(WindowsRuntimeExceptionMessages.InvalidOperation_CannotRemoveLastFromEmptyCollection)
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };

                throw exception;
            }

            if (count == 0)
            {
                ThrowInvalidOperationException();
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the stream does not support writing for a resize operation.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="stream"/> does not support writing, with an HResult of <c>E_ILLEGAL_METHOD_CALL</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotWriteForResize(Stream stream)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowInvalidOperationException()
            {
                InvalidOperationException exception = new(WindowsRuntimeExceptionMessages.InvalidOperation_CannotSetStreamSizeCannotWrite)
                {
                    HResult = WellKnownErrorCodes.E_ILLEGAL_METHOD_CALL
                };

                throw exception;
            }

            if (!stream.CanWrite)
            {
                ThrowInvalidOperationException();
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the buffer has been invalidated.
        /// </summary>
        /// <param name="data">The pointer to the buffer data.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="data"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static unsafe void ThrowIfBufferIsInvalidated(byte* data)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowInvalidOperationException()
                => throw new InvalidOperationException(WindowsRuntimeExceptionMessages.InvalidOperation_CannotAccessInvalidatedBuffer);

            if (data is null)
            {
                ThrowInvalidOperationException();
            }
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> indicating that the method cannot be called in the current state.
        /// </summary>
        /// <returns>The resulting <see cref="InvalidOperationException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetCannotCallThisMethodInCurrentStateException()
        {
            return new(WindowsRuntimeExceptionMessages.InvalidOperation_CannotCallThisMethodInCurrentState);
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> indicating that the I/O completion callback was invoked more than once.
        /// </summary>
        /// <returns>The resulting <see cref="InvalidOperationException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetMultipleIOCompletionCallbackInvocationException()
        {
            return new(WindowsRuntimeExceptionMessages.InvalidOperation_MultipleIOCompletionCallbackInvocation);
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> indicating that the buffer size of a stream adapter cannot be changed.
        /// </summary>
        /// <param name="methodName">The name of the method being called.</param>
        /// <returns>The resulting <see cref="InvalidOperationException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetCannotChangeBufferSizeOfStreamAdapterException(string methodName)
        {
            return new(string.Format(WindowsRuntimeExceptionMessages.InvalidOperation_CannotChangeBufferSizeOfStreamAdapter, methodName));
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> indicating that the buffer size of a stream adapter cannot be changed to zero.
        /// </summary>
        /// <param name="methodName">The name of the method being called.</param>
        /// <returns>The resulting <see cref="InvalidOperationException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static InvalidOperationException GetCannotChangeBufferSizeOfStreamAdapterToZeroException(string methodName)
        {
            return new(string.Format(WindowsRuntimeExceptionMessages.InvalidOperation_CannotChangeBufferSizeOfStreamAdapterToZero, methodName));
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
        [StackTraceHidden]
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

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if the stream has been disposed (ie. the stream reference is <see langword="null"/>).
        /// </summary>
        /// <param name="stream">The stream reference to check.</param>
        /// <exception cref="ObjectDisposedException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamIsDisposed([NotNull] object? stream)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowObjectDisposedException()
                => throw new ObjectDisposedException(WindowsRuntimeExceptionMessages.ObjectDisposed_CannotPerformOperationOnDisposedStream);

            if (stream is null)
            {
                ThrowObjectDisposedException();
            }
        }

        /// <summary>
        /// Creates an <see cref="ObjectDisposedException"/> indicating that the stream has been disposed, with an HResult of <c>RO_E_CLOSED</c>.
        /// </summary>
        /// <returns>The resulting <see cref="ObjectDisposedException"/> instance with an HResult of <c>RO_E_CLOSED</c>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ObjectDisposedException GetStreamIsClosedException()
        {
            return new ObjectDisposedException(WindowsRuntimeExceptionMessages.ObjectDisposed_CannotPerformOperationOnDisposedStream)
            {
                HResult = WellKnownErrorCodes.RO_E_CLOSED
            };
        }

        /// <summary>
        /// Creates an <see cref="ObjectDisposedException"/> indicating that the stream has been disposed.
        /// </summary>
        /// <returns>The resulting <see cref="ObjectDisposedException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ObjectDisposedException GetStreamIsDisposedException()
        {
            return new ObjectDisposedException(WindowsRuntimeExceptionMessages.ObjectDisposed_CannotPerformOperationOnDisposedStream);
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
        [StackTraceHidden]
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

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="index"/> is out of range for a collection of the specified <paramref name="count"/>.
        /// </summary>
        /// <param name="index">The index to check.</param>
        /// <param name="count">The size of the collection.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range, with an HResult of <c>E_BOUNDS</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfIndexLargerThanMaxValue(uint index, int count, [CallerArgumentExpression(nameof(index))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName)
            {
                ArgumentOutOfRangeException exception = new(paramName, WindowsRuntimeExceptionMessages.ArgumentOutOfRange_IndexLargerThanMaxValue)
                {
                    HResult = WellKnownErrorCodes.E_BOUNDS
                };

                throw exception;
            }

            if (index >= (uint)count)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ArgumentOutOfRangeException"/> with the specified parameter name.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <returns>The resulting <see cref="ArgumentOutOfRangeException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentOutOfRangeException GetArgumentOutOfRangeException(string? paramName)
        {
            return new ArgumentOutOfRangeException(paramName);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified buffer <paramref name="length"/> exceeds <see cref="Array.MaxLength"/>.
        /// </summary>
        /// <param name="length">The buffer length to check.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length"/> exceeds <see cref="Array.MaxLength"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfBufferLengthExceedsArrayMaxLength(uint length)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException()
                => throw new ArgumentOutOfRangeException(null, WindowsRuntimeExceptionMessages.ArgumentOutOfRange_BufferLengthExceedsArrayMaxLength);

            if (length > Array.MaxLength)
            {
                ThrowArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <paramref name="count"/> exceeds <see cref="int.MaxValue"/>.
        /// </summary>
        /// <param name="count">The count value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> exceeds <see cref="int.MaxValue"/>, with an HResult of <c>E_INVALIDARG</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfCountExceedsInt32MaxValue(uint count, [CallerArgumentExpression(nameof(count))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName)
            {
                ArgumentOutOfRangeException exception = new(paramName)
                {
                    HResult = WellKnownErrorCodes.E_INVALIDARG
                };

                throw exception;
            }

            if (count > int.MaxValue)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <see cref="InputStreamOptions"/> value is not valid.
        /// </summary>
        /// <param name="options">The options value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="options"/> is not a valid value, with an HResult of <c>E_INVALIDARG</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        [SupportedOSPlatform("windows10.0.10240.0")]
        public static void ThrowIfInvalidInputStreamOptions(InputStreamOptions options, [CallerArgumentExpression(nameof(options))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName)
            {
                ArgumentOutOfRangeException exception = new(paramName, WindowsRuntimeExceptionMessages.ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue)
                {
                    HResult = WellKnownErrorCodes.E_INVALIDARG
                };

                throw exception;
            }

            if (options is not (InputStreamOptions.None or InputStreamOptions.Partial or InputStreamOptions.ReadAhead))
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified stream position <paramref name="value"/> is negative.
        /// </summary>
        /// <param name="value">The position value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfNegativeStreamPosition(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName)
                => throw new ArgumentOutOfRangeException(paramName, WindowsRuntimeExceptionMessages.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);

            if (value < 0)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified stream length <paramref name="value"/> is negative.
        /// </summary>
        /// <param name="value">The length value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="value"/> is negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfNegativeStreamLength(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName)
                => throw new ArgumentOutOfRangeException(paramName, WindowsRuntimeExceptionMessages.ArgumentOutOfRange_CannotResizeStreamToNegative);

            if (value < 0)
            {
                ThrowArgumentOutOfRangeException(paramName);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <see cref="FileShare"/> value is out of the valid range.
        /// </summary>
        /// <param name="share">The <see cref="FileShare"/> value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="share"/> is not a valid combination of flags.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfFileShareOutOfRange(FileShare share, [CallerArgumentExpression(nameof(share))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName, FileShare share)
                => throw new ArgumentOutOfRangeException(paramName, share, null);

            if (share is < FileShare.None or > (FileShare.ReadWrite | FileShare.Delete))
            {
                ThrowArgumentOutOfRangeException(paramName, share);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <see cref="FileOptions"/> value contains unsupported flags.
        /// </summary>
        /// <param name="options">The <see cref="FileOptions"/> value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="options"/> contains unsupported flags.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfFileOptionsOutOfRange(FileOptions options, [CallerArgumentExpression(nameof(options))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName, FileOptions options)
                => throw new ArgumentOutOfRangeException(paramName, options, null);

            if (options != FileOptions.None && (options &
                ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose |
                  FileOptions.SequentialScan | (FileOptions)0x20000000 /* NoBuffering */)) != 0)
            {
                ThrowArgumentOutOfRangeException(paramName, options);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified <see cref="FileMode"/> value is out of the valid range.
        /// </summary>
        /// <param name="mode">The <see cref="FileMode"/> value to check.</param>
        /// <param name="paramName">The name of the parameter being checked.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="mode"/> is not a valid value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfFileModeOutOfRange(FileMode mode, [CallerArgumentExpression(nameof(mode))] string? paramName = null)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException(string? paramName, FileMode mode)
                => throw new ArgumentOutOfRangeException(paramName, mode, null);

            if (mode is < FileMode.CreateNew or > FileMode.Append)
            {
                ThrowArgumentOutOfRangeException(paramName, mode);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ArgumentOutOfRangeException"/> for an invalid <see cref="FileAccess"/> value.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="access">The invalid <see cref="FileAccess"/> value.</param>
        /// <returns>The resulting <see cref="ArgumentOutOfRangeException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentOutOfRangeException GetFileAccessOutOfRangeException(string? paramName, FileAccess access)
        {
            return new ArgumentOutOfRangeException(paramName, access, null);
        }
    }

    extension(KeyNotFoundException)
    {
        /// <summary>
        /// Throws a <see cref="KeyNotFoundException"/> indicating that the given key was not present in the dictionary.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Always thrown with an HResult of <c>E_BOUNDS</c>.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowKeyNotFound()
        {
            KeyNotFoundException exception = new(WindowsRuntimeExceptionMessages.Arg_KeyNotFoundWithKey)
            {
                HResult = WellKnownErrorCodes.E_BOUNDS
            };

            throw exception;
        }

        /// <summary>
        /// Creates a <see cref="KeyNotFoundException"/> indicating that the given key was not present in the dictionary.
        /// </summary>
        /// <param name="innerException">The inner exception, if available.</param>
        /// <returns>The resulting <see cref="KeyNotFoundException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static KeyNotFoundException GetKeyNotFoundException(Exception? innerException = null)
        {
            return innerException is null
                ? new KeyNotFoundException(WindowsRuntimeExceptionMessages.Arg_KeyNotFoundWithKey)
                : new KeyNotFoundException(WindowsRuntimeExceptionMessages.Arg_KeyNotFoundWithKey, innerException);
        }
    }

    extension(ArgumentException)
    {
        /// <summary>
        /// Throws an <see cref="ArgumentException"/> indicating that a duplicate key was added to a dictionary.
        /// </summary>
        /// <exception cref="ArgumentException">Always thrown.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowAddingDuplicate()
        {
            throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_AddingDuplicate);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> indicating that the specified index is out of bounds of the array.
        /// </summary>
        /// <exception cref="ArgumentException">Always thrown.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowIndexOutOfArrayBounds()
        {
            throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_IndexOutOfArrayBounds);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> indicating that there is insufficient space to copy the collection.
        /// </summary>
        /// <exception cref="ArgumentException">Always thrown.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowInsufficientSpaceToCopyCollection()
        {
            throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InsufficientSpaceToCopyCollection);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified <paramref name="rank"/> is not 1.
        /// </summary>
        /// <param name="rank">The array rank to check.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="rank"/> is not 1.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfRankMultiDimNotSupported(int rank)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Arg_RankMultiDimNotSupported);

            if (rank != 1)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the array does not have enough elements after the specified <paramref name="offset"/>.
        /// </summary>
        /// <param name="totalLength">The total length of the array.</param>
        /// <param name="offset">The offset into the array.</param>
        /// <param name="required">The number of required elements after the offset.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="totalLength"/> minus <paramref name="offset"/> is less than <paramref name="required"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfInsufficientArrayElementsAfterOffset(int totalLength, int offset, int required)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InsufficientArrayElementsAfterOffset);

            if (totalLength - offset < required)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the buffer capacity is insufficient for the specified length.
        /// </summary>
        /// <param name="capacity">The buffer capacity.</param>
        /// <param name="length">The required length.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="capacity"/> is less than <paramref name="length"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfInsufficientBufferCapacity(int capacity, int length)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InsufficientBufferCapacity);

            if (capacity < length)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified buffer <paramref name="index"/> exceeds the buffer <paramref name="capacity"/>.
        /// </summary>
        /// <param name="index">The buffer index to check.</param>
        /// <param name="capacity">The buffer capacity.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="index"/> is greater than <paramref name="capacity"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfBufferIndexExceedsCapacity(uint index, uint capacity)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_BufferIndexExceedsCapacity);

            if (index > capacity)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified buffer <paramref name="index"/> exceeds the buffer <paramref name="length"/>.
        /// </summary>
        /// <param name="index">The buffer index to check.</param>
        /// <param name="length">The buffer length.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="index"/> is greater than <paramref name="length"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfBufferIndexExceedsLength(uint index, uint length)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_BufferIndexExceedsLength);

            if (index > length)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified buffer <paramref name="offset"/> is not within the buffer <paramref name="length"/>.
        /// </summary>
        /// <param name="offset">The buffer offset to check.</param>
        /// <param name="length">The buffer length.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="offset"/> is greater than or equal to <paramref name="length"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfBufferOffsetOutOfRange(uint offset, uint length)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_BufferOffsetExceedsLength);

            if (offset >= length)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if there is insufficient space in the target buffer.
        /// </summary>
        /// <param name="capacity">The total capacity of the target buffer.</param>
        /// <param name="index">The starting index in the target buffer.</param>
        /// <param name="required">The number of elements to write.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="capacity"/> minus <paramref name="index"/> is less than <paramref name="required"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfInsufficientSpaceInTargetBuffer(uint capacity, uint index, uint required)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InsufficientSpaceInTargetBuffer);

            if (capacity - index < required)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if there is insufficient space in the source buffer.
        /// </summary>
        /// <param name="length">The total length of the source buffer.</param>
        /// <param name="index">The starting index in the source buffer.</param>
        /// <param name="required">The number of elements to read.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="length"/> minus <paramref name="index"/> is less than <paramref name="required"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfInsufficientSpaceInSourceBuffer(uint length, uint index, uint required)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InsufficientSpaceInSourceBuffer);

            if (length - index < required)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified stream position is beyond the end of the stream.
        /// </summary>
        /// <param name="streamLength">The length of the stream.</param>
        /// <param name="position">The position to check.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="streamLength"/> is less than <paramref name="position"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamPositionBeyondEndOfStream(long streamLength, int position)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_StreamPositionBeyondEndOfStream);

            if (streamLength < position)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that the provided <c>IBuffer</c> instance is not valid.
        /// </summary>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetInvalidIBufferInstanceException()
        {
            return new(WindowsRuntimeExceptionMessages.Argument_InvalidIBufferInstance);
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the buffer capacity is insufficient for the specified count.
        /// </summary>
        /// <param name="bufferCapacity">The buffer capacity.</param>
        /// <param name="requiredCount">The required count.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="bufferCapacity"/> is less than <paramref name="requiredCount"/>, with an HResult of <c>E_INVALIDARG</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfBufferCapacityInsufficient(uint bufferCapacity, uint requiredCount)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
            {
                ArgumentException exception = new(WindowsRuntimeExceptionMessages.Argument_InsufficientBufferCapacity)
                {
                    HResult = WellKnownErrorCodes.E_INVALIDARG
                };

                throw exception;
            }

            if (bufferCapacity < requiredCount)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the buffer length exceeds the buffer capacity.
        /// </summary>
        /// <param name="bufferCapacity">The buffer capacity.</param>
        /// <param name="bufferLength">The buffer length.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="bufferCapacity"/> is less than <paramref name="bufferLength"/>, with an HResult of <c>E_INVALIDARG</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfBufferLengthExceedsBufferCapacity(uint bufferCapacity, uint bufferLength)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
            {
                ArgumentException exception = new(WindowsRuntimeExceptionMessages.Argument_BufferLengthExceedsCapacity)
                {
                    HResult = WellKnownErrorCodes.E_INVALIDARG
                };

                throw exception;
            }

            if (bufferCapacity < bufferLength)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified value exceeds <see cref="long.MaxValue"/>.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> exceeds <see cref="long.MaxValue"/>, with an HResult of <c>E_INVALIDARG</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfSizeExceedsInt64MaxValue(ulong value)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
            {
                ArgumentException exception = new(WindowsRuntimeExceptionMessages.IO_CannotSetSizeBeyondInt64MaxValue)
                {
                    HResult = WellKnownErrorCodes.E_INVALIDARG
                };

                throw exception;
            }

            if (value > long.MaxValue)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified stream position exceeds <see cref="long.MaxValue"/>.
        /// </summary>
        /// <param name="position">The position to check.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="position"/> exceeds <see cref="long.MaxValue"/>, with an HResult of <c>E_INVALIDARG</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfPositionExceedsInt64MaxValue(ulong position)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
            {
                ArgumentException exception = new(WindowsRuntimeExceptionMessages.IO_CannotSeekBeyondInt64MaxValue)
                {
                    HResult = WellKnownErrorCodes.E_INVALIDARG
                };

                throw exception;
            }

            if (position > long.MaxValue)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the buffer does not have enough space for the specified read operation.
        /// </summary>
        /// <param name="totalLength">The total length of the buffer.</param>
        /// <param name="offset">The offset into the buffer.</param>
        /// <param name="required">The number of required elements after the offset.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="totalLength"/> minus <paramref name="offset"/> is less than <paramref name="required"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfInsufficientSpaceInTargetBuffer(int totalLength, int offset, int required)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
                => throw new ArgumentException(WindowsRuntimeExceptionMessages.Argument_InsufficientSpaceInTargetBuffer);

            if (totalLength - offset < required)
            {
                ThrowArgumentException();
            }
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that the stream does not have sufficient capabilities to convert to a Windows Runtime stream.
        /// </summary>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetNotSufficientCapabilitiesToConvertToWinRtStreamException()
        {
            return new(WindowsRuntimeExceptionMessages.Argument_NotSufficientCapabilitiesToConvertToWinRtStream);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that the specified object is not a Windows Runtime stream.
        /// </summary>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetObjectMustBeWinRtStreamException()
        {
            return new(WindowsRuntimeExceptionMessages.Argument_ObjectMustBeWinRtStreamToConvertToNetFxStream);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that an <c>IRandomAccessStream</c> reporting <c>CanRead</c> must also implement <c>IInputStream</c>.
        /// </summary>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetCanReadStreamMustImplementIInputStreamException()
        {
            return new(WindowsRuntimeExceptionMessages.Argument_InstancesImplementingIRASThatCanReadMustImplementIIS);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that an <c>IRandomAccessStream</c> reporting <c>CanWrite</c> must also implement <c>IOutputStream</c>.
        /// </summary>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetCanWriteStreamMustImplementIOutputStreamException()
        {
            return new(WindowsRuntimeExceptionMessages.Argument_InstancesImplementingIRASThatCanWriteMustImplementIOS);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that a Windows Runtime stream supports neither reading nor writing.
        /// </summary>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetWinRtStreamCannotReadOrWriteException()
        {
            return new(WindowsRuntimeExceptionMessages.Argument_WinRtStreamCannotReadOrWrite);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that the specified <see cref="IAsyncResult"/> is not expected.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetUnexpectedAsyncResultException(string? paramName)
        {
            return new(WindowsRuntimeExceptionMessages.Argument_UnexpectedAsyncResult, paramName);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> indicating that the specified seek origin is not valid.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <returns>The resulting <see cref="ArgumentException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentException GetInvalidSeekOriginException(string? paramName)
        {
            return new(WindowsRuntimeExceptionMessages.Argument_InvalidSeekOrigin, paramName);
        }
    }

    extension(UnauthorizedAccessException)
    {
        /// <summary>
        /// Throws an <see cref="UnauthorizedAccessException"/> indicating that the internal buffer of a <see cref="System.IO.MemoryStream"/> cannot be accessed.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Always thrown.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowInternalBufferAccess()
        {
            throw new UnauthorizedAccessException(WindowsRuntimeExceptionMessages.UnauthorizedAccess_InternalBuffer);
        }
    }

    extension(Win32Exception)
    {
        /// <summary>
        /// Throws a <see cref="Win32Exception"/> with the last system error code.
        /// </summary>
        /// <exception cref="Win32Exception">Always thrown.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static void ThrowLastWin32Error()
        {
            // The 'Win32Exception' constructor will automatically get the last system error
            throw new Win32Exception();
        }
    }

    extension(Exception)
    {
        /// <summary>
        /// Creates an <see cref="Exception"/> indicating that the index was out of range.
        /// </summary>
        /// <param name="innerException">The inner exception, if available.</param>
        /// <returns>The resulting <see cref="Exception"/> instance with an HResult of <c>E_BOUNDS</c>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception GetIndexOutOfRangeException(Exception? innerException = null)
        {
            return new Exception(WindowsRuntimeExceptionMessages.ArgumentOutOfRange_Index, innerException)
            {
                HResult = WellKnownErrorCodes.E_BOUNDS
            };
        }
    }

    extension(NotSupportedException)
    {
        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support reading.
        /// </summary>
        /// <param name="canRead">Whether the stream supports reading.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canRead"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotRead(bool canRead)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotReadFromStream);

            if (!canRead)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support writing.
        /// </summary>
        /// <param name="canWrite">Whether the stream supports writing.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canWrite"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotWrite(bool canWrite)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotWriteToStream);

            if (!canWrite)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support the <c>Length</c> property.
        /// </summary>
        /// <param name="canSeek">Whether the stream supports seeking.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canSeek"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotUseLength(bool canSeek)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotUseLength_StreamNotSeekable);

            if (!canSeek)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support the <c>Position</c> property.
        /// </summary>
        /// <param name="canSeek">Whether the stream supports seeking.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canSeek"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotUsePosition(bool canSeek)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotUsePosition_StreamNotSeekable);

            if (!canSeek)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support seeking.
        /// </summary>
        /// <param name="canSeek">Whether the stream supports seeking.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canSeek"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotSeek(bool canSeek)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotSeekInStream);

            if (!canSeek)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support reading for conversion to an <see cref="IInputStream"/>.
        /// </summary>
        /// <param name="canRead">Whether the stream supports reading.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canRead"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotConvertToInputStream(bool canRead)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotConvertNotReadableStreamToInputStream);

            if (!canRead)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support writing for conversion to an <see cref="IOutputStream"/>.
        /// </summary>
        /// <param name="canWrite">Whether the stream supports writing.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canWrite"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotConvertToOutputStream(bool canWrite)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotConvertNotWritableStreamToOutputStream);

            if (!canWrite)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the stream does not support seeking for conversion to an <see cref="IRandomAccessStream"/>.
        /// </summary>
        /// <param name="canSeek">Whether the stream supports seeking.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="canSeek"/> is <see langword="false"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfStreamCannotConvertToRandomAccessStream(bool canSeek)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_CannotConvertNotSeekableStreamToRandomAccessStream);

            if (!canSeek)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Creates a <see cref="NotSupportedException"/> indicating that the stream does not support cloning.
        /// </summary>
        /// <param name="methodName">The name of the method that requires cloning support.</param>
        /// <returns>The resulting <see cref="NotSupportedException"/> instance with an HResult of <c>E_NOTIMPL</c>.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static NotSupportedException GetCloningNotSupportedException(string methodName)
        {
            return new NotSupportedException(string.Format(WindowsRuntimeExceptionMessages.NotSupported_CloningNotSupported, methodName))
            {
                HResult = WellKnownErrorCodes.E_NOTIMPL
            };
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the specified <paramref name="share"/> value has the <see cref="FileShare.Inheritable"/> flag.
        /// </summary>
        /// <param name="share">The <see cref="FileShare"/> value to check.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="share"/> has the <see cref="FileShare.Inheritable"/> flag.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfFileShareIsInheritable(FileShare share)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_InheritableIsNotSupportedOption);

            if ((share & FileShare.Inheritable) != 0)
            {
                ThrowNotSupportedException();
            }
        }

        /// <summary>
        /// Throws a <see cref="NotSupportedException"/> if the specified <paramref name="options"/> value has the <see cref="FileOptions.Encrypted"/> flag.
        /// </summary>
        /// <param name="options">The <see cref="FileOptions"/> value to check.</param>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="options"/> has the <see cref="FileOptions.Encrypted"/> flag.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfFileOptionsAreEncrypted(FileOptions options)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException()
                => throw new NotSupportedException(WindowsRuntimeExceptionMessages.NotSupported_EncryptedIsNotSupportedOption);

            if ((options & FileOptions.Encrypted) != 0)
            {
                ThrowNotSupportedException();
            }
        }
    }

    extension(IOException)
    {
        /// <summary>
        /// Throws an <see cref="IOException"/> if the sum of <paramref name="basePosition"/> and <paramref name="offset"/> would exceed <see cref="long.MaxValue"/>.
        /// </summary>
        /// <param name="basePosition">The base position value.</param>
        /// <param name="offset">The offset value.</param>
        /// <exception cref="IOException">Thrown if <paramref name="basePosition"/> + <paramref name="offset"/> would exceed <see cref="long.MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfSeekWouldExceedMaxPosition(long basePosition, long offset)
        {
            if (long.MaxValue - basePosition < offset)
            {
                ThrowCannotSeekBeyondInt64MaxValue();
            }
        }

        /// <summary>
        /// Throws an <see cref="IOException"/> if the specified <paramref name="offset"/> is non-negative (indicating a forward seek from an oversized stream).
        /// </summary>
        /// <param name="offset">The seek offset value.</param>
        /// <exception cref="IOException">Thrown if <paramref name="offset"/> is greater than or equal to zero.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfNonNegativeOffsetForOversizedStream(long offset)
        {
            if (offset >= 0)
            {
                ThrowCannotSeekBeyondInt64MaxValue();
            }
        }

        /// <summary>
        /// Throws an <see cref="IOException"/> if the specified <paramref name="position"/> exceeds <see cref="long.MaxValue"/>.
        /// </summary>
        /// <param name="position">The position value to check.</param>
        /// <exception cref="IOException">Thrown if <paramref name="position"/> exceeds <see cref="long.MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfSeekPositionExceedsInt64MaxValue(ulong position)
        {
            if (position > long.MaxValue)
            {
                ThrowCannotSeekBeyondInt64MaxValue();
            }
        }

        /// <summary>
        /// Throws an <see cref="IOException"/> if the specified <paramref name="position"/> is negative.
        /// </summary>
        /// <param name="position">The position value to check.</param>
        /// <exception cref="IOException">Thrown if <paramref name="position"/> is negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfSeekResultNegative(long position)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowCannotSeekToNegativePosition()
                => throw new IOException(WindowsRuntimeExceptionMessages.ArgumentOutOfRange_IO_CannotSeekToNegativePosition);

            if (position < 0)
            {
                ThrowCannotSeekToNegativePosition();
            }
        }

        /// <summary>
        /// Throws an <see cref="IOException"/> if the specified <paramref name="value"/> exceeds <see cref="long.MaxValue"/>,
        /// indicating that the underlying Windows Runtime stream is too long to use length or position operations.
        /// </summary>
        /// <param name="value">The stream size or position value to check.</param>
        /// <exception cref="IOException">Thrown if <paramref name="value"/> exceeds <see cref="long.MaxValue"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        public static void ThrowIfUnderlyingWinRTStreamTooLong(ulong value)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowUnderlyingWinRTStreamTooLong()
                => throw new IOException(WindowsRuntimeExceptionMessages.IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition);

            if (value > long.MaxValue)
            {
                ThrowUnderlyingWinRTStreamTooLong();
            }
        }

#pragma warning disable IDE0051 // TODO: remove this once Roslyn bug is fixed
        /// <summary>
        /// Throw helper for <see cref="ThrowIfSeekPositionExceedsInt64MaxValue"/>.
        /// </summary>
        [DoesNotReturn]
        [StackTraceHidden]
        private static void ThrowCannotSeekBeyondInt64MaxValue()
        {
            throw new IOException(WindowsRuntimeExceptionMessages.IO_CannotSeekBeyondInt64MaxValue);
        }
#pragma warning restore IDE0051
    }

    extension(ArgumentNullException)
    {
        /// <summary>
        /// Creates an <see cref="ArgumentNullException"/> indicating that the I/O completion callback received a <see langword="null"/> async info.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <returns>The resulting <see cref="ArgumentNullException"/> instance.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentNullException GetIOCompletionCallbackCannotProcessNullAsyncInfoException(string? paramName)
        {
            return new(paramName, WindowsRuntimeExceptionMessages.ArgumentNullReference_IOCompletionCallbackCannotProcessNullAsyncInfo);
        }
    }

    extension(UnreachableException)
    {
        /// <summary>
        /// Throws an <see cref="UnreachableException"/>.
        /// </summary>
        /// <returns>This method never returns.</returns>
        /// <exception cref="UnreachableException">Always thrown.</exception>
        [DoesNotReturn]
        [StackTraceHidden]
        public static bool Throw()
        {
            throw new UnreachableException();
        }
    }
}
