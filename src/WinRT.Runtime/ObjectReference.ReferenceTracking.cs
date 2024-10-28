// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using WinRT.Interop;

namespace WinRT
{
    unsafe partial class IObjectReference
    {
        /// <summary>
        /// Finalizes the current object instance. If the underlying native
        /// resources are still active, it also releases them as needed.
        /// </summary>
        ~IObjectReference()
        {
            DisposeUnsafe();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            DisposeUnsafe();
        }

        /// <summary>
        /// Gets the underlying pointer owned by the current instance, after incrementing its reference count.
        /// </summary>
        /// <returns>The underlying pointer owned by the current instance.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves the same pointer as <see cref="ThisPtr"/>. That is,
        /// it retrieves the pointer to the underlying object, for the current context.
        /// </para>
        /// <para>
        /// This method will increment the reference count of the returned pointer.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the current instance has been disposed.</exception>
        public IntPtr GetThisPtr()
        {
            AddRefUnsafe();
            NativeAddRefUnsafe(addRefFromTrackerSource: false);

            IntPtr thisPtr = GetThisPtrUnsafe();

            ReleaseUnsafe();

            return thisPtr;
        }

        /// <summary>
        /// Gets the underlying pointer owned by the current instance.
        /// </summary>
        /// <returns>The underlying pointer owned by the current instance.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves the same pointer as <see cref="ThisPtr"/>. That is,
        /// it retrieves the pointer to the underlying object, for the current context.
        /// </para>
        /// <para>
        /// This method does not check for disposal, nor does it increment the managed reference count of
        /// the current object. Callers must call <see cref="AddRefUnsafe"/> and <see cref="ReleaseUnsafe"/>.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetThisPtrUnsafe()
        {
            return GetThisPtrForCurrentContextUnsafe();
        }

        /// <summary>
        /// Gets the pointer to the reference tracker object tied to the current instance.
        /// </summary>
        /// <returns>The pointer to the reference tracker object tied to the current instance.</returns>
        /// <remarks>
        /// <para>
        /// This method retrieves the same pointer as <see cref="ReferenceTrackerPtr"/>.
        /// </para>
        /// <para>
        /// This method does not check for disposal, nor does it increment the managed reference count of
        /// the current object. Callers must call <see cref="AddRefUnsafe"/> and <see cref="ReleaseUnsafe"/>.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetReferenceTrackerPtrUnsafe()
        {
            return _referenceTrackerPtr;
        }

        /// <summary>
        /// Increments the managed reference count for the current <see cref="IObjectReference"/> instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the current instance has been disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddRefUnsafe()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(!TryAddRefUnsafe(), this);
#else
            if (!TryAddRefUnsafe())
            {
                throw new ObjectDisposedException(nameof(IObjectReference), "Object reference has been closed.");
            }
#endif
        }

        /// <summary>
        /// Tries to increment the managed reference count for the current <see cref="IObjectReference"/> instance.
        /// </summary>
        /// <returns>Whether the managed reference count has been increased successfully.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddRefUnsafe()
        {
            bool success = true;

            int currentValue;
            int originalValue;

            // To safely increment the reference count, the procedure is as follows:
            //   - If the object has been disposed (ie. if Disposed() has been called),
            //     even if the object hasn't actually released the unmanaged resources
            //     yet, then incrementing the ref count will fail and have no effect.
            //   - If the object hasn't been disposed, the reference count is incremented.
            // This can be done without taking a look, as follows:
            //   - Do an interlocked read to get the current reference tracking mask.
            //   - If the object has been disposed, the 32nd bit will be set. Due to the
            //     mask being a signed integer in two-complement, we can just compare and
            //     check whether the mask is lower than 0. If that is the case, just bail.
            //   - Do an interlocked compare exchange incrementing the reference count by 1.
            //     If the original value is the same as the current one, it means no other
            //     thread performed a concurrent update between our read and write, so we can
            //     stop. Otherwise, just loop until a compare exchange completes successfully.
            // The assumption is contention will be extremely rare, given that adding and releasing
            // a reference is incredibly fast compared to the time other operations need.
            do
            {
                currentValue = _referenceTrackingMask;

                if (currentValue < 0)
                {
                    success = false;

                    break;
                }

                originalValue = Interlocked.CompareExchange(
                    location1: ref _referenceTrackingMask,
                    value: currentValue + 1,
                    comparand: currentValue);
            }
            while (currentValue != originalValue);

            return success;
        }

        /// <inheritdoc/>
        private void DisposeUnsafe()
        {
            bool isDisposed = false;

            int currentValue;
            int originalValue;

            // To request a dispose operation, the procedure is as follows:
            //   - If the dispose bit has already been set, just do nothing. This means
            //     that another thread was the first to call Dispose(). In that case, the
            //     actual releasing of unmanaged resources will be performed either by that
            //     thread if there are no active callers, or by the last returned caller.
            //   - Do an interlocked compare exchange setting the dispose bit (32nd bit).
            //     Like above, if the original value doesn't match the current one, it means
            //     that another thread raced against this one, so the value is invalid, and
            //     another loop is executed. If the value matches, the loop just ends.
            // After this atomic update, we can then check whether (1) this was the first
            // thread to call Dispose() (ie. the dispose flag wasn't previously set and it
            // was set successfully by this call), and (2) there are no other active callers.
            // If both checks are true, the object is effectively dead and we can safely release
            // unmanaged resources. All other callers will just fail to be taken after this anyway.
            do
            {
                currentValue = _referenceTrackingMask;

                if (currentValue < 0)
                {
                    isDisposed = true;

                    break;
                }

                originalValue = Interlocked.CompareExchange(
                    location1: ref _referenceTrackingMask,
                    value: currentValue | (1 << 31),
                    comparand: currentValue);
            }
            while (currentValue != originalValue);

            // Only release resources if this is the first time Dispose() has been called, and
            // there are no outstanding leases. If there is one, don't do anything now. The
            // tracked object will just be released once the last active lease is returned.
            if (!isDisposed && currentValue == 0)
            {
                NativeReleaseUnsafe();
            }
        }

