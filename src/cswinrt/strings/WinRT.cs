// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value
#pragma warning disable CA1060

namespace WinRT
{
    internal static partial class DelegateExtensions
    {
        public static void DynamicInvokeAbi(this System.Delegate del, object[] invoke_params)
        {
            Marshal.ThrowExceptionForHR((int)del.DynamicInvoke(invoke_params));
        }
    }

    internal static class IActivationFactoryMethods
    {
        public static unsafe ObjectReference<I> ActivateInstance<I>(IObjectReference obj)
        {
            IntPtr instancePtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)obj.ThisPtr)[6](obj.ThisPtr, &instancePtr));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface<I>(instancePtr);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(instancePtr);
            }
        }
    }

    internal class ComponentActivationFactory : global::WinRT.Interop.IActivationFactory
    {
        public IntPtr ActivateInstance()
        {
            throw new NotImplementedException();
        }
    }

    internal class ActivatableComponentActivationFactory<T> : ComponentActivationFactory, global::WinRT.Interop.IActivationFactory where T : class, new()
    {
        public new IntPtr ActivateInstance()
        {
            T comp = new T();
            return MarshalInspectable<T>.FromManaged(comp);
        }
    }

#pragma warning disable CA2002

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
            int hr = Marshal.QueryInterface(ptr, ref Unsafe.AsRef(IReferenceTrackerTargetVftbl.IID), out referenceTrackerTargetPtr);
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
                IReferenceTrackerTargetVftbl vftblReferenceTracker = **(IReferenceTrackerTargetVftbl**)referenceTrackerTargetPtr;
                vftblReferenceTracker.AddRefFromReferenceTracker(referenceTrackerTargetPtr);
                uint refTrackerCount = vftblReferenceTracker.ReleaseFromReferenceTracker(referenceTrackerTargetPtr);
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
        private readonly ConcurrentDictionary<int, System.WeakReference<State>> states = new ConcurrentDictionary<int, System.WeakReference<State>>();

        private static readonly ReaderWriterLockSlim cachesLock = new ReaderWriterLockSlim();
        private static readonly ConcurrentDictionary<IntPtr, Cache> caches = new ConcurrentDictionary<IntPtr, Cache>();

        private Cache Update(IWeakReference target, int index, System.WeakReference<State> state)
        {
            // If target no longer exists, destroy cache
            lock (this)
            {
                using var resolved = this.target.Resolve(InterfaceIIDs.IUnknown_IID);
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
                using var resolved = this.target.Resolve(InterfaceIIDs.IUnknown_IID);
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
                catch(Exception)
                {
                    return;
                }
#else
            int hr = obj.TryAs<IUnknownVftbl>(InterfaceIIDs.IWeakReferenceSource_IID, out var weakRefSource);
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
                    var localDel = (System.EventHandler<T>) del;
                    if (localDel != null)
                        localDel.Invoke(obj, e);
                };
                return handler;
            }
        }
    }

#pragma warning restore CA2002

    internal static class InterfaceIIDs
    {
#if NET
        internal static readonly Guid IInspectable_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xE0, 0xE2, 0x86, 0xAF, 0x2D, 0xB1, 0x6A, 0x4C, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90 }));
        internal static readonly Guid IUnknown_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IWeakReferenceSource_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x38, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IWeakReference_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x37, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IActivationFactory_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IAgileObject_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x94, 0x2B, 0xEA, 0x94, 0xCC, 0xE9, 0xE0, 0x49, 0xC0, 0xFF, 0xEE, 0x64, 0xCA, 0x8F, 0x5B, 0x90 }));
        internal static readonly Guid IMarshal_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid IContextCallback_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xDA, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 }));
        internal static readonly Guid ICallbackWithNoReentrancyToApplicationSTA_IID = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x74, 0x97, 0x29, 0x0A, 0x4E, 0x3E, 0x42, 0xFC, 0x1D, 0x9D, 0x72, 0xCE, 0xE1, 0x05, 0xCA, 0x57 }));
#else
        internal static readonly Guid IInspectable_IID = new(0xAF86E2E0, 0xB12D, 0x4c6a, 0x9C, 0x5A, 0xD7, 0xAA, 0x65, 0x10, 0x1E, 0x90);
        internal static readonly Guid IUnknown_IID = new(0, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IWeakReferenceSource_IID = new(0x00000038, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IWeakReference_IID = new(0x00000037, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IActivationFactory_IID = new (0x00000035, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IAgileObject_IID = new(0x94ea2b94, 0xe9cc, 0x49e0, 0xc0, 0xff, 0xee, 0x64, 0xca, 0x8f, 0x5b, 0x90);
        internal static readonly Guid IMarshal_IID = new(0x00000003, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid IContextCallback_IID = new(0x000001da, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
        internal static readonly Guid ICallbackWithNoReentrancyToApplicationSTA_IID = new(0x0A299774, 0x3E4E, 0xFC42, 0x1D, 0x9D, 0x72, 0xCE, 0xE1, 0x05, 0xCA, 0x57);
#endif
    }
}

#if !NET
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
}
#endif

namespace WinRT
{
    internal static class ProjectionInitializer
    {
        [ModuleInitializer]
        internal static void InitalizeProjection()
        {
            ComWrappersSupport.RegisterProjectionAssembly(typeof(ProjectionInitializer).Assembly);
        }
    }
}
