using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq.Expressions;
using System.Diagnostics;
using WinRT.Interop;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value
#pragma warning disable CA1060

namespace WinRT
{
    internal static class DelegateExtensions
    {
        public static void DynamicInvokeAbi(this System.Delegate del, object[] invoke_params)
        {
            Marshal.ThrowExceptionForHR((int)del.DynamicInvoke(invoke_params));
        }
    }

    internal class Platform
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoCreateInstance(ref Guid clsid, IntPtr outer, uint clsContext, ref Guid iid, IntPtr* instance);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern int CoDecrementMTAUsage(IntPtr cookie);

        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr moduleHandle);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, BestFitMapping = false)]
        internal static unsafe extern void* TryGetProcAddress(IntPtr moduleHandle, [MarshalAs(UnmanagedType.LPStr)] string functionName);
        internal static unsafe void* GetProcAddress(IntPtr moduleHandle, string functionName)
        {
            void* functionPtr = Platform.TryGetProcAddress(moduleHandle, functionName);
            if (functionPtr == null)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error(), new IntPtr(-1));
            }
            return functionPtr;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryExW([MarshalAs(UnmanagedType.LPWStr)] string fileName, IntPtr fileHandle, uint flags);

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, IntPtr* factory);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString,
                                                  int length,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsCreateStringReference(char* sourceString,
                                                  int length,
                                                  IntPtr* hstring_header,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern int WindowsDeleteString(IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int WindowsDuplicateString(IntPtr sourceString,
                                                  IntPtr* hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe char* WindowsGetStringRawBuffer(IntPtr hstring, uint* length);

        [DllImport("api-ms-win-core-com-l1-1-1.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern unsafe int RoGetAgileReference(uint options, ref Guid iid, IntPtr unknown, IntPtr* agileReference);
    }

    internal struct VftblPtr
    {
        public IntPtr Vftbl;
    }

    internal unsafe class DllModule
    {
        readonly string _fileName;
        readonly IntPtr _moduleHandle;
        readonly delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> _GetActivationFactory;
        readonly delegate* unmanaged[Stdcall]<int> _CanUnloadNow; // TODO: Eventually periodically call

        static readonly string _currentModuleDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static Dictionary<string, DllModule> _cache = new System.Collections.Generic.Dictionary<string, DllModule>(StringComparer.Ordinal);

        public static bool TryLoad(string fileName, out DllModule module)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(fileName, out module))
                {
                    return true;
                }
                else if (TryCreate(fileName, out module))
                {
                    _cache[fileName] = module;
                    return true;
                }
                return false;
            }
        }

        static unsafe bool TryCreate(string fileName, out DllModule module)
        {
            // Explicitly look for module in the same directory as this one, and
            // use altered search path to ensure any dependencies in the same directory are found.
            var moduleHandle = Platform.LoadLibraryExW(System.IO.Path.Combine(_currentModuleDirectory, fileName), IntPtr.Zero, /* LOAD_WITH_ALTERED_SEARCH_PATH */ 8);
#if !NETSTANDARD2_0 && !NETCOREAPP2_0
            if (moduleHandle == IntPtr.Zero)
            {
                NativeLibrary.TryLoad(fileName, Assembly.GetExecutingAssembly(), null, out moduleHandle);
            }
#endif
            if (moduleHandle == IntPtr.Zero)
            {
                module = null;
                return false;
            }

            var getActivationFactory = Platform.TryGetProcAddress(moduleHandle, "DllGetActivationFactory");
            if (getActivationFactory == null)
            {
                module = null;
                return false;
            }
            
            module = new DllModule(
                fileName, 
                moduleHandle, 
                getActivationFactory);
            return true;
        }

        DllModule(string fileName, IntPtr moduleHandle, void* getActivationFactory)
        {
            _fileName = fileName;
            _moduleHandle = moduleHandle;
            _GetActivationFactory = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)getActivationFactory;

            var canUnloadNow = Platform.TryGetProcAddress(_moduleHandle, "DllCanUnloadNow");
            if (canUnloadNow != null)
            {
                _CanUnloadNow = (delegate* unmanaged[Stdcall]<int>)canUnloadNow;
            }
        }

        public unsafe (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            IntPtr instancePtr;
            var hstrRuntimeClassId = MarshalString.CreateMarshaler(runtimeClassId);
            try
            {
                int hr = _GetActivationFactory(MarshalString.GetAbi(hstrRuntimeClassId), &instancePtr);
                return (hr == 0 ? ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr) : null, hr);
            }
            finally
            {
                hstrRuntimeClassId.Dispose();
            }
        }

        ~DllModule()
        {
            System.Diagnostics.Debug.Assert(_CanUnloadNow == null || _CanUnloadNow() == 0); // S_OK
            lock (_cache)
            {
                _cache.Remove(_fileName);
            }
            if ((_moduleHandle != IntPtr.Zero) && !Platform.FreeLibrary(_moduleHandle))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }
    }

    internal class WeakLazy<T> where T : class, new()
    {
        WeakReference<T> _instance = new WeakReference<T>(null);
        public T Value
        {
            get
            {
                lock (_instance)
                {
                    T value;
                    if (!_instance.TryGetTarget(out value))
                    {
                        value = new T();
                        _instance.SetTarget(value);
                    }
                    return value;
                }
            }
        }
    }

    internal class WinrtModule
    {
        readonly IntPtr _mtaCookie;
        volatile static WinrtModule _instance;
        private static WinrtModule MakeWinRTModule()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _instance, new WinrtModule(), null);
            return _instance;
        }
        public static WinrtModule Instance => _instance ?? MakeWinRTModule();

        public unsafe WinrtModule()
        {
            IntPtr mtaCookie;
            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&mtaCookie));
            _mtaCookie = mtaCookie;
        }

        public static unsafe (IntPtr instancePtr, int hr) GetActivationFactory(IntPtr hstrRuntimeClassId)
        {
            var module = Instance; // Ensure COM is initialized
            Guid iid = typeof(IActivationFactoryVftbl).GUID;
            IntPtr instancePtr;
            int hr = Platform.RoGetActivationFactory(hstrRuntimeClassId, ref iid, &instancePtr);
            return (hr == 0 ? instancePtr : IntPtr.Zero, hr);
        }

        public static unsafe (ObjectReference<IActivationFactoryVftbl> obj, int hr) GetActivationFactory(string runtimeClassId)
        {
            // TODO: "using var" with ref struct and remove the try/catch below
            var m = MarshalString.CreateMarshaler(runtimeClassId);
            try
            {
                IntPtr instancePtr;
                int hr;
                (instancePtr, hr) = GetActivationFactory(MarshalString.GetAbi(m));
                return (hr == 0 ? ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr) : null, hr);
            }
            finally
            {
                m.Dispose();
            }
        }

        ~WinrtModule()
        {
            Marshal.ThrowExceptionForHR(Platform.CoDecrementMTAUsage(_mtaCookie));
        }
    }

    internal class BaseActivationFactory
    {
        private ObjectReference<IActivationFactoryVftbl> _IActivationFactory;

        public ObjectReference<IActivationFactoryVftbl> Value { get => _IActivationFactory; }

        public I AsInterface<I>() => _IActivationFactory.AsInterface<I>();

        public BaseActivationFactory(string typeNamespace, string typeFullName)
        {
            // Prefer the RoGetActivationFactory HRESULT failure over the LoadLibrary/etc. failure
            int hr;
            (_IActivationFactory, hr) = WinrtModule.GetActivationFactory(typeFullName);
            if (_IActivationFactory != null) { return; }

            var moduleName = typeNamespace;
            while (true)
            {
                DllModule module = null;
                if (DllModule.TryLoad(moduleName + ".dll", out module))
                {
                    (_IActivationFactory, _) = module.GetActivationFactory(typeFullName);
                    if (_IActivationFactory != null) { return; }
                }

                var lastSegment = moduleName.LastIndexOf(".", StringComparison.Ordinal);
                if (lastSegment <= 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
                moduleName = moduleName.Remove(lastSegment);
            }
        }

        public unsafe ObjectReference<I> _ActivateInstance<I>()
        {
            IntPtr instancePtr;
            Marshal.ThrowExceptionForHR(_IActivationFactory.Vftbl.ActivateInstance(_IActivationFactory.ThisPtr, &instancePtr));
            try
            {
                return ComWrappersSupport.GetObjectReferenceForInterface(instancePtr).As<I>();
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(instancePtr);
            }
        }

        public ObjectReference<I> _As<I>() => _IActivationFactory.As<I>();
        public IObjectReference _As(Guid iid) => _IActivationFactory.As<WinRT.Interop.IUnknownVftbl>(iid);
    }

    internal class ActivationFactory<T> : BaseActivationFactory
    {
        public ActivationFactory() : base(typeof(T).Namespace, typeof(T).FullName) { }

        static WeakLazy<ActivationFactory<T>> _factory = new WeakLazy<ActivationFactory<T>>();
        public new static I AsInterface<I>() => _factory.Value.Value.AsInterface<I>();
        public static ObjectReference<I> As<I>() => _factory.Value._As<I>();
        public static IObjectReference As(Guid iid) => _factory.Value._As(iid);
        public static ObjectReference<I> ActivateInstance<I>() => _factory.Value._ActivateInstance<I>();
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

    internal unsafe abstract class EventSource<TDelegate>
        where TDelegate : class, MulticastDelegate
    {
        protected readonly IObjectReference _obj;
        protected readonly int _index;
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> _addHandler;
        readonly delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> _removeHandler;

        // Registration state and delegate cached separately to survive EventSource garbage collection
        // and to prevent the generated event delegate from impacting the lifetime of the
        // event source.
        protected abstract class State : IDisposable
        {
            public EventRegistrationToken token;
            public TDelegate del;
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
                Guid iid = typeof(IReferenceTrackerTargetVftbl).GUID;
                int hr = Marshal.QueryInterface(ptr, ref iid, out referenceTrackerTargetPtr);
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

            public bool HasComReferences()
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
        protected System.WeakReference<State> _state;

        protected EventSource(IObjectReference obj,
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index = 0)
        {
            _obj = obj;
            _addHandler = addHandler;
            _removeHandler = removeHandler;
            _index = index;
            _state = Cache.GetState(obj, index);
        }

        protected abstract IObjectReference CreateMarshaler(TDelegate del);

        protected abstract IntPtr GetAbi(IObjectReference marshaler);

        protected abstract void DisposeMarshaler(IObjectReference marshaler);

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
                        var nativeDelegate = GetAbi(marshaler);
                        state.InitalizeReferenceTracking(nativeDelegate);
                        ExceptionHelpers.ThrowExceptionForHR(_addHandler(_obj.ThisPtr, nativeDelegate, out state.token));
                    }
                    finally
                    {
                        // Dispose our managed reference to the delegate's CCW.
                        // Either the native event holds a reference now or the _addHandler call failed.
                        DisposeMarshaler(marshaler);
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

        void UnsubscribeFromNative(State state)
        {
            ExceptionHelpers.ThrowExceptionForHR(_removeHandler(_obj.ThisPtr, state.token));
            state.Dispose();
            _state = null;
        }

        private class Cache
        {
            Cache(IWeakReference target, int index, System.WeakReference<State> state)
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
                    using var resolved = this.target.Resolve(typeof(IUnknownVftbl).GUID);
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
                    using var resolved = this.target.Resolve(typeof(IUnknownVftbl).GUID);
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
                try
                {
#if NETSTANDARD2_0
                    var weakRefSource = (IWeakReferenceSource)typeof(IWeakReferenceSource).GetHelperType().GetConstructor(new[] { typeof(IObjectReference) }).Invoke(new object[] { obj });
#else
                    var weakRefSource = ((object)new WinRT.IInspectable(obj)) as IWeakReferenceSource;
#endif
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
#if NETSTANDARD2_0
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
    }

    internal unsafe sealed class EventSource__EventHandler<T> : EventSource<System.EventHandler<T>>
    {
        internal EventSource__EventHandler(IObjectReference obj,
            delegate* unmanaged[Stdcall]<System.IntPtr, System.IntPtr, out WinRT.EventRegistrationToken, int> addHandler,
            delegate* unmanaged[Stdcall]<System.IntPtr, WinRT.EventRegistrationToken, int> removeHandler,
            int index) : base(obj, addHandler, removeHandler, index)
        {
        }

        protected override IObjectReference CreateMarshaler(System.EventHandler<T> del) =>
            del is null ? null : ABI.System.EventHandler<T>.CreateMarshaler(del);

        protected override void DisposeMarshaler(IObjectReference marshaler) =>
            ABI.System.EventHandler<T>.DisposeMarshaler(marshaler);

        protected override IntPtr GetAbi(IObjectReference marshaler) =>
            marshaler is null ? IntPtr.Zero : ABI.System.EventHandler<T>.GetAbi(marshaler);

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
                    var localDel = del;
                    if (localDel != null)
                        localDel.Invoke(obj, e);
                };
                return handler;
            }
        }
    }

#pragma warning restore CA2002

    // An event registration token table stores mappings from delegates to event tokens, in order to support
    // sourcing WinRT style events from managed code.
    internal sealed class EventRegistrationTokenTable<T> where T : class, global::System.Delegate
    {
        // Note this dictionary is also used as the synchronization object for this table
        private readonly Dictionary<EventRegistrationToken, T> m_tokens = new Dictionary<EventRegistrationToken, T>();

        public EventRegistrationToken AddEventHandler(T handler)
        {
            // Windows Runtime allows null handlers.  Assign those the default token (token value 0) for simplicity
            if (handler == null)
            {
                return default;
            }

            lock (m_tokens)
            {
                return AddEventHandlerNoLock(handler);
            }
        }

        private EventRegistrationToken AddEventHandlerNoLock(T handler)
        {
            Debug.Assert(handler != null);

            // Get a registration token, making sure that we haven't already used the value.  This should be quite
            // rare, but in the case it does happen, just keep trying until we find one that's unused.
            EventRegistrationToken token = GetPreferredToken(handler);
            while (m_tokens.ContainsKey(token))
            {
                token = new EventRegistrationToken { Value = token.Value + 1 };
            }
            m_tokens[token] = handler;

            return token;
        }

        // Generate a token that may be used for a particular event handler.  We will frequently be called
        // upon to look up a token value given only a delegate to start from.  Therefore, we want to make
        // an initial token value that is easily determined using only the delegate instance itself.  Although
        // in the common case this token value will be used to uniquely identify the handler, it is not
        // the only possible token that can represent the handler.
        //
        // This means that both:
        //  * if there is a handler assigned to the generated initial token value, it is not necessarily
        //    this handler.
        //  * if there is no handler assigned to the generated initial token value, the handler may still
        //    be registered under a different token
        //
        // Effectively the only reasonable thing to do with this value is either to:
        //  1. Use it as a good starting point for generating a token for handler
        //  2. Use it as a guess to quickly see if the handler was really assigned this token value
        private static EventRegistrationToken GetPreferredToken(T handler)
        {
            Debug.Assert(handler != null);

            // We want to generate a token value that has the following properties:
            //  1. is quickly obtained from the handler instance
            //  2. uses bits in the upper 32 bits of the 64 bit value, in order to avoid bugs where code
            //     may assume the value is really just 32 bits
            //  3. uses bits in the bottom 32 bits of the 64 bit value, in order to ensure that code doesn't
            //     take a dependency on them always being 0.
            //
            // The simple algorithm chosen here is to simply assign the upper 32 bits the metadata token of the
            // event handler type, and the lower 32 bits the hash code of the handler instance itself. Using the
            // metadata token for the upper 32 bits gives us at least a small chance of being able to identify a
            // totally corrupted token if we ever come across one in a minidump or other scenario.
            //
            // The hash code of a unicast delegate is not tied to the method being invoked, so in the case
            // of a unicast delegate, the hash code of the target method is used instead of the full delegate
            // hash code.
            //
            // While calculating this initial value will be somewhat more expensive than just using a counter
            // for events that have few registrations, it will also give us a shot at preventing unregistration
            // from becoming an O(N) operation.
            //
            // We should feel free to change this algorithm as other requirements / optimizations become
            // available.  This implementation is sufficiently random that code cannot simply guess the value to
            // take a dependency upon it.  (Simply applying the hash-value algorithm directly won't work in the
            // case of collisions, where we'll use a different token value).

            uint handlerHashCode;
            global::System.Delegate[] invocationList = ((global::System.Delegate)(object)handler).GetInvocationList();
            if (invocationList.Length == 1)
            {
                handlerHashCode = (uint)invocationList[0].Method.GetHashCode();
            }
            else
            {
                handlerHashCode = (uint)handler.GetHashCode();
            }

            ulong tokenValue = ((ulong)(uint)typeof(T).MetadataToken << 32) | handlerHashCode;
            return new EventRegistrationToken { Value = (long)tokenValue };
        }

        // Remove the event handler from the table and
        // Get the delegate associated with an event registration token if it exists
        // If the event registration token is not registered, returns false
        public bool RemoveEventHandler(EventRegistrationToken token, out T handler)
        {
            lock (m_tokens)
            {
                if (m_tokens.TryGetValue(token, out handler))
                {
                    RemoveEventHandlerNoLock(token);
                    return true;
                }
            }

            return false;
        }

        private void RemoveEventHandlerNoLock(EventRegistrationToken token)
        {
            if (m_tokens.TryGetValue(token, out T handler))
            {
                m_tokens.Remove(token);
            }
        }
    }
}


namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class ModuleInitializerAttribute : Attribute { }
}

namespace WinRT
{
    internal static class ProjectionInitializer
    {
#pragma warning disable 0436
        [ModuleInitializer]
#pragma warning restore 0436
        internal static void InitalizeProjection()
        {
            ComWrappersSupport.RegisterProjectionAssembly(typeof(ProjectionInitializer).Assembly);
        }
    }
}
