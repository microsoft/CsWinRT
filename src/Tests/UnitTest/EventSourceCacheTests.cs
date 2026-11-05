// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestComponentCSharp;
using Windows.Foundation.Collections;
using WindowsRuntime.InteropServices;

namespace UnitTest;

[TestClass]
public unsafe class EventSourceCacheTests
{
    private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
    private const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int E_ACCESSDENIED = unchecked((int)0x80070005);

    // A failed 'Resolve' call (e.g. after an out-of-process server exited) must be treated like a target that
    // is no longer alive: the cache must not return stale states, and must not throw from the lookup.
    [TestMethod]
    [DataRow(RPC_E_DISCONNECTED)]
    [DataRow(RPC_S_SERVER_UNAVAILABLE)]
    [DataRow(E_FAIL)]
    [DataRow(E_ACCESSDENIED)]
    public void GetStateTreatsFailedResolveAsExpired(int resolveHResult)
    {
        void* source = FakeWeakReferenceSource.Create();
        WindowsRuntimeObjectReference objectReference = FakeWeakReferenceSource.CreateObjectReference(source);
        WeakReference<object> state = new(new object());

        try
        {
            EventSourceCacheAccessor.Create(objectReference, 1, state);

            Assert.AreSame(state, EventSourceCacheAccessor.GetState(objectReference, 1));

            FakeWeakReferenceSource.SetResolveHResult(source, resolveHResult);

            Assert.IsNull(EventSourceCacheAccessor.GetState(objectReference, 1));
        }
        finally
        {
            EventSourceCacheAccessor.RemoveState(source, 1, state);

            objectReference.Dispose();

            FakeWeakReferenceSource.Destroy(source);
        }
    }

    // Registering a state for a new object that reuses the native pointer of a previous one must
    // replace the cached target and drop the states of the previous object, rather than failing.
    [TestMethod]
    [DataRow(RPC_E_DISCONNECTED)]
    [DataRow(RPC_S_SERVER_UNAVAILABLE)]
    [DataRow(E_FAIL)]
    [DataRow(E_ACCESSDENIED)]
    public void CreateReplacesTargetAfterFailedResolve(int resolveHResult)
    {
        void* source = FakeWeakReferenceSource.Create();
        WindowsRuntimeObjectReference objectReference = FakeWeakReferenceSource.CreateObjectReference(source);
        WeakReference<object> oldState = new(new object());
        WeakReference<object> newState = new(new object());

        try
        {
            EventSourceCacheAccessor.Create(objectReference, 1, oldState);

            Assert.AreEqual(1, FakeWeakReferenceSource.GetWeakReferenceCount(source));

            FakeWeakReferenceSource.SetResolveHResult(source, resolveHResult);

            EventSourceCacheAccessor.Create(objectReference, 2, newState);

            Assert.AreEqual(2, FakeWeakReferenceSource.GetWeakReferenceCount(source));
            Assert.IsNull(EventSourceCacheAccessor.GetState(objectReference, 1));
            Assert.AreSame(newState, EventSourceCacheAccessor.GetState(objectReference, 2));
        }
        finally
        {
            EventSourceCacheAccessor.RemoveState(source, 1, oldState);
            EventSourceCacheAccessor.RemoveState(source, 2, newState);

            objectReference.Dispose();

            FakeWeakReferenceSource.Destroy(source);
        }
    }

    [TestMethod]
    public void CreatePreservesStatesForLiveTarget()
    {
        void* source = FakeWeakReferenceSource.Create();
        WindowsRuntimeObjectReference objectReference = FakeWeakReferenceSource.CreateObjectReference(source);
        WeakReference<object> firstState = new(new object());
        WeakReference<object> secondState = new(new object());

        try
        {
            EventSourceCacheAccessor.Create(objectReference, 1, firstState);
            EventSourceCacheAccessor.Create(objectReference, 2, secondState);

            Assert.AreEqual(1, FakeWeakReferenceSource.GetWeakReferenceCount(source));
            Assert.AreSame(firstState, EventSourceCacheAccessor.GetState(objectReference, 1));
            Assert.AreSame(secondState, EventSourceCacheAccessor.GetState(objectReference, 2));
        }
        finally
        {
            EventSourceCacheAccessor.RemoveState(source, 1, firstState);
            EventSourceCacheAccessor.RemoveState(source, 2, secondState);

            objectReference.Dispose();

            FakeWeakReferenceSource.Destroy(source);
        }
    }

