// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

#nullable enable

namespace ABI.WinRT.Interop
{
    using EventRegistrationToken = global::WinRT.EventRegistrationToken;

    /// <summary>
    /// A type representing all associated state for a given <see cref="EventSource{TDelegate}"/> instance.
    /// </summary>
    /// <typeparam name="TDelegate">The type of delegate being managed from the associated event.</typeparam>
    /// <remarks>This type is only meant to be used by generated projections.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif    
    abstract class EventSourceState<TDelegate> : IDisposable
        where TDelegate : class, MulticastDelegate
    {
        // Registration state and delegate cached separately to survive EventSource garbage collection
        // and to prevent the generated event delegate from impacting the lifetime of the event source.

        private bool disposedValue;
        private readonly IntPtr obj;
        private readonly int index;
        private readonly global::System.WeakReference<object> cacheEntry;
        private IntPtr eventInvokePtr;
        private IntPtr referenceTrackerTargetPtr;
        internal EventRegistrationToken token;
        internal TDelegate? targetDelegate;
        internal TDelegate eventInvoke;

        /// <summary>
        /// Creates a new <see cref="EventSourceState{TDelegate}"/> instance with the specified parameters.
        /// </summary>
        /// <param name="thisPtr">The pointer to the target object owning the associated event.</param>
        /// <param name="index">The index of the event the state is associated to.</param>
        protected EventSourceState(IntPtr thisPtr, int index)
        {
            this.obj = thisPtr;
            this.index = index;
            eventInvoke = GetEventInvoke();
            cacheEntry = new global::System.WeakReference<object>(this);
        }

        /// <summary>
        /// Finalizes the current <see cref="EventSourceState{TDelegate}"/> instance.
        /// </summary>
        ~EventSourceState()
        {
            // The lifetime of this object is managed by the delegate / eventInvoke
            // through its target reference to it.  Once the delegate no longer has
            // any references, this object will also no longer have any references.
            Dispose(false);
        }

        /// <summary>
        /// Gets the current <typeparamref name="TDelegate"/> value with the active subscriptions for the target event.
        /// </summary>
        protected TDelegate? TargetDelegate => targetDelegate;

        /// <summary>
        /// Gets a <typeparamref name="TDelegate"/> instance responsible for actually raising the
        /// event, if any targets are currently available, or for doing nothing if the current
        /// target handler is currently <see langword="null"/>.
        /// </summary>
        /// <returns>The resulting <typeparamref name="TDelegate"/> instance to raise the event.</returns>
        /// <remarks>
        /// The returned delegate must capture <see langword="this"/> to ensure the associated state is kept alive.
        /// </remarks>
        protected abstract TDelegate GetEventInvoke();

        /// <summary>
        /// Disposes the current instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the method was called by <see cref="Dispose()"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Uses the dispose pattern to ensure we only remove
            // from the cache once: either via unsubscribe or via
            // the finalizer.
            if (!disposedValue)
            {
                EventSourceCache.Remove(obj, index, cacheEntry);
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Dispose(true);
        }

        // Allows to retrieve a singleton like weak reference to use
        // with the cache to allow for proper removal with comparision.
        internal global::System.WeakReference<object> GetWeakReferenceForCache()
        {
            return cacheEntry;
        }

        internal void InitializeReferenceTracking(IntPtr ptr)
        {
            eventInvokePtr = ptr;
            int hr = Marshal.QueryInterface(ptr, ref Unsafe.AsRef(in IID.IID_IReferenceTrackerTarget), out referenceTrackerTargetPtr);
            if (hr != 0)
            {
                referenceTrackerTargetPtr = default;
            }
            else
            {
                // We don't want to keep ourselves alive and as long as this object
                // is alive, the CCW still exists.
                Marshal.Release(referenceTrackerTargetPtr);
            }
        }

        internal unsafe bool HasComReferences()
        {
            if (eventInvokePtr != default)
            {
                IUnknownVftbl vftblIUnknown = **(IUnknownVftbl**)eventInvokePtr;
                vftblIUnknown.AddRef(eventInvokePtr);
                uint comRefCount = vftblIUnknown.Release(eventInvokePtr);
                if (comRefCount != 0)
                {
                    return true;
                }
            }

            if (referenceTrackerTargetPtr != default)
            {
                void** vftblReferenceTracker = *(void***)referenceTrackerTargetPtr;

                // AddRefFromReferenceTracker
                _ = ((delegate* unmanaged[Stdcall]<IntPtr, uint>)(vftblReferenceTracker[3]))(referenceTrackerTargetPtr);

                // ReleaseFromReferenceTracker
                uint refTrackerCount = ((delegate* unmanaged[Stdcall]<IntPtr, uint>)(vftblReferenceTracker[4]))(referenceTrackerTargetPtr);

                if (refTrackerCount != 0)
                {
                    // Note we can't tell if the reference tracker ref is pegged or not, so this is best effort where if there
                    // are any reference tracker references, we assume the event has references.
                    return true;
                }
            }

            return false;
        }
    }
}

// Restore in case this file is merged with others.
#nullable restore