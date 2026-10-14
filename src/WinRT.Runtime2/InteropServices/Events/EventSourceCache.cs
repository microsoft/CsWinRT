// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type providing caching infrastructure for Windows Runtime events.
/// </summary>
internal sealed unsafe class EventSourceCache
{
    /// <summary>
    /// The reader-writer lock protecting <see cref="Caches"/>.
    /// </summary>
    /// <remarks>
    /// Holding the read lock prevents a cache from being removed and disposed.
    /// </remarks>
    private static readonly ReaderWriterLockSlim CachesLock = new();

    /// <summary>
    /// The global cache of event source caches.
    /// </summary>
    /// <remarks>
    /// It is the responsibility of subscribed states to remove themselves from the cache.
    /// </remarks>
    private static readonly ConcurrentDictionary<nint, EventSourceCache> Caches = new();

    /// <summary>
    /// The registered event states for the current cache, keyed by event index.
    /// </summary>
    private readonly ConcurrentDictionary<int, WeakReference<object>> _states = new();

    /// <summary>
    /// The native weak reference owned by the current cache, or <see langword="null"/> after disposal.
    /// </summary>
    private WindowsRuntimeObjectReference? _targetWeakReference;

    /// <summary>
    /// Creates a new <see cref="EventSourceCache"/> instance with the specified parameters.
    /// </summary>
    /// <param name="targetWeakReference">The native weak reference whose ownership is transferred to the cache.</param>
    /// <param name="index">The index of the target event being registered first.</param>
    /// <param name="state">The event state currently being registered.</param>
    private EventSourceCache(WindowsRuntimeObjectReference targetWeakReference, int index, WeakReference<object> state)
    {
        _targetWeakReference = targetWeakReference;

        SetState(index, state);
    }

    /// <summary>
    /// Caches a state for the target event, creating an <see cref="EventSourceCache"/> instance if needed.
    /// </summary>
    /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> instance for the object exposing the event.</param>
    /// <param name="index">The index of the event being registered.</param>
    /// <param name="state">The state for the event registration.</param>
    public static void Create(WindowsRuntimeObjectReference objectReference, int index, WeakReference<object> state)
    {
        // Try to get the weak reference source for the input object (it's not guaranteed to be present)
        if (!objectReference.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource, out void* weakReferenceSource))
        {
            return;
        }

        // If event source implements weak reference support, track event registrations so that
        // unsubscribes will work across garbage collections. Note that most static/factory classes
        // do not implement 'IWeakReferenceSource', so a static codegen caching approach is also used.
        WindowsRuntimeObjectReference targetWeakReference;
        void* thisPtr;

        try
        {
            objectReference.AddRefUnsafe();

            // This pointer is just used as a dictionary key, we don't need to actually keep it alive.
            // Because this call might be expensive (ie. require marshalling), do this outside the lock.
            thisPtr = objectReference.GetThisPtrUnsafe();

            objectReference.ReleaseUnsafe();

            // Reuse a live cache without acquiring another native weak reference
            CachesLock.EnterReadLock();

            try
            {
                if (Caches.TryGetValue((nint)thisPtr, out EventSourceCache? cache) &&
                    cache.TrySetStateIfTargetAlive(index, state))
                {
                    return;
                }
            }
            finally
            {
                CachesLock.ExitReadLock();
            }

            void* weakReference;

            // Get a new native weak reference to the object
            HRESULT hresult = IWeakReferenceSourceVftbl.GetWeakReferenceUnsafe(weakReferenceSource, &weakReference);

            // The call above should pretty much always succeed
            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            targetWeakReference = WindowsRuntimeObjectReference.AttachUnsafe(
                thisPtr: ref weakReference,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IWeakReference)!;
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(weakReferenceSource);
        }

