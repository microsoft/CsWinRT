// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Collections.Generic;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type providing caching infrastructure for Windows Runtime events.
/// </summary>
internal sealed unsafe class EventSourceCache
{
    /// <summary>
    /// The reader-writer lock protecting <see cref="Caches"/>.
    /// </summary>
    private static readonly ReaderWriterLockSlim CachesLock = new();

    /// <summary>
    /// The global cache of event source caches.
    /// </summary>
    /// <remarks>
    /// It is responsibility of subscribed states to remove themselves from the cache.
    /// </remarks>
    private static readonly ConcurrentDictionary<nint, EventSourceCache> Caches = new();

    /// <summary>
    /// The set of registered event states for a given <see cref="EventSourceCache"/> instance.
    /// </summary>
    private readonly ConcurrentDictionary<int, WeakReference<object>> _states = new();

    /// <summary>
    /// The target weak reference for the event source cache.
    /// </summary>
    private WindowsRuntimeObjectReference? _target;

    /// <summary>
    /// Creates a new <see cref="EventSourceCache"/> instance with the specified parameters.
    /// </summary>
    /// <param name="target">The target weak reference for the event source cache.</param>
    /// <param name="index">The index of the target event being registered first.</param>
    /// <param name="state">The event state currently being registered.</param>
    private EventSourceCache(WindowsRuntimeObjectReference target, int index, WeakReference<object> state)
    {
        _target = target;

        SetState(index, state);
    }

    /// <summary>
    /// Creates a new <see cref="EventSourceCache"/> instance for the target event and state.
    /// </summary>
    /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> instance for the object exposing the event.</param>
    /// <param name="index">The index of the event being registered.</param>
    /// <param name="state">The state for the event registration.</param>
    public static void Create(WindowsRuntimeObjectReference objectReference, int index, WeakReference<object> state)
    {
        // Try to get the weak reference source for the input object (it's not guaranteed to be present)
        if (!objectReference.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IWeakReferenceSource, out void* weakRefSourceSource))
        {
            return;
        }

        // If event source implements weak reference support, track event registrations so that
        // unsubscribes will work across garbage collections. Note that most static/factory classes
        // do not implement 'IWeakReferenceSource', so a static codegen caching approach is also used.
        WindowsRuntimeObjectReference target;
        void* thisPtr;

        try
        {
            objectReference.AddRefUnsafe();

            // This pointer is just used as a dictionary key, we don't need to actually keep it alive.
            // Because this call might be expensive (ie. require marshalling), do this outside the lock.
            thisPtr = objectReference.GetThisPtrUnsafe();

            objectReference.ReleaseUnsafe();

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

            // Resolve the weak reference from the current object
            HRESULT hresult = IWeakReferenceSourceVftbl.GetWeakReferenceUnsafe(weakRefSourceSource, &weakReference);

            // The call above should pretty much always succeed
            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            target = WindowsRuntimeObjectReference.AttachUnsafe(
                ref weakReference,
                in WellKnownWindowsInterfaceIIDs.IID_IWeakReference)!;
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(weakRefSourceSource);
        }

        WindowsRuntimeObjectReference? targetToDispose = null;

        try
        {
            while (true)
            {
                CachesLock.EnterReadLock();

                try
                {
                    if (Caches.TryGetValue((nint)thisPtr, out EventSourceCache? cache))
                    {
                        cache.Update(target, index, state, out targetToDispose);
                        target = null!;
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
                        _ = Caches.TryAdd((nint)thisPtr, new EventSourceCache(target, index, state));
                        target = null!;
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
            target?.Dispose();
            targetToDispose?.Dispose();
        }
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

        // Get the pointer value for the lookup (see notes above)
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
    public static void Remove(void* thisPtr, int index, WeakReference<object> state)
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

        // Using double-checked lock idiom to only take the lock when we might actually have a match
        if (cache._states.IsEmpty)
        {
            bool shouldDispose = false;

            CachesLock.EnterWriteLock();

            try
            {
                if (cache._states.IsEmpty)
                {
                    if (Caches.TryRemove(new KeyValuePair<nint, EventSourceCache>((nint)thisPtr, cache)))
                    {
                        shouldDispose = true;
                    }
                }
            }
            finally
            {
                CachesLock.ExitWriteLock();
            }

            if (shouldDispose)
            {
                cache.Dispose();
            }
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
        void* weakReference = null;

        lock (this)
        {
            if (_target is null)
            {
                return false;
            }

            ResolveTargetUnsafe(_target, out weakReference);
        }

        if (weakReference is null)
        {
            return false;
        }

        _ = IUnknownVftbl.ReleaseUnsafe(weakReference);

        SetState(index, state);

        return true;
    }

    /// <summary>
    /// Updates the cache for a given event.
    /// </summary>
    /// <param name="target">The target native object for the event.</param>
    /// <param name="index">The event index.</param>
    /// <param name="state">The event state.</param>
    /// <param name="targetToDispose">The native weak reference that is no longer used by the cache.</param>
    private void Update(
        WindowsRuntimeObjectReference target,
        int index,
        WeakReference<object> state,
        out WindowsRuntimeObjectReference targetToDispose)
    {
        void* weakReference = null;

        // If the target no longer exists, destroy the cache
        lock (this)
        {
            // The global read lock prevents the cache from being removed and disposed while updating.
            ResolveTargetUnsafe(_target!, out weakReference);

            // Update the target and clear the state if the old target is not alive anymore
            if (weakReference is null)
            {
                targetToDispose = _target!;
                _target = target;
                target = null!;

                _states.Clear();
            }
            else
            {
                targetToDispose = target;
                target = null!;
            }
        }

        // Release native references outside of the lock to avoid holding it for longer.
        if (weakReference is not null)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(weakReference);
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
        void* weakReference = null;

        // If target no longer exists, destroy cache
        lock (this)
        {
            if (_target is null)
            {
                return null;
            }

            ResolveTargetUnsafe(_target, out weakReference);
        }

        // There's no state to return if the target is not alive anymore
        if (weakReference is null)
        {
            return null;
        }

        // Release the weak reference outside the lock (see notes above)
        _ = IUnknownVftbl.ReleaseUnsafe(weakReference);

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
        WindowsRuntimeObjectReference? target;

        lock (this)
        {
            target = _target;
            _target = null;
        }

        target?.Dispose();
    }

    /// <summary>
    /// Resolves a native weak reference to an <c>IUnknown</c> pointer.
    /// </summary>
    /// <param name="target">The native weak reference to resolve.</param>
    /// <param name="objectReference">The resolved strong reference, if the target is still alive.</param>
    private static void ResolveTargetUnsafe(WindowsRuntimeObjectReference target, out void* objectReference)
    {
        using WindowsRuntimeObjectReferenceValue targetValue = target.AsValue();
        Guid iid = WellKnownWindowsInterfaceIIDs.IID_IUnknown;
        void* resolvedObject = null;

        HRESULT hresult = IWeakReferenceVftbl.ResolveUnsafe(targetValue.GetThisPtrUnsafe(), &iid, &resolvedObject);

        if (hresult.Failed)
        {
            resolvedObject = null;
        }

        objectReference = resolvedObject;
    }
}