    [TestMethod]
    public void MultipleEventsRemainUnsubscribableAfterCollection()
    {
        bool intEventCalled = false;
        bool boolEventCalled = false;
        void OnIntPropertyChanged(object sender, int value) => intEventCalled = true;
        void OnBoolPropertyChanged(object sender, bool value) => boolEventCalled = true;

        var classInstance = new Class();
        classInstance.IntPropertyChanged += OnIntPropertyChanged;
        classInstance.BoolPropertyChanged += OnBoolPropertyChanged;
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsTrue(intEventCalled);
        Assert.IsTrue(boolEventCalled);

        intEventCalled = false;
        boolEventCalled = false;

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();

        classInstance.IntPropertyChanged -= OnIntPropertyChanged;
        classInstance.BoolPropertyChanged -= OnBoolPropertyChanged;
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsFalse(intEventCalled);
        Assert.IsFalse(boolEventCalled);
    }

    // Unsubscribing through a new wrapper for the same native object requires the event states to be
    // recovered from the event source cache. Both events share a single cache entry for that object.
    [TestMethod]
    public void MultipleEventsRemainUnsubscribableThroughNewWrapper()
    {
        bool intEventCalled = false;
        bool boolEventCalled = false;
        EventHandler<int> onIntPropertyChanged = (sender, value) => intEventCalled = true;
        EventHandler<bool> onBoolPropertyChanged = (sender, value) => boolEventCalled = true;

        // Keep the native object alive without keeping its managed wrapper alive
        PropertySet holder = [];
        GCHandle originalWrapper = SubscribeAndStore(holder, onIntPropertyChanged, onBoolPropertyChanged);

        try
        {
            Assert.IsTrue(intEventCalled);
            Assert.IsTrue(boolEventCalled);

            intEventCalled = false;
            boolEventCalled = false;

            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true);

            // A weak 'GCHandle' is used rather than 'WeakReference<T>', as the latter can create a new wrapper
            // for a native object that is still alive, if that object implements 'IWeakReferenceSource'.
            Assert.IsNull(originalWrapper.Target, "The original wrapper must be collected to exercise the cache.");
        }
        finally
        {
            originalWrapper.Free();
        }

        Class classInstance = (Class)holder["instance"];

        // The handlers must still be registered after the original wrapper is collected, otherwise
        // the checks below would pass even if the unsubscriptions had never reached the native object.
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsTrue(intEventCalled);
        Assert.IsTrue(boolEventCalled);

        intEventCalled = false;
        boolEventCalled = false;

        classInstance.IntPropertyChanged -= onIntPropertyChanged;
        classInstance.BoolPropertyChanged -= onBoolPropertyChanged;
        classInstance.RaiseIntChanged();
        classInstance.RaiseBoolChanged();

        Assert.IsFalse(intEventCalled);
        Assert.IsFalse(boolEventCalled);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static GCHandle SubscribeAndStore(
            PropertySet holder,
            EventHandler<int> onIntPropertyChanged,
            EventHandler<bool> onBoolPropertyChanged)
        {
            Class classInstance = new();
            classInstance.IntPropertyChanged += onIntPropertyChanged;
            classInstance.BoolPropertyChanged += onBoolPropertyChanged;
            classInstance.RaiseIntChanged();
            classInstance.RaiseBoolChanged();

            holder["instance"] = classInstance;

            return GCHandle.Alloc(classInstance, GCHandleType.Weak);
        }
    }
}

/// <summary>
/// Accessors for the internal <c>EventSourceCache</c> type from <c>WinRT.Runtime</c>.
/// </summary>
file static unsafe class EventSourceCacheAccessor
{
    private const string TypeName = "WindowsRuntime.InteropServices.EventSourceCache, WinRT.Runtime";

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Create")]
    public static extern void Create(
        [UnsafeAccessorType(TypeName)] object _,
        WindowsRuntimeObjectReference objectReference,
        int index,
        WeakReference<object> state);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetState")]
    public static extern WeakReference<object> GetState(
        [UnsafeAccessorType(TypeName)] object _,
        WindowsRuntimeObjectReference objectReference,
        int index);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RemoveState")]
    public static extern void RemoveState(
        [UnsafeAccessorType(TypeName)] object _,
        void* thisPtr,
        int index,
        WeakReference<object> state);

    public static void Create(WindowsRuntimeObjectReference objectReference, int index, WeakReference<object> state)
    {
        Create(null, objectReference, index, state);
    }

    public static WeakReference<object> GetState(WindowsRuntimeObjectReference objectReference, int index)
    {
        return GetState(null, objectReference, index);
    }

    public static void RemoveState(void* thisPtr, int index, WeakReference<object> state)
    {
        RemoveState(null, thisPtr, index, state);
    }
}

