#nullable enable

namespace Microsoft.UI.Dispatching
{
    /// <summary>
    /// The <see cref="DispatcherQueueSynchronizationContext"/> type allows developers to await calls and get back onto
    /// the UI thread. Needs to be installed on the UI thread through <see cref="SynchronizationContext.SetSynchronizationContext"/>.
    /// </summary>
    public sealed class DispatcherQueueSynchronizationContext : global::System.Threading.SynchronizationContext
    {
        /// <summary>
        /// The <see cref="WindowsRuntimeObjectReference"/> instance for the target dispatcher queue.
        /// </summary>
        private readonly WindowsRuntimeObjectReference _objectReference;

        /// <summary>
        /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
        /// </summary>
        /// <param name="dispatcherQueue">The target <see cref="global::Microsoft.UI.Dispatching.DispatcherQueue"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dispatcherQueue"/> is <see langword="null"/>.</exception>
        public DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
        {
            ArgumentNullException.ThrowIfNull(dispatcherQueue);

            if (!WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(dispatcherQueue, out _objectReference!))
            {
                throw new ArgumentException(null, nameof(dispatcherQueue));
            }
        }

        /// <summary>
        /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
        /// </summary>
        /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> instance for the target dispatcher queue.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="objectReference"/> is <see langword="null"/>.</exception>
        private DispatcherQueueSynchronizationContext(WindowsRuntimeObjectReference objectReference)
        {
            ArgumentNullException.ThrowIfNull(objectReference);

            _objectReference = objectReference;
        }

        /// <inheritdoc/>
        public override unsafe void Post(global::System.Threading.SendOrPostCallback d, object? state)
        {
            ArgumentNullException.ThrowIfNull(d);

            global::ABI.Microsoft.UI.Dispatching.DispatcherQueueProxyHandler* dispatcherQueueProxyHandler = global::ABI.Microsoft.UI.Dispatching.DispatcherQueueProxyHandler.Create(d, state);
            int hresult;

            try
            {
                _objectReference.AddRefUnsafe();

                void* thisPtr = _objectReference.GetThisPtrUnsafe();
                bool success;

                // Note: we're intentionally ignoring the retval for 'DispatcherQueue::TryEnqueue'.
                hresult = ((delegate* unmanaged<void*, void*, byte*, int>)(*(void***)thisPtr)[7])(thisPtr, dispatcherQueueProxyHandler, (byte*)&success);
            }
            finally
            {
                dispatcherQueueProxyHandler->Release();
                _objectReference.ReleaseUnsafe();
            }


            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }

        /// <inheritdoc/>
        public override void Send(global::System.Threading.SendOrPostCallback d, object? state)
        {
            throw new NotSupportedException("'SynchronizationContext.Send' is not supported.");
        }

        /// <inheritdoc/>
        public override global::System.Threading.SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSynchronizationContext(_objectReference);
        }
    }
}

#nullable disable