        AddOrUpdateState(thisPtr, targetWeakReference, index, state);
    }

    /// <summary>
    /// Gets the state for a given event, for a given object, if present.
    /// </summary>
    /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> instance for the object exposing the event.</param>
    /// <param name="index">The index of the event to retrieve the state for.</param>
    /// <returns>The state for the target event, if present.</returns>
    public static WeakReference<object>? GetState(WindowsRuntimeObjectReference objectReference, int index)
    {
        objectReference.AddRefUnsafe();

        // The pointer is only used as a cache key, so it does not need to be kept alive
        void* thisPtr = objectReference.GetThisPtrUnsafe();

        objectReference.ReleaseUnsafe();

        return Caches.TryGetValue((nint)thisPtr, out EventSourceCache? cache) ? cache.GetState(index) : null;
    }

    /// <summary>
    /// Removes the state for a given event, for a given object, if present.
    /// </summary>
    /// <param name="thisPtr">The pointer for the native object to unregister the event for.</param>
    /// <param name="index">The index of the event being unregistered.</param>
    /// <param name="state">The state for the event being unregistered.</param>
    public static void RemoveState(void* thisPtr, int index, WeakReference<object> state)
    {
        if (!Caches.TryGetValue((nint)thisPtr, out EventSourceCache? cache))
        {
            return;
        }

        // If we failed to remove the entry, we can stop here without checking the actual state. Even if there
        // was a value when we were called, we might've raced against another thread, which removed the item
        // first. That is still fine: this thread can stop here, and the one that won the race will do the
        // check below and cleanup the event cache instance in case that was the last remaining cache entry.
        if (!cache._states.TryRemove(new KeyValuePair<int, WeakReference<object>>(index, state)))
        {
            return;
        }

        // Only take the write lock if this might have been the last state
        if (!cache._states.IsEmpty)
        {
            return;
        }

        bool shouldDispose;

        CachesLock.EnterWriteLock();

        try
        {
            // Recheck under the write lock, and only remove this exact cache instance.
            // Another thread might have registered a state or replaced the cache.
            shouldDispose = cache._states.IsEmpty &&
                Caches.TryRemove(new KeyValuePair<nint, EventSourceCache>((nint)thisPtr, cache));
        }
        finally
        {
            CachesLock.ExitWriteLock();
        }

        // Dispose outside the global lock, as releasing a native reference can be expensive
        if (shouldDispose)
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Adds or updates the cache for a given event, taking ownership of a new native weak reference.
    /// </summary>
    /// <param name="thisPtr">The native object pointer used as the cache key.</param>
    /// <param name="targetWeakReference">The native weak reference whose ownership is transferred to this method.</param>
    /// <param name="index">The event index.</param>
    /// <param name="state">The event state.</param>
    private static void AddOrUpdateState(
        void* thisPtr,
        WindowsRuntimeObjectReference targetWeakReference,
        int index,
        WeakReference<object> state)
    {
        WindowsRuntimeObjectReference? ownedTargetWeakReference = targetWeakReference;
        WindowsRuntimeObjectReference? targetWeakReferenceToDispose = null;

        try
        {
            // Update an existing cache under the read lock, or add one under the write lock.
            // Retry if another thread added a cache between the two lock acquisitions.
            while (true)
            {
                CachesLock.EnterReadLock();

                try
                {
                    if (Caches.TryGetValue((nint)thisPtr, out EventSourceCache? cache))
                    {
                        cache.UpdateState(ownedTargetWeakReference, index, state, out targetWeakReferenceToDispose);

                        // The reference is now owned by the cache or scheduled for disposal
                        ownedTargetWeakReference = null;

                        break;
                    }
                }
                finally
                {
                    CachesLock.ExitReadLock();
                }

                CachesLock.EnterWriteLock();

                try
                {
                    if (!Caches.ContainsKey((nint)thisPtr))
                    {
                        _ = Caches.TryAdd((nint)thisPtr, new EventSourceCache(ownedTargetWeakReference, index, state));

                        // The new cache owns the reference
                        ownedTargetWeakReference = null;

                        break;
                    }
                }
                finally
                {
                    CachesLock.ExitWriteLock();
                }
            }
        }
        finally
        {
            // Release unconsumed or replaced references only after leaving the global locks
            ownedTargetWeakReference?.Dispose();
            targetWeakReferenceToDispose?.Dispose();
        }
    }

    /// <summary>
    /// Tries to cache an event state if the native target is still alive.
    /// </summary>
    /// <param name="index">The event index.</param>
    /// <param name="state">The event state.</param>
    /// <returns>Whether the state was cached.</returns>
    private bool TrySetStateIfTargetAlive(int index, WeakReference<object> state)
    {
        void* targetPtr;

        lock (this)
        {
            if (_targetWeakReference is null)
            {
                return false;
            }

            targetPtr = ResolveTargetUnsafe(_targetWeakReference);
        }

        if (targetPtr is null)
        {
            return false;
        }

        // The resolved strong reference is only needed to check whether the target is alive
        _ = IUnknownVftbl.ReleaseUnsafe(targetPtr);

        SetState(index, state);

        return true;
    }

    /// <summary>
    /// Updates the cache for a given event.
    /// </summary>
    /// <param name="targetWeakReference">The new native weak reference to cache or dispose.</param>
    /// <param name="index">The event index.</param>
    /// <param name="state">The event state.</param>
    /// <param name="targetWeakReferenceToDispose">The replaced or unused native weak reference to dispose outside the global lock.</param>
    /// <remarks>
    /// The caller must hold the global read lock to prevent the cache from being removed and disposed.
    /// </remarks>
    private void UpdateState(
        WindowsRuntimeObjectReference targetWeakReference,
        int index,
        WeakReference<object> state,
        out WindowsRuntimeObjectReference targetWeakReferenceToDispose)
    {
        void* targetPtr;

        lock (this)
        {
            targetPtr = ResolveTargetUnsafe(_targetWeakReference!);

            // A reused native address must not retain states from the previous target
            if (targetPtr is null)
            {
                targetWeakReferenceToDispose = _targetWeakReference!;
                _targetWeakReference = targetWeakReference;

                _states.Clear();
            }
            else
            {
                // The existing target is still alive, so the incoming weak reference is redundant
                targetWeakReferenceToDispose = targetWeakReference;
            }
        }

        // Release the resolved strong reference outside the instance lock
        if (targetPtr is not null)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(targetPtr);
        }

        SetState(index, state);
    }

    /// <summary>
    /// Gets the state for a given event index.
    /// </summary>
    /// <param name="index">The index of the event to get the state for.</param>
    /// <returns>The state for the target event, if present.</returns>
    private WeakReference<object>? GetState(int index)
    {
        void* targetPtr;

        lock (this)
        {
            // Lookups do not hold the global read lock, so the cache might already be disposed
            if (_targetWeakReference is null)
            {
                return null;
            }

            targetPtr = ResolveTargetUnsafe(_targetWeakReference);
        }

        // There's no state to return if the target is not alive anymore
        if (targetPtr is null)
        {
            return null;
        }

        // Release the resolved strong reference outside the instance lock
        _ = IUnknownVftbl.ReleaseUnsafe(targetPtr);

        return _states.TryGetValue(index, out WeakReference<object>? weakState) ? weakState : null;
    }

    /// <summary>
    /// Sets the state for a given event index.
    /// </summary>
    /// <param name="index">The index of the event to set the state for.</param>
    /// <param name="state">The event state to set.</param>
    private void SetState(int index, WeakReference<object> state)
    {
        _states[index] = state;
    }

    /// <summary>
    /// Releases the native weak reference owned by the current cache.
    /// </summary>
    private void Dispose()
    {
        WindowsRuntimeObjectReference? targetWeakReference;

        lock (this)
        {
            targetWeakReference = _targetWeakReference;

            _targetWeakReference = null;
        }

        // Native cleanup must not keep the instance lock held
        targetWeakReference?.Dispose();
    }

    /// <summary>
    /// Resolves a native weak reference to a strong <c>IUnknown</c> pointer.
    /// </summary>
    /// <param name="targetWeakReference">The native weak reference to resolve.</param>
    /// <returns>
    /// The strong reference to release after use, or <see langword="null"/> if the target is no longer alive or resolution fails.
    /// </returns>
    private static void* ResolveTargetUnsafe(WindowsRuntimeObjectReference targetWeakReference)
    {
        using WindowsRuntimeObjectReferenceValue targetWeakReferenceValue = targetWeakReference.AsValue();
        Guid iid = WellKnownWindowsInterfaceIIDs.IID_IUnknown;
        void* targetPtr = null;

        HRESULT hresult = IWeakReferenceVftbl.ResolveUnsafe(targetWeakReferenceValue.GetThisPtrUnsafe(), &iid, &targetPtr);

        return hresult.Failed ? null : targetPtr;
    }
}
