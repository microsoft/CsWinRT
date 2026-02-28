// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
}
