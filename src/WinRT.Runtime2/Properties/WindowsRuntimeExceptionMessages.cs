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

    public const string Argument_BufferOffsetExceedsLength = "The specified buffer offset exceeds the buffer length.";

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

    public const string Argument_InstancesImplementingIRASThatCanReadMustImplementIIS = "The specified Windows Runtime stream supports the IRandomAccessStream interface and its CanRead property returns TRUE, however it does not implement the IInputStream interface. Windows Runtime streams with such inconsistent capabilities cannot be converted to managed Stream objects. IRandomAccessStream instances whose CanRead property returns TRUE must implement the IInputStream interface.";

    public const string Argument_InstancesImplementingIRASThatCanWriteMustImplementIOS = "The specified Windows Runtime stream supports the IRandomAccessStream interface and its CanWrite property returns TRUE, however it does not implement the IOutputStream interface. Windows Runtime streams with such inconsistent capabilities cannot be converted to managed Stream objects. IRandomAccessStream instances whose CanWrite property returns TRUE must implement the IOutputStream interface.";

    public const string Argument_InvalidSeekOrigin = "Invalid seek origin.";

    public const string Argument_NotSufficientCapabilitiesToConvertToWinRtStream = "Cannot convert the specified Stream object to a Windows Runtime stream because it does not have sufficient capabilities. In order to convert a System.IO.Stream instance to a Windows Runtime stream at least one of the properties CanRead, CanWrite, CanSeek must return TRUE; however, none of these properties returns TRUE for the specified Stream.";

    public const string Argument_ObjectMustBeWinRtStreamToConvertToNetFxStream = "The specified object cannot be converted to a System.IO.Stream instance because it is not a Windows Runtime stream. In order to convert an object to a Stream instance it must implement at least one of the following 3 Windows Runtime stream interfaces: IInputStream, IOutputStream, IRandomAccessStream.";

    public const string Argument_UnexpectedAsyncResult = "The specified AsyncResult does not correspond to any outstanding IO operation.";

    public const string Argument_WinRtStreamCannotReadOrWrite = "The specified Windows Runtime stream does not support reading nor writing. Windows Runtime streams with such capabilities cannot be converted to managed Stream objects. Use a Windows Runtime stream that can support reading, writing or both.";

    public const string ArgumentNullReference_IOCompletionCallbackCannotProcessNullAsyncInfo = "The Windows Runtime stream that underlies this 'System.IO.Stream' object has invoked an I/O completion callback and specified 'null' for the 'IAsyncInfo' instance that describes the completed IO operation. This behavior is not supported, because results cannot be retrieved from a 'null' operation. Either the underlying Windows Runtime stream has a faulty implementation, or the Windows Runtime object in being used in an unsupported runtime environment.";

    public const string ArgumentOutOfRange_CannotResizeStreamToNegative = "Cannot set the length of a stream to a negative value.";

    public const string ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue = "The specified value is not a valid member of the InputStreamOptions enumeration.";

    public const string ArgumentOutOfRange_IO_CannotSeekToNegativePosition = "Cannot seek to an absolute stream position that is negative.";

    public const string InvalidOperation_CannotCallThisMethodInCurrentState = "The state of this object does not permit invoking this method.";

    public const string InvalidOperation_CannotSetStreamSizeCannotWrite = "Cannot set the size of this stream because it cannot be written to.";

    public const string InvalidOperation_MultipleIOCompletionCallbackInvocation = "A callback for the same asynchronous IO operation was invoked more than once.";

    public const string InvalidOperation_UnexpectedAsyncOperationID = "This AsyncResult or Task corresponds to a different asynchronous operation ID than the one that invoked the completion callback.";

    public const string IO_CannotSeekBeyondInt64MaxValue = "Cannot seek to an absolute stream position that is larger than 2^63 - 1 bytes. (2^63 - 1 = 0x7FFFFFFFFFFFFFFF = Int64.MaxValue).";

    public const string IO_CannotSetSizeBeyondInt64MaxValue = "This Windows Runtime stream is backed by a .NET Stream; its size cannot be set to a value that is larger than 2^63 - 1 bytes. (2^63 - 1 = 0x7FFFFFFFFFFFFFFF = Int64.MaxValue).";

    public const string IO_General = "An IO error occurred in the Windows runtime system.";

    public const string IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition = "This Stream is backed by a Windows Runtime stream with a length that exceeds 2^63 - 1 bytes. Operations related to the stream's length or position cannot be performed on streams when the length exceeds 2^63 - 1 bytes. (2^63 - 1 = 0x7FFFFFFFFFFFFFFF = Int64.MaxValue = approx. 8000 PetaBytes.)";

    public const string NotSupported_CannotReadFromStream = "This stream does not support read access.";

    public const string NotSupported_CannotSeekInStream = "This stream does not support seeking.";

    public const string NotSupported_CannotUseLength_StreamNotSeekable = "This stream does not support the Length property because it is not seekable.";

    public const string NotSupported_CannotUsePosition_StreamNotSeekable = "This stream does not support the Position property because it is not seekable.";

    public const string NotSupported_CannotWriteToStream = "This stream does not support write access.";

    public const string NotSupported_CloningNotSupported = "This IRandomAccessStream does not support the {0} method because it requires cloning and this stream does not support cloning.";

    public const string NotSupported_UnrecognizedStreamReadOptimization = "This stream is using a read optimization mode that was not recognized.";

    public const string ObjectDisposed_CannotPerformOperationOnDisposedStream = "The requested operation cannot be performed because this stream has already been disposed.";
}