/// <summary>
/// A fake native object implementing <c>IWeakReferenceSource</c>, whose weak references can be made to fail on <c>Resolve</c>.
/// </summary>
file static unsafe class FakeWeakReferenceSource
{
    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IID_IAgileObject = new("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90");
    private static readonly Guid IID_IWeakReference = new("00000037-0000-0000-C000-000000000046");
    private static readonly Guid IID_IWeakReferenceSource = new("00000038-0000-0000-C000-000000000046");

    private static readonly void** SourceVftbl = CreateSourceVftbl();
    private static readonly void** WeakReferenceVftbl = CreateWeakReferenceVftbl();

    // The same layout is used for both the source and its weak references, to keep the fake simple
    private struct Instance
    {
        public void** Vftbl;
        public int ReferenceCount;

        // Source only: the weak reference most recently handed out (an owned reference is kept on it)
        public Instance* LastWeakReference;
        public int GetWeakReferenceCount;

        // Weak reference only: the 'Resolve' result, and the source that is returned when it succeeds
        public int ResolveHResult;
        public Instance* Source;
    }

    /// <summary>Creates a new source object, with an initial reference count of 1.</summary>
    public static void* Create()
    {
        Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));

        instance->Vftbl = SourceVftbl;
        instance->ReferenceCount = 1;

        return instance;
    }

    public static WindowsRuntimeObjectReference CreateObjectReference(void* source)
    {
        return CreateUnsafe(null, source, in IID_IUnknown);
    }

    /// <summary>Makes <c>Resolve</c> fail with the given <c>HRESULT</c> for the most recent weak reference.</summary>
    public static void SetResolveHResult(void* source, int hresult)
    {
        ((Instance*)source)->LastWeakReference->ResolveHResult = hresult;
    }

    public static int GetWeakReferenceCount(void* source)
    {
        return ((Instance*)source)->GetWeakReferenceCount;
    }

    /// <summary>Releases the initial reference of a source, and the one it holds on its last weak reference.</summary>
    public static void Destroy(void* source)
    {
        Instance* instance = (Instance*)source;

        if (instance->LastWeakReference is not null)
        {
            _ = ReleaseCore(instance->LastWeakReference);

            instance->LastWeakReference = null;
        }

        _ = ReleaseCore(instance);
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CreateUnsafe")]
    private static extern WindowsRuntimeObjectReference CreateUnsafe(WindowsRuntimeObjectReference _, void* thisPtr, in Guid iid);

    private static void** CreateSourceVftbl()
    {
        void** vftbl = (void**)NativeMemory.Alloc(4, (nuint)sizeof(void*));

        vftbl[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        vftbl[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        vftbl[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&Release;
        vftbl[3] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetWeakReference;

        return vftbl;
    }

    private static void** CreateWeakReferenceVftbl()
    {
        void** vftbl = (void**)NativeMemory.Alloc(4, (nuint)sizeof(void*));

        vftbl[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        vftbl[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        vftbl[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&Release;
        vftbl[3] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&Resolve;

        return vftbl;
    }

    private static uint ReleaseCore(Instance* instance)
    {
        int referenceCount = Interlocked.Decrement(ref instance->ReferenceCount);

        if (referenceCount == 0)
        {
            NativeMemory.Free(instance);
        }

        return (uint)referenceCount;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterface(void* thisPtr, Guid* iid, void** ppvObject)
    {
        Instance* instance = (Instance*)thisPtr;

        // Respond to 'IAgileObject' so the object is treated as free-threaded, independently of the test apartment
        bool supported = *iid == IID_IUnknown || *iid == IID_IAgileObject ||
            (instance->Vftbl == SourceVftbl ? *iid == IID_IWeakReferenceSource : *iid == IID_IWeakReference);

        if (!supported)
        {
            *ppvObject = null;

            return unchecked((int)0x80004002); // E_NOINTERFACE
        }

        _ = Interlocked.Increment(ref instance->ReferenceCount);

        *ppvObject = instance;

        return 0; // S_OK
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* thisPtr)
    {
        return (uint)Interlocked.Increment(ref ((Instance*)thisPtr)->ReferenceCount);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* thisPtr)
    {
        return ReleaseCore((Instance*)thisPtr);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetWeakReference(void* thisPtr, void** weakReference)
    {
        Instance* source = (Instance*)thisPtr;
        Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));

        // One reference is returned to the caller, and one is kept by the source
        instance->Vftbl = WeakReferenceVftbl;
        instance->ReferenceCount = 2;
        instance->Source = source;

        if (source->LastWeakReference is not null)
        {
            _ = ReleaseCore(source->LastWeakReference);
        }

        source->LastWeakReference = instance;
        source->GetWeakReferenceCount++;

        *weakReference = instance;

        return 0; // S_OK
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int Resolve(void* thisPtr, Guid* riid, void** objectReference)
    {
        Instance* instance = (Instance*)thisPtr;

        if (instance->ResolveHResult < 0)
        {
            *objectReference = null;

            return instance->ResolveHResult;
        }

        _ = Interlocked.Increment(ref instance->Source->ReferenceCount);

        *objectReference = instance->Source;

        return 0; // S_OK
    }
}