        /// <summary>
        /// Decrements the managed reference count for the current <see cref="IObjectReference"/> instance.
        /// If <see cref="Dispose()"/> has been called concurrently and this is the last caller, releases all
        /// native resources owned by the current <see cref="IObjectReference"/> instance as well.
        /// </summary>
        /// <remarks>
        /// Calls to <see cref="ReleaseUnsafe"/> should always exactly match calls to <see cref="AddRefUnsafe"/>.
        /// </remarks>
        public void ReleaseUnsafe()
        {
            // To release, we can simply do an interlocked decrement on the reference tracking
            // mask. Each caller is guaranteed to only call this method once (the contract states to only
            // ever use it per 'AddRefUnsafe' call), and a valid reference existing implies that the reference
            // counting mask had previously been incremented by 1. There is also no need to check for
            // disposal, because decrementing the count on a disposed object is perfectly valid (given that
            // the actual disposal is deferred until all active callers have returned).
            int currentValue = Interlocked.Decrement(ref _referenceTrackingMask);

            // If Dispose() has been called and this was the last reference, release the tracked object.
            // This is the case if the dispose bit is set (the 32nd one), and no other bit is set.
            if (currentValue == 1 << 31)
            {
                NativeReleaseUnsafe();
            }
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException"/> if <see cref="Dispose"/> has already been called on the current instance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that calling this method does not protect callers against concurrent threads calling <see cref="Dispose"/> on the
        /// same instance, as that behavior is explicitly undefined. Similarly, callers using this to then access the underlying
        /// pointers should also make sure to keep the current instance alive until they're done using the pointer (unless they're
        /// also incrementing it via <c>AddRef</c> in some way), or the GC could concurrently collect the instance and cause the
        /// same problem (ie. the underlying pointer being in use becoming invalid right after retrieving it from the object).
        /// </para>
        /// <para>
        /// This method exists mostly for backwards compatibility for older APIs. New code should always use <see cref="AddRefUnsafe"/>
        /// and <see cref="ReleaseUnsafe"/>, and then <see cref="GetThisPtrUnsafe"/> to access the native pointer to use for interop.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the current instance is disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposedUnsafe()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(_referenceTrackingMask < 0, this);
#else
            if (_referenceTrackingMask < 0)
            {
                throw new ObjectDisposedException(nameof(IObjectReference), "Object reference has been closed.");
            }
#endif
        }

        /// <summary>
        /// Increments the native reference count for the current <see cref="IObjectReference"/> instance.
        /// </summary>
        /// <param name="addRefFromTrackerSource">Whether to also increment the reference count from the tracker source.</param>
        /// <remarks>
        /// This method does not check for disposal, nor does it increment the managed reference count of
        /// the current object. Callers must call <see cref="AddRefUnsafe"/> and <see cref="ReleaseUnsafe"/>.
        /// </remarks>
        internal void NativeAddRefUnsafe(bool addRefFromTrackerSource)
        {
            Marshal.AddRef(GetThisPtrUnsafe());

            if (addRefFromTrackerSource)
            {
                NativeAddRefFromTrackerSourceUnsafe();
            }
        }

        /// <summary>
        /// Increments the native reference count for the current <see cref="IObjectReference"/> instance from the tracker source.
        /// </summary>
        /// <remarks>
        /// This method does not check for disposal, nor does it increment the managed reference count of
        /// the current object. Callers must call <see cref="AddRefUnsafe"/> and <see cref="ReleaseUnsafe"/>.
        /// </remarks>
        internal void NativeAddRefFromTrackerSourceUnsafe()
        {
            IntPtr referenceTrackerPtr = GetReferenceTrackerPtrUnsafe();

            if (referenceTrackerPtr != IntPtr.Zero)
            {
                _ = (**(IReferenceTrackerVftbl**)referenceTrackerPtr).AddRefFromTrackerSource(referenceTrackerPtr);
            }
        }

        /// <summary>
        /// Releases all native resources owned by the current <see cref="IObjectReference"/> instance.
        /// </summary>
        /// <remarks>
        /// Callers are responsible for ensuring no active callers exist when this method is used.
        /// Only <see cref="Dispose()"/> and <see cref="ReleaseUnsafe"/> should call this method.
        /// </remarks>
        private void NativeReleaseUnsafe()
        {
#if DEBUG
            if (BreakOnDispose && System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
#endif

            if (!PreventReleaseOnDispose)
            {
                Release();
            }

            DisposeTrackerSourceUnsafe();

            GC.RemoveMemoryPressure(ComWrappersSupport.GC_PRESSURE_BASE);
        }
    }
}
