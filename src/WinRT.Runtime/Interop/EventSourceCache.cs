// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using WinRT;
using WinRT.Interop;

namespace ABI.WinRT.Interop
{
    internal sealed class EventSourceCache
    {
        private static readonly ReaderWriterLockSlim cachesLock = new();
        private static readonly ConcurrentDictionary<IntPtr, EventSourceCache> caches = new();

        private readonly ConcurrentDictionary<int, global::System.WeakReference<object>> states = new();
        private global::WinRT.Interop.IWeakReference target;

        private EventSourceCache(global::WinRT.Interop.IWeakReference target, int index, global::System.WeakReference<object> state)
        {
            this.target = target;

            SetState(index, state);
        }

        private EventSourceCache Update(global::WinRT.Interop.IWeakReference target, int index, global::System.WeakReference<object> state)
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

        private global::System.WeakReference<object> GetState(int index)
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

        private void SetState(int index, global::System.WeakReference<object> state)
        {
            states[index] = state;
        }

        public static void Create(IObjectReference obj, int index, global::System.WeakReference<object> state)
        {
            // If event source implements weak reference support, track event registrations so that
            // unsubscribes will work across garbage collections.  Note that most static/factory classes
            // do not implement IWeakReferenceSource, so static codegen caching approach is also used.
            global::WinRT.Interop.IWeakReference target = null;
#if !NET
            try
            {
                var weakRefSource = (global::WinRT.Interop.IWeakReferenceSource)typeof(global::WinRT.Interop.IWeakReferenceSource).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { obj });
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
#if NET
                caches.AddOrUpdate(
                    key: obj.ThisPtr,
                    addValueFactory: static (IntPtr ThisPtr, CachesFactoryArgs args) => new EventSourceCache(args.Target, args.Index, args.State),
                    updateValueFactory: static (IntPtr ThisPtr, EventSourceCache cache, CachesFactoryArgs args) => cache.Update(args.Target, args.Index, args.State),
                    factoryArgument: new CachesFactoryArgs(target, index, state));
#else
                caches.AddOrUpdate(obj.ThisPtr,
                    (IntPtr ThisPtr) => new EventSourceCache(target, index, state),
                    (IntPtr ThisPtr, EventSourceCache cache) => cache.Update(target, index, state));
#endif
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        public static global::System.WeakReference<object> GetState(IObjectReference obj, int index)
        {
            if (caches.TryGetValue(obj.ThisPtr, out var cache))
            {
                return cache.GetState(index);
            }

            return null;
        }

        public static void Remove(IntPtr thisPtr, int index, global::System.WeakReference<object> state)
        {
            if (caches.TryGetValue(thisPtr, out var cache))
            {
#if !NET
                // https://devblogs.microsoft.com/pfxteam/little-known-gems-atomic-conditional-removals-from-concurrentdictionary/
                ((ICollection<KeyValuePair<int, global::System.WeakReference<object>>>)cache.states).Remove(
                    new KeyValuePair<int, global::System.WeakReference<object>>(index, state));
#else
                cache.states.TryRemove(new KeyValuePair<int, global::System.WeakReference<object>>(index, state));
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

#if NET
        // We're intentionally using a separate type and not a value tuple, because creating generic
        // type instantiations with this type when delegates are involved results in additional
        // metadata being preserved after trimming. This can save a few KBs in binary size on Native AOT.
        // See: https://github.com/dotnet/runtime/pull/111204#issuecomment-2599397292.
        private readonly struct CachesFactoryArgs(
            global::WinRT.Interop.IWeakReference target,
            int index,
            global::System.WeakReference<object> state)
        {
            public readonly global::WinRT.Interop.IWeakReference Target = target;
            public readonly int Index = index;
            public readonly global::System.WeakReference<object> State = state;
        }
#endif
    }
}
