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
        /// The <see cref="WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext"/> instance to use.
        /// </summary>
        private readonly WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext _innerContext;

        /// <summary>
        /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
        /// </summary>
        /// <param name="dispatcherQueue">The target <see cref="global::Microsoft.UI.Dispatching.DispatcherQueue"/> instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dispatcherQueue"/> is <see langword="null"/>.</exception>
        public DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
        {
            _innerContext = new WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext(dispatcherQueue);
        }

        /// <summary>
        /// Creates a new <see cref="DispatcherQueueSynchronizationContext"/> instance with the specified parameters.
        /// </summary>
        /// <param name="innerContext">The <see cref="WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext"/> instance for the target dispatcher queue.</param>
        private DispatcherQueueSynchronizationContext(WindowsRuntime.InteropServices.DispatcherQueueSynchronizationContext innerContext)
        {
            _innerContext = innerContext;
        }

        /// <inheritdoc/>
        public override void Post(global::System.Threading.SendOrPostCallback d, object? state)
        {
            _innerContext.Post(d, state);
        }

        /// <inheritdoc/>
        public override void Send(global::System.Threading.SendOrPostCallback d, object? state)
        {
            _innerContext.Send(d, state);
        }

        /// <inheritdoc/>
        public override global::System.Threading.SynchronizationContext CreateCopy()
        {
            return new DispatcherQueueSynchronizationContext(_innerContext);
        }
    }
}

#nullable restore