// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Runtime.InteropServices.Marshalling;
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
    private IWeakReference _target;

    /// <summary>
    /// Creates a new <see cref="EventSourceCache"/> instance with the specified parameters.
    /// </summary>
    /// <param name="target">The target weak reference for the event source cache.</param>
    /// <param name="index">The index of the target event being registered first.</param>
    /// <param name="state">The event state currently being registered.</param>
    private EventSourceCache(IWeakReference target, int index, WeakReference<object> state)
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
        IWeakReference target;

        try
        {
            void* weakReference;

            // Resolve the weak reference from the current object
            HRESULT hresult = IWeakReferenceSourceVftbl.GetWeakReferenceUnsafe(weakRefSourceSource, &weakReference);

            // The call above should pretty much always succeed
            RestrictedErrorInfo.ThrowExceptionForHR(hresult);

            // Let the default 'ComWrappers' implementation handle the weak reference.
            // We're intentionally doing this so 'WindowsRuntimeComWrappers' can be
            // used exclusively for Windows Runtime types, which keeps it simpler.
            try
            {
                target = ComInterfaceMarshaller<IWeakReference>.ConvertToManaged(weakReference)!;
            }
            finally
            {
                _ = IUnknownVftbl.ReleaseUnsafe(weakReference);
            }
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(weakRefSourceSource);
        }

        objectReference.AddRefUnsafe();

        // This pointer is just used as a dictionary key, we don't need to actually keep it alive.
        // Because this call might be expensive (ie. require marshalling), do this outside the lock.
        void* thisPtr = objectReference.GetThisPtrUnsafe();

        objectReference.ReleaseUnsafe();

        CachesLock.EnterReadLock();

        try
        {
            // Add a new cache instance to the global map, or update the existing one, if present
            _ = Caches.AddOrUpdate(
                key: (nint)thisPtr,
                addValueFactory: static (thisPtr, args) => new EventSourceCache(args.Target, args.Index, args.State),
                updateValueFactory: static (thisPtr, cache, args) => cache.Update(args.Target, args.Index, args.State),
                factoryArgument: new CachesFactoryArgs(target, index, state));
        }
        finally
        {
            CachesLock.ExitReadLock();
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
            CachesLock.EnterWriteLock();

            try
            {
                if (cache._states.IsEmpty)
                {
                    _ = Caches.TryRemove((nint)thisPtr, out _);
                }
            }
            finally
            {
                CachesLock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Updates the cache for a given event.
    /// </summary>
    /// <param name="target">The target native object for the event.</param>
    /// <param name="index">The event index.</param>
    /// <param name="state">The event state.</param>
    /// <returns>The current <see cref="EventSourceCache"/> instance.</returns>
    private EventSourceCache Update(IWeakReference target, int index, WeakReference<object> state)
    {
        void* weakReference = null;

        // If the target no longer exists, destroy the cache
        lock (this)
        {
            _ = _target.Resolve(WellKnownWindowsInterfaceIIDs.IID_IUnknown, out weakReference);

            // Update the target and clear the state if the old target is not alive anymore
            if (weakReference is null)
            {
                _target = target;

                _states.Clear();
            }
        }

        // Release the weak reference if we got one, we don't actually need it.
        // We can do this outside of the lock to avoid holding it for longer.
        if (weakReference is not null)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(weakReference);
        }

        SetState(index, state);

        return this;
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
            _ = _target.Resolve(WellKnownWindowsInterfaceIIDs.IID_IUnknown, out weakReference);
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
}

/// <summary>
/// Arguments for the <see cref="EventSourceCache"/> factory.
/// </summary>
/// <param name="target"><inheritdoc cref="Target" path="/summary/node()"/></param>
/// <param name="index"><inheritdoc cref="Index" path="/summary/node()"/></param>
/// <param name="state"><inheritdoc cref="State" path="/summary/node()"/></param>
file readonly struct CachesFactoryArgs(
    IWeakReference target,
    int index,
    WeakReference<object> state)
{
    // We're intentionally using a separate type and not a value tuple, because creating generic
    // type instantiations with this type when delegates are involved results in additional
    // metadata being preserved after trimming. This can save a few KBs in binary size on Native AOT.
    // See: https://github.com/dotnet/runtime/pull/111204#issuecomment-2599397292.

    /// <summary>
    /// The target weak reference.
    /// </summary>
    public readonly IWeakReference Target = target;

    /// <summary>
    /// The event index.
    /// </summary>
    public readonly int Index = index;

    /// <summary>
    /// A weak reference to the event state.
    /// </summary>
    public readonly WeakReference<object> State = state;
}