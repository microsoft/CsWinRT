#if NET5_0_OR_GREATER

using System.Runtime.CompilerServices;

namespace Microsoft.System
{
    /// <inheritdoc/>
    public partial class DispatcherQueueSynchronizationContext
    {
        /// <summary>
        /// A struct mapping the native WinRT <c>IDispatcherQueue</c> interface.
        /// </summary>
        internal unsafe struct IDispatcherQueue
        {
            /// <summary>
            /// The vtable pointer for the current instance.
            /// </summary>
            private readonly void** lpVtbl;

            /// <summary>
            /// Native API for <see cref="DispatcherQueue.TryEnqueue(DispatcherQueueHandler)"/>.
            /// </summary>
            /// <param name="callback">A pointer to an <c>IDispatcherQueueHandler</c> object.</param>
            /// <param name="result">The result of the operation (the <see cref="bool"/> WinRT retval).</param>
            /// <returns>The <c>HRESULT</c> for the operation.</returns>
            /// <remarks>The <paramref name="callback"/> parameter is assumed to be a pointer to an <c>IDispatcherQueueHandler</c> object.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int TryEnqueue(void* callback, byte* result)
            {
                return ((delegate* unmanaged<IDispatcherQueue*, void*, byte*, int>)lpVtbl[7])((IDispatcherQueue*)Unsafe.AsPointer(ref this), callback, result);
            }
        }
    }
}

#endif