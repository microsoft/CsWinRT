// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Windows.Foundation;

static class SR
{
    public static string Argument_BufferLengthExceedsCapacity = "The specified useful data length exceeds the capacity of this buffer.";

    public static string Argument_IndexOutOfArrayBounds = "The specified index is out of bounds of the specified array.";

    public static string Argument_InsufficientArrayElementsAfterOffset = "The specified array does not contain the specified number of elements starting at the specified offset.";

    public static string Argument_UnexpectedAsyncResult = "The specified AsyncResult does not correspond to any outstanding IO operation.";

    public static string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

    public static string InvalidOperation_CannotCallThisMethodInCurrentState = "The state of this object does not permit invoking this method.";

    public static string InvalidOperation_CannotGetResultsFromIncompleteOperation = "Cannot call GetResults on this asynchronous info because the underlying operation has not completed.";

    public static string InvalidOperation_CannotSetCompletionHanlderMoreThanOnce = "The 'Completed' handler delegate cannot be set more than once, but this handler has already been set.";

    public static string InvalidOperation_CannotSetStreamSizeCannotWrite = "Cannot set the size of this stream because it cannot be written to.";

    public static string InvalidOperation_IllegalStateChange = "The specified state transition is illegal for the current state of this object.";

    public static string InvalidOperation_InvalidAsyncCompletion = "The asynchronous operation could not be completed.";

    public static string InvalidOperation_MultipleIOCompletionCallbackInvocation = "A callback for the same asynchronous IO operation was invoked more than once.";

    public static string InvalidOperation_TaskProviderReturnedUnstartedTask = "The Task provider delegate specified for this IAsyncInfo instance returned a Task object that was not started. Task instances must be run immediately upon creation.";

    public static string InvalidOperation_UnexpectedAsyncOperationID = "This AsyncResult or Task corresponds to a different asynchronous operation ID than the one that invoked the completion callback.";

    public static string InvalidOperation_UnstartedTaskSpecified = "The specified underlying Task is not started. Task instances must be run immediately upon creation.";

    public static string IO_General = "An IO error occurred in the Windows runtime system.";

    public static string NotImplemented_NativeRoutineNotFound = "A native library routine was not found: {0}.";

    public static string NullReference_IOCompletionCallbackCannotProcessNullAsyncInfo = "The Windows Runtime stream that underlies this System.IO.Stream object has invoked an IO completion callback and specified null for the IAsyncInfo instance that describes the completed IO operation. This behavior is not supported because results cannot be retrieved from a null operation. Either the underlying Windows Runtime stream has a faulty implementation, or you are using a Windows Runtime object in an unsupported runtime environment.";

    public static string NullReference_TaskProviderReturnedNull = "The task provider delegate used to create this asynchronous operation returned null, but a valid Task object was expected.";

    public static string ObjectDisposed_AsyncInfoIsClosed = "The requested invocation is not permitted because this IAsyncInfo instance has already been closed.";

    public static string ObjectDisposed_CannotPerformOperation = "The requested operation cannot be performed because this stream has already been disposed.";

    public static string WinRtCOM_Error = "An error has occurred.";

    public static string Argument_InvalidSeekOrigin = "Invalid seek origin.";

    public static string InvalidOperation_SendNotSupportedOnWindowsrTSynchronizationContext = "Send is not supported in the Windows Runtime SynchronizationContext";

    public static string InvalidAction = "Invalid action value: '{0}'.";

}
