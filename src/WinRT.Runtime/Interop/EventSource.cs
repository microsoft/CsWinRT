﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinRT.Interop
{
    // Registration state and delegate cached separately to survive EventSource garbage collection
    // and to prevent the generated event delegate from impacting the lifetime of the
    // event source.
    internal abstract class State : IDisposable
    {
        public EventRegistrationToken token;
        public System.Delegate del;
        public System.Delegate eventInvoke;
        private bool disposedValue;
        private readonly IntPtr obj;
        private readonly int index;
        private readonly System.WeakReference<State> cacheEntry;
        private IntPtr eventInvokePtr;
        private IntPtr referenceTrackerTargetPtr;

        protected State(IntPtr obj, int index)
        {
            this.obj = obj;
            this.index = index;
            eventInvoke = GetEventInvoke();
            cacheEntry = new System.WeakReference<State>(this);
        }

        // The lifetime of this object is managed by the delegate / eventInvoke
        // through its target reference to it.  Once the delegate no longer has
        // any references, this object will also no longer have any references.
        ~State()
        {
            Dispose(false);
        }

        // Allows to retrieve a singleton like weak reference to use
        // with the cache to allow for proper removal with comparision.
        public System.WeakReference<State> GetWeakReferenceForCache()
        {
            return cacheEntry;
        }

        protected abstract System.Delegate GetEventInvoke();

        protected virtual void Dispose(bool disposing)
        {
            // Uses the dispose pattern to ensure we only remove
            // from the cache once: either via unsubscribe or via
            // the finalizer.
            if (!disposedValue)
            {
                Cache.Remove(obj, index, cacheEntry);
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void InitalizeReferenceTracking(IntPtr ptr)
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

        public unsafe bool HasComReferences()
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

    internal sealed class Cache
    {
        private Cache(IWeakReference target, int index, System.WeakReference<State> state)
        {
            this.target = target;
            SetState(index, state);
        }

        private IWeakReference target;
        private readonly ConcurrentDictionary<int, System.WeakReference<State>> states = new();

        private static readonly ReaderWriterLockSlim cachesLock = new();
        private static readonly ConcurrentDictionary<IntPtr, Cache> caches = new();

        private Cache Update(IWeakReference target, int index, System.WeakReference<State> state)
        {
            // If target no longer exists, destroy cache
            lock (this)
            {
                using var resolved = this.target.Resolve(IID.IID_IUnknown);
                if (resolved == null)
                {
                    this.target = target;
                    states.Clear();
                }
            }
            SetState(index, state);
            return this;
        }

        private System.WeakReference<State> GetState(int index)
        {
            // If target no longer exists, destroy cache
            lock (this)
            {
                using var resolved = this.target.Resolve(IID.IID_IUnknown);
                if (resolved == null)
                {
                    return null;
                }
            }

            if (states.TryGetValue(index, out var weakState))
            {
                return weakState;
            }
            return null;
        }

        private void SetState(int index, System.WeakReference<State> state)
        {
            states[index] = state;
        }

        public static void Create(IObjectReference obj, int index, System.WeakReference<State> state)
        {
            // If event source implements weak reference support, track event registrations so that
            // unsubscribes will work across garbage collections.  Note that most static/factory classes
            // do not implement IWeakReferenceSource, so static codegen caching approach is also used.
            IWeakReference target = null;
#if !NET
            try
            {
                var weakRefSource = (IWeakReferenceSource)typeof(IWeakReferenceSource).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { obj });
                if (weakRefSource == null)
                {
                    return;
                }
                target = weakRefSource.GetWeakReference();
            }
            catch (Exception)
            {
                return;
            }
#else
            int hr = obj.TryAs<IUnknownVftbl>(IID.IID_IWeakReferenceSource, out var weakRefSource);
            if (hr != 0)
            {
                return;
            }

            target = ABI.WinRT.Interop.IWeakReferenceSourceMethods.GetWeakReference(weakRefSource);
#endif

            cachesLock.EnterReadLock();
            try
            {
                caches.AddOrUpdate(obj.ThisPtr,
                    (IntPtr ThisPtr) => new Cache(target, index, state),
                    (IntPtr ThisPtr, Cache cache) => cache.Update(target, index, state));
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        public static System.WeakReference<State> GetState(IObjectReference obj, int index)
        {
            if (caches.TryGetValue(obj.ThisPtr, out var cache))
            {
                return cache.GetState(index);
            }

            return null;
        }

        public static void Remove(IntPtr thisPtr, int index, System.WeakReference<State> state)
        {
            if (caches.TryGetValue(thisPtr, out var cache))
            {
#if !NET
                // https://devblogs.microsoft.com/pfxteam/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
                ((ICollection<KeyValuePair<int, System.WeakReference<State>>>)cache.states).Remove(
                    new KeyValuePair<int, System.WeakReference<State>>(index, state));
#else
                cache.states.TryRemove(new KeyValuePair<int, System.WeakReference<State>>(index, state));
#endif
                // using double-checked lock idiom
                if (cache.states.IsEmpty)
                {
                    cachesLock.EnterWriteLock();
                    try
                    {
                        if (cache.states.IsEmpty)
                        {
                            caches.TryRemove(thisPtr, out var _);
                        }
                    }
                    finally
                    {
                        cachesLock.ExitWriteLock();
                    }
                }
            }
        }
    }

    internal unsafe abstract class EventSource<TDelegate>
        where TDelegate : class, MulticastDelegate
    {
        protected readonly IObjectReference _obj;
        protected readonly int _index;
#if NET
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> _addHandler;
#else
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> _addHandler;
#endif
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> _removeHandler;
        protected System.WeakReference<State> _state;
        private readonly (Action<TDelegate>, Action<TDelegate>) _handlerTuple;

        protected EventSource(IObjectReference obj,
#if NET
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index = 0)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _index = index;
            _state = Cache.GetState(obj, index);
            _handlerTuple = (Subscribe, Unsubscribe);
        }

        protected abstract ObjectReferenceValue CreateMarshaler(TDelegate del);

        protected abstract State CreateEventState();

        public void Subscribe(TDelegate del)
        {
            lock (this)
            {
                State state = null;
                bool registerHandler =
                    _state is null ||
                    !_state.TryGetTarget(out state) ||
                    // We have a wrapper delegate, but no longer has any references from any event source.
                    !state.HasComReferences();
                if (registerHandler)
                {
                    state = CreateEventState();
                    _state = state.GetWeakReferenceForCache();
                    Cache.Create(_obj, _index, _state);
                }

                state.del = (TDelegate)global::System.Delegate.Combine(state.del, del);
                if (registerHandler)
                {
                    var eventInvoke = (TDelegate)state.eventInvoke;
                    var marshaler = CreateMarshaler(eventInvoke);
                    try
                    {
                        var nativeDelegate = marshaler.GetAbi();
                        state.InitalizeReferenceTracking(nativeDelegate);
#if NET
                        WinRT.EventRegistrationToken token;
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, &token));
                        state.token = token;
#else
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, out state.token));
#endif
                    }
                    finally
                    {
                        // Dispose our managed reference to the delegate's CCW.
                        // Either the native event holds a reference now or the _addHandler call failed.
                        marshaler.Dispose();
                    }
                }
            }
        }

        public void Unsubscribe(TDelegate del)
        {
            if (_state is null || !_state.TryGetTarget(out var state))
            {
                return;
            }

            lock (this)
            {
                var oldEvent = state.del;
                state.del = (TDelegate)global::System.Delegate.Remove(state.del, del);
                if (oldEvent is object && state.del is null)
                {
                    UnsubscribeFromNative(state);
                }
            }
        }

        public (Action<TDelegate>, Action<TDelegate>) EventActions => _handlerTuple;

        private void UnsubscribeFromNative(State state)
        {
            ExceptionHelpers.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, state.token));
            state.Dispose();
            _state = null;
        }
    }

    internal unsafe sealed class EventSource__EventHandler<T> : EventSource<System.EventHandler<T>>
    {
        internal EventSource__EventHandler(IObjectReference obj,
#if NET
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, WinRT.EventRegistrationToken*, int> addHandler,
#else
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
#endif
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index) : base(obj, addHandler, removeHandler, index)
        {
        }

        protected override ObjectReferenceValue CreateMarshaler(System.EventHandler<T> del) =>
            ABI.System.EventHandler<T>.CreateMarshaler2(del);

        protected override State CreateEventState() =>
            new EventState(_obj.ThisPtr, _index);

        private sealed class EventState : State
        {
            public EventState(IntPtr obj, int index)
                : base(obj, index)
            {
            }

            protected override Delegate GetEventInvoke()
            {
                System.EventHandler<T> handler = (System.Object obj, T e) =>
                {
                    var localDel = (System.EventHandler<T>)del;
                    if (localDel != null)
                        localDel.Invoke(obj, e);
                };
                return handler;
            }
        }
    }
}
