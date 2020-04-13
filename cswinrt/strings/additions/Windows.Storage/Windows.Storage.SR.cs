// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Windows.Storage
{
    class SR
    {
        public static string Argument_BufferIndexExceedsCapacity = "The specified buffer index is not within the buffer capacity.";

        public static string Argument_BufferLengthExceedsCapacity = "The specified useful data length exceeds the capacity of this buffer.";

        public static string Argument_IndexOutOfArrayBounds = "The specified index is out of bounds of the specified array.";

        public static string Argument_InstancesImplementingIRASThatCanReadMustImplementIIS = "The specified Windows Runtime stream supports the IRandomAccessStream interface and its CanRead property returns TRUE, however it does not implement the IInputStream interface. Windows Runtime streams with such inconsistent capabilities cannot be converted to managed Stream objects. IRandomAccessStream instances whose CanRead property returns TRUE must implement the IInputStream interface.";

        public static string Argument_InstancesImplementingIRASThatCanWriteMustImplementIOS = "The specified Windows Runtime stream supports the IRandomAccessStream interface and its CanWrite property returns TRUE, however it does not implement the IOutputStream interface. Windows Runtime streams with such inconsistent capabilities cannot be converted to managed Stream objects. IRandomAccessStream instances whose CanWrite property returns TRUE must implement the IOutputStream interface.";

        public static string Argument_InsufficientArrayElementsAfterOffset = "The specified array does not contain the specified number of elements starting at the specified offset.";

        public static string Argument_InsufficientBufferCapacity = "The specified buffer capacity is not sufficient to hold data of the specified length.";

        public static string Argument_InsufficientSpaceInSourceBuffer = "The specified source buffer does not contain the specified number of elements starting at the specified offset.";

        public static string Argument_InsufficientSpaceInTargetBuffer = "The specified destination buffer is not large enough to hold the specified number of bytes starting at the specified offset.";

        public static string Argument_NotSufficientCapabilitiesToConvertToWinRtStream = "Cannot convert the specified Stream object to a Windows Runtime stream because it does not have sufficient capabilities. In order to convert a System.IO.Stream instance to a Windows Runtime stream at least one of the properties CanRead, CanWrite, CanSeek must return TRUE; however, none of these properties returns TRUE for the specified Stream.";

        public static string Argument_ObjectMustBeWinRtStreamToConvertToNetFxStream = "The specified object cannot be converted to a System.IO.Stream instance because it is not a Windows Runtime stream. In order to convert an object to a Stream instance it must implement at least one of the following 3 Windows Runtime stream interfaces: IInputStream, IOutputStream, IRandomAccessStream.";

        public static string Argument_RelativePathMayNotBeWhitespaceOnly = "The specified relative path may not consist of whitespace only";

        public static string Argument_StreamPositionBeyondEOS = "The specified stream position is beyond the end of the stream.";

        public static string Argument_UnexpectedAsyncResult = "The specified AsyncResult does not correspond to any outstanding IO operation.";

        public static string Argument_WinRtStreamCannotReadOrWrite = "The specified Windows Runtime stream does not support reading nor writing. Windows Runtime streams with such capabilities cannot be converted to managed Stream objects. Use a Windows Runtime stream that can support reading, writing or both.";

        public static string ArgumentOutOfRange_CannotResizeStreamToNegative = "Cannot set the length of a stream to a negative value.";

        public static string ArgumentOutOfRange_IO_CannotSeekToNegativePosition = "Cannot seek to an absolute stream position that is negative.";

        public static string ArgumentOutOfRange_InvalidInputStreamOptionsEnumValue = "The specified value is not a valid member of the InputStreamOptions enumeration.";

        public static string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

        public static string ArgumentOutOfRange_WinRtAdapterBufferSizeMayNotBeNegative = "The buffer size for a Windows Runtime stream adapter may not be negative. Use a positive buffer size or 0 to disable buffering.";

        public static string InvalidOperation_CannotCallThisMethodInCurrentState = "The state of this object does not permit invoking this method.";

        public static string InvalidOperation_CannotChangeBufferSizeOfWinRtStreamAdapter = "Cannot convert the specified Windows Runtime stream to a managed System.IO.Stream object with the specified buffer size because this Windows Runtime stream has been previously converted to a managed Stream object with a different buffer size. Ensure that the 'bufferSize' argument matches the existing buffer or use the '{0}'-overload without the 'bufferSize' argument to convert the specified Windows Runtime stream to a Stream object with the same buffer size as previously.";

        public static string InvalidOperation_CannotChangeBufferSizeOfWinRtStreamAdapterToZero = "Cannot convert the specified Windows Runtime stream to a managed System.IO.Stream object without a buffer because this Windows Runtime stream has been previously converted to a managed Stream object with a buffer. Ensure that the 'bufferSize' argument matches the existing buffer or use the '{0}'-overload without the 'bufferSize' argument to convert the specified Windows Runtime stream to a Stream object with the same buffer size as previously.";

        public static string InvalidOperation_CannotGetResultsFromIncompleteOperation = "Cannot call GetResults on this asynchronous info because the underlying operation has not completed.";

        public static string InvalidOperation_CannotSetCompletionHanlderMoreThanOnce = "The 'Completed' handler delegate cannot be set more than once, but this handler has already been set.";

        public static string InvalidOperation_CannotSetStreamSizeCannotWrite = "Cannot set the size of this stream because it cannot be written to.";

        public static string InvalidOperation_IllegalStateChange = "The specified state transition is illegal for the current state of this object.";

        public static string InvalidOperation_InvalidAsyncCompletion = "The asynchronous operation could not be completed.";

        public static string InvalidOperation_MultipleIOCompletionCallbackInvocation = "A callback for the same asynchronous IO operation was invoked more than once.";

        public static string InvalidOperation_TaskProviderReturnedUnstartedTask = "The Task provider delegate specified for this IAsyncInfo instance returned a Task object that was not started. Task instances must be run immediately upon creation.";

        public static string InvalidOperation_UnexpectedAsyncOperationID = "This AsyncResult or Task corresponds to a different asynchronous operation ID than the one that invoked the completion callback.";

        public static string InvalidOperation_UnstartedTaskSpecified = "The specified underlying Task is not started. Task instances must be run immediately upon creation.";

        public static string IO_CannotSeekBeyondInt64MaxValue = "Cannot seek to an absolute stream position that is larger than 2^63 - 1 bytes. (2^63 - 1 = 0x7FFFFFFFFFFFFFFF = Int64.MaxValue).";

        public static string IO_CannotSetSizeBeyondInt64MaxValue = "This Windows Runtime stream is backed by a .NET Stream; its size cannot be set to a value that is larger than 2^63 - 1 bytes. (2^63 - 1 = 0x7FFFFFFFFFFFFFFF = Int64.MaxValue).";

        public static string IO_General = "An IO error occurred in the Windows runtime system.";

        public static string IO_UnderlyingWinRTStreamTooLong_CannotUseLengthOrPosition = "This Stream is backed by a Windows Runtime stream with a length that exceeds 2^63 - 1 bytes. Operations related to the stream's length or position cannot be performed on streams when the length exceeds 2^63 - 1 bytes. (2^63 - 1 = 0x7FFFFFFFFFFFFFFF = Int64.MaxValue = approx. 8000 PetaBytes.)";

        public static string NotImplemented_NativeRoutineNotFound = "A native library routine was not found: {0}.";

        public static string NotSupported_CannotConvertNotReadableToInputStream = "Cannot use the specified Stream as a Windows Runtime IInputStream because this Stream is not readable.";

        public static string NotSupported_CannotConvertNotSeekableToRandomAccessStream = "Cannot use the specified Stream as a Windows Runtime IRandomAccessStream because this Stream does not support seeking.";

        public static string NotSupported_CannotConvertNotWritableToOutputStream = "Cannot use the specified Stream as a Windows Runtime IOutputStream because this Stream is not writable.";

        public static string NotSupported_CannotReadFromStream = "This stream does not support read access.";

        public static string NotSupported_CannotSeekInStream = "This stream does not support seeking.";

        public static string NotSupported_CannotUseLength_StreamNotSeekable = "This stream does not support the Length property because it is not seekable.";

        public static string NotSupported_CannotUsePosition_StreamNotSeekable = "This stream does not support the Position property because it is not seekable.";

        public static string NotSupported_CannotWriteToStream = "This stream does not support write access.";

        public static string NotSupported_CloningNotSupported = "This IRandomAccessStream does not support the {0} method because it requires cloning and this stream does not support cloning.";

        public static string NullReference_IOCompletionCallbackCannotProcessNullAsyncInfo = "The Windows Runtime stream that underlies this System.IO.Stream object has invoked an IO completion callback and specified null for the IAsyncInfo instance that describes the completed IO operation. This behavior is not supported because results cannot be retrieved from a null operation. Either the underlying Windows Runtime stream has a faulty implementation, or you are using a Windows Runtime object in an unsupported runtime environment.";

        public static string NullReference_TaskProviderReturnedNull = "The task provider delegate used to create this asynchronous operation returned null, but a valid Task object was expected.";

        public static string ObjectDisposed_AsyncInfoIsClosed = "The requested invocation is not permitted because this IAsyncInfo instance has already been closed.";

        public static string ObjectDisposed_CannotPerformOperation = "The requested operation cannot be performed because this stream has already been disposed.";

        public static string WinRtCOM_Error = "An error has occurred.";

        public static string ObjectDisposed_StreamClosed = "Cannot access a closed stream.";

        public static string NotSupported_UnseekableStream = "Stream does not support seeking.";

        public static string NotSupported_UnreadableStream = "Stream does not support reading.";

        public static string NotSupported_UnwritableStream = "Stream does not support writing.";

        public static string Argument_InvalidSeekOrigin = "Invalid seek origin.";

        public static string DirectUI_Empty = "Empty.";

        public static string InvalidOperation_SendNotSupportedOnWindowsRTSynchronizationContext = "Send is not supported in the Windows Runtime SynchronizationContext";

        public static string UnauthorizedAccess_InternalBuffer = "MemoryStream's internal buffer cannot be accessed.";

        public static string NotSupported_Inheritable = "Inheritable is not a supported option.";

        public static string NotSupported_Encrypted = "Encrypted is not a supported option.";

        public static string IO_FileNotFound = "Unable to find the specified file.";

        public static string IO_FileNotFound_FileName = "Could not find file '{0}'.";

        public static string IO_PathNotFound_NoPathName = "Could not find a part of the path.";

        public static string IO_PathNotFound_Path = "Could not find a part of the path '{0}'.";

        public static string UnauthorizedAccess_IODenied_NoPathName = "Access to the path is denied.";

        public static string UnauthorizedAccess_IODenied_Path = "Access to the path '{0}' is denied.";

        public static string IO_AlreadyExists_Name = "Cannot create '{0}' because a file or directory with the same name already exists.";

        public static string IO_PathTooLong = "The specified file name or path is too long, or a component of the specified path is too long.";

        public static string IO_SharingViolation_File = "The process cannot access the file '{0}' because it is being used by another process.";

        public static string IO_SharingViolation_NoFileName = "The process cannot access the file because it is being used by another process.";

        public static string IO_FileExists_Name = "The file '{0}' already exists.";

        public static string IO_PathTooLong_Path = "The path '{0}' is too long, or a component of the specified path is too long.";

        public static string InvalidAction = "Invalid action value: '{0}'.";

    }
}
