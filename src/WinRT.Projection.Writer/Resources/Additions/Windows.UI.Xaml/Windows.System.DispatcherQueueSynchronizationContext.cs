#nullable enable

namespace Windows.System
{
    /// <summary>
    /// The <see cref="DispatcherQueueSynchronizationContext"/> type allows developers to await calls and get back onto
    /// the UI thread. Needs to be installed on the UI thread through <see cref="SynchronizationContext.SetSynchronizationContext"/>.
    /// </summary>
    public sealed class DispatcherQueueSynchronizationContext : global::System.Threading.SynchronizationContext
    {
#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>
        /// The <see cref="WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext"/> instance to use.
        /// </summary>
        private readonly WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext _innerContext;
#endif

        /// <summary>
        /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
        /// </summary>
        /// <param name="dispatcherQueue">The target <see cref="global::Windows.System.DispatcherQueue"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dispatcherQueue"/> is <see langword="null"/>.</exception>
        public DispatcherQueueSynchronizationContext(global::Windows.System.DispatcherQueue dispatcherQueue)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            _innerContext = new WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext(dispatcherQueue);
#endif
        }

#if !CSWINRT_REFERENCE_PROJECTION
        /// <summary>
        /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
        /// </summary>
        /// <param name="innerContext">The <see cref="WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext"/> instance for the target dispatcher queue.</param>
        private DispatcherQueueSynchronizationContext(WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext innerContext)
        {
            _innerContext = innerContext;
        }
#endif

        /// <inheritdoc/>
        public override void Post(global::System.Threading.SendOrPostCallback d, object? state)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            _innerContext.Post(d, state);
#endif
        }

        /// <inheritdoc/>
        public override void Send(global::System.Threading.SendOrPostCallback d, object? state)
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            _innerContext.Send(d, state);
#endif
        }

        /// <inheritdoc/>
        public override global::System.Threading.SynchronizationContext CreateCopy()
        {
#if CSWINRT_REFERENCE_PROJECTION
            throw null;
#else
            return new DispatcherQueueSynchronizationContext(_innerContext);
#endif
        }
    }
}

#nullable restore