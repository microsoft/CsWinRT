// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Threading.Tasks
{
    using System;
    using System.Runtime.ExceptionServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    internal static class ExceptionDispatchHelper
    {
        private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
        private const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
        private const int JSCRIPT_E_CANTEXECUTE = unchecked((int)0x89020001);

        internal static void ThrowAsync(Exception exception, SynchronizationContext targetContext)
        {
            if (exception == null)
                return;

            // If this is an error indicating the RPC called failed,
            // it is most likely due to the other process is gone.
            // We ignore it as it just means we weren't able to report
            // the completion and not actually an error in the other process.
            if (exception is COMException comException &&
                (comException.HResult == RPC_E_DISCONNECTED ||
                 comException.HResult == RPC_S_SERVER_UNAVAILABLE ||
                 comException.HResult == JSCRIPT_E_CANTEXECUTE))
            {
                return;
            }

            ExceptionDispatchInfo exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);

            if (targetContext != null)
            {
                try
                {
                    targetContext.Post((edi) => ((ExceptionDispatchInfo)edi!).Throw(), exceptionDispatchInfo);
                }
                catch
                {
                    // Something went wrong in the Post; let's try using the thread pool instead:
                    ThrowAsync(exception, null);
                }
                return;
            }

            bool scheduled = true;
            try
            {
                new SynchronizationContext().Post((edi) => ((ExceptionDispatchInfo)edi!).Throw(), exceptionDispatchInfo);
            }
            catch
            {
                // Something went wrong when scheduling the thrower; we do our best by throwing the exception here:
                scheduled = false;
            }

            if (!scheduled)
                exceptionDispatchInfo.Throw();
        }
    }  // ExceptionDispatchHelper
}  // namespace

// ExceptionDispatchHelper.cs
