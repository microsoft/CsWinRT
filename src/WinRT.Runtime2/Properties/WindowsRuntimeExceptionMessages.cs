// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime;

/// <summary>
/// Exception messages for internal exceptions thrown by APIs within 'WinRT.Runtime.dll'.
/// </summary>
internal static class WindowsRuntimeExceptionMessages
{
    public const string Arg_KeyNotFoundWithKey = "The given key was not present in the dictionary.";

    public const string Arg_RankMultiDimNotSupported = "Multi-dimensional arrays are not supported.";

    public const string Argument_AddingDuplicate = "An item with the same key has already been added.";

    public const string Argument_BufferIndexExceedsCapacity = "The specified buffer index exceeds the buffer capacity.";

    public const string Argument_BufferIndexExceedsLength = "The specified buffer index exceeds the buffer length.";

    public const string Argument_BufferLengthExceedsCapacity = "The specified useful data length exceeds the capacity of this buffer.";

    public const string Argument_IndexOutOfArrayBounds = "The specified index is out of bounds of the specified array.";

    public const string Argument_InsufficientArrayElementsAfterOffset = "The specified array does not contain the specified number of elements starting at the specified offset.";

    public const string Argument_InsufficientBufferCapacity = "The buffer capacity is insufficient for the specified length.";

    public const string Argument_InsufficientSpaceToCopyCollection = "Insufficient space in the target array to copy the collection.";

    public const string Argument_InsufficientSpaceInSourceBuffer = "Insufficient space in the source buffer.";

    public const string Argument_InsufficientSpaceInTargetBuffer = "Insufficient space in the target buffer.";

    public const string Argument_InvalidIBufferInstance = "The provided 'IBuffer' instance is not valid, and retrieving its underlying data failed.";

    public const string Argument_StreamPositionBeyondEndOfStream = "The specified position is beyond the end of the stream.";

    public const string ArgumentOutOfRange_BufferLengthExceedsArrayMaxLength = "The specified buffer length exceeds the maximum array length.";

    public const string ArgumentOutOfRange_Index = "Index was out of range.";

    public const string ArgumentOutOfRange_IndexLargerThanMaxValue = "The specified index is larger than the maximum allowed value.";

    public const string InvalidOperation_CannotGetResultsFromIncompleteOperation = "Cannot call 'GetResults' on this asynchronous info because the underlying operation has not completed.";

    public const string InvalidOperation_CannotRemoveLastFromEmptyCollection = "Cannot remove the last element from an empty collection.";

    public const string InvalidOperation_CannotSetCompletionHandlerMoreThanOnce = "The 'Completed' handler delegate cannot be set more than once, but this handler has already been set.";

    public const string InvalidOperation_CollectionBackingListTooLarge = "The backing list is too large to be used as a .NET collection.";

    public const string InvalidOperation_IllegalStateChange = "The specified state transition is illegal for the current state of this object.";

    public const string InvalidOperation_TaskProviderReturnedUnstartedTask = "The Task provider delegate specified for this 'IAsyncInfo' instance returned a 'Task' object that was not started. 'Task' instances must be run immediately upon creation.";

    public const string InvalidOperation_UnstartedTaskSpecified = "The specified underlying 'Task' is not started. 'Task' instances must be run immediately upon creation.";

    public const string NullReference_TaskProviderReturnedNull = "The task provider delegate used to create this asynchronous operation returned 'null', but a valid 'Task' object was expected.";

    public const string ObjectDisposed_AsyncInfoIsClosed = "The requested invocation is not permitted because this 'IAsyncInfo' instance has already been closed.";

    public const string UnauthorizedAccess_InternalBuffer = "The underlying buffer of the 'MemoryStream' cannot be accessed.";

    public const string WinRtCOM_Error = "An error has occurred.";
}