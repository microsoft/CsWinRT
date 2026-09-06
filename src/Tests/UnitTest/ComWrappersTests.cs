// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace UnitTest;

/// <summary>
/// Tests for the <c>ComWrappers</c> infrastructure backing RCW creation.
/// </summary>
[TestClass]
public unsafe class ComWrappersTests
{
    [TestMethod]
    public void TestConvertToManagedNullReturnsNull()
    {
        Assert.IsNull(WindowsRuntimeMarshal.ConvertToManaged(null));
    }

    [TestMethod]
    public void TestConvertToManagedManagedObjectReturnsOriginal()
    {
        object original = new();
        void* target = WindowsRuntimeMarshal.ConvertToUnmanaged(original);

        try
        {
            Assert.AreSame(original, WindowsRuntimeMarshal.ConvertToManaged(target));
        }
        finally
        {
            WindowsRuntimeMarshal.Free(target);
        }
    }

    [TestMethod]
    public void TestConvertToManagedManagedIUnknownWithoutIInspectableReturnsOriginal()
    {
        object original = new();
        IUnknownOnlyComWrappers comWrappers = new();
        nint target = comWrappers.GetOrCreateComInterfaceForObject(original, CreateComInterfaceFlags.None);
        Guid iid = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");
        nint inspectable = 0;

        try
        {
            Assert.AreEqual(unchecked((int)0x80004002), Marshal.QueryInterface(target, in iid, out inspectable));
            Assert.AreSame(original, WindowsRuntimeMarshal.ConvertToManaged((void*)target));
        }
        finally
        {
            WindowsRuntimeMarshal.Free((void*)inspectable);
            WindowsRuntimeMarshal.Free((void*)target);
            GC.KeepAlive(comWrappers);
        }
    }

    [TestMethod]
    public void TestConvertToManagedNativeIUnknownWithoutIInspectableThrowsArgumentException()
    {
        void* target = FakeInspectable.Create(nested: null, inspectableQueryHResult: unchecked((int)0x80004002));
        void* unknown = FakeInspectable.GetIUnknown(target);

        try
        {
            ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => WindowsRuntimeMarshal.ConvertToManaged(unknown));

            Assert.AreEqual(0, FakeInspectable.GetInvalidInspectableCallCount(target), "An IUnknown-only object must never be used as an IInspectable.");
            Assert.AreEqual("value", exception.ParamName);
            StringAssert.Contains(exception.Message, "IInspectable");
            Assert.AreEqual(1, FakeInspectable.GetInspectableQueryCount(target));
            Assert.AreEqual(1, FakeInspectable.GetReferenceCount(target));
        }
        finally
        {
            FakeInspectable.Release(target);
        }
    }

    [TestMethod]
    public void TestConvertToManagedPropagatesQueryInterfaceFailure()
    {
        void* target = FakeInspectable.Create(nested: null, inspectableQueryHResult: unchecked((int)0x80070005));

        try
        {
            UnauthorizedAccessException exception = Assert.ThrowsExactly<UnauthorizedAccessException>(() => WindowsRuntimeMarshal.ConvertToManaged(target));

            Assert.AreEqual(unchecked((int)0x80070005), exception.HResult);
            Assert.AreEqual(1, FakeInspectable.GetReferenceCount(target));
        }
        finally
        {
            FakeInspectable.Release(target);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void TestConvertToManagedNativeObjectPreservesIdentityAndReferences(bool useIUnknown)
    {
        void* target = FakeInspectable.Create(nested: null);
        void* unknown = FakeInspectable.GetIUnknown(target);
        void* input = useIUnknown ? unknown : target;

        try
        {
            Assert.AreNotEqual((nint)target, (nint)unknown);

            object wrapper = WindowsRuntimeMarshal.ConvertToManaged(input);

            Assert.IsNotNull(wrapper);
            Assert.AreEqual(0, FakeInspectable.GetInvalidInspectableCallCount(target));

            int referenceCount = FakeInspectable.GetReferenceCount(target);
            int queryCount = FakeInspectable.GetInspectableQueryCount(target);

            // Cache hits still validate public inputs, but must release the temporary QI reference
            Assert.AreSame(wrapper, WindowsRuntimeMarshal.ConvertToManaged(unknown));
            Assert.AreSame(wrapper, WindowsRuntimeMarshal.ConvertToManaged(target));
            Assert.AreEqual(queryCount + 2, FakeInspectable.GetInspectableQueryCount(target));
            Assert.AreEqual(referenceCount, FakeInspectable.GetReferenceCount(target));

            void* roundTrip = WindowsRuntimeMarshal.ConvertToUnmanaged(wrapper);

            try
            {
                Assert.AreEqual((nint)target, (nint)roundTrip);
            }
            finally
            {
                WindowsRuntimeMarshal.Free(roundTrip);
            }

            Assert.AreEqual(referenceCount, FakeInspectable.GetReferenceCount(target));
            GC.KeepAlive(wrapper);
        }
        finally
        {
            FakeInspectable.Release(target);
        }
    }

    [TestMethod]
    public void TestConvertToManagedReleasesInspectableWhenWrapperCreationThrows()
    {
        // Let the public QI succeed, then fail the QI performed while constructing the object reference
        void* target = FakeInspectable.Create(
            nested: null,
            inspectableQueryHResult: unchecked((int)0x80004002),
            successfulInspectableQueriesBeforeFailure: 1);

        try
        {
            Assert.ThrowsExactly<InvalidCastException>(() => WindowsRuntimeMarshal.ConvertToManaged(target));
            Assert.AreEqual(2, FakeInspectable.GetInspectableQueryCount(target));
            Assert.AreEqual(1, FakeInspectable.GetReferenceCount(target));
        }
        finally
        {
            FakeInspectable.Release(target);
        }
    }

    [TestMethod]
    public void TestInternalConvertToManagedDoesNotValidateInspectable()
    {
        void* target = FakeInspectable.Create(nested: null);

        try
        {
            object wrapper = WindowsRuntimeObjectMarshaller.ConvertToManaged(target);

            Assert.IsNotNull(wrapper);

            int referenceCount = FakeInspectable.GetReferenceCount(target);
            int queryCount = FakeInspectable.GetInspectableQueryCount(target);

            Assert.AreSame(wrapper, WindowsRuntimeObjectMarshaller.ConvertToManaged(target));
            Assert.AreEqual(queryCount, FakeInspectable.GetInspectableQueryCount(target));
            Assert.AreEqual(referenceCount, FakeInspectable.GetReferenceCount(target));
            GC.KeepAlive(wrapper);
        }
        finally
        {
            FakeInspectable.Release(target);
        }
    }

    // Marshalling a native object to managed goes through 'WindowsRuntimeComWrappers.CreateObject', which is
    // handed an interface pointer that the caller owns. Some marshallers additionally marshal nested objects
    // while they run, which re-enters the marshalling infrastructure on the same thread.
    //
    // 'NotifyCollectionChangedEventArgs' is the shipping example: 'CreateObject' resolves its runtime class
    // name to its marshaller, whose 'ConvertToManaged' marshals the 'NewItems'/'OldItems' collections through
    // 'IListMarshaller.ConvertToManaged'. That nested operation used to reset the thread-static state that the
    // outer 'CreateObject' read in its 'finally' block to decide whether it had acquired the interface pointer
    // itself. It wrongly concluded that it had, and released a pointer owned by the caller.
    //
    // That type needs the WinUI runtime to activate, which is not available in this test host, so this test
    // reproduces the same re-entrancy with a minimal native object whose 'GetRuntimeClassName' marshals a
    // second native object (that callback runs inside the outer 'CreateObject', exactly like the nested
    // marshalling above). Rather than hard-coding how many references the marshalling infrastructure takes
    // internally, the test compares a re-entrant marshalling operation against a non-re-entrant one: both must
    // have the same net effect on the reference count of the pointer owned by the caller.
    [TestMethod]
    public void TestReentrantMarshallingDoesNotReleaseCallerOwnedInterfacePointer()
    {
        int deltaWithoutReentrancy = MarshalAndGetReferenceCountDelta(reentrant: false);
        int deltaWithReentrancy = MarshalAndGetReferenceCountDelta(reentrant: true);

        // Sanity check that the marshalling actually took a reference on the object we passed in
        Assert.IsTrue(deltaWithoutReentrancy > 0, "Marshalling a native object should take a reference on it.");

        Assert.AreEqual(
            deltaWithoutReentrancy,
            deltaWithReentrancy,
            "Marshalling a native object that re-enters the marshalling infrastructure should have the same effect " +
            "on the reference count of the caller-owned interface pointer as one that does not. A smaller delta means " +
            "the caller-owned pointer was released by 'CreateObject', which does not own it.");

        static int MarshalAndGetReferenceCountDelta(bool reentrant)
        {
            void* nested = reentrant ? FakeInspectable.Create(nested: null) : null;
            void* target = FakeInspectable.Create(nested);

            try
            {
                int referenceCountBefore = FakeInspectable.GetReferenceCount(target);

                object wrapper = WindowsRuntimeMarshal.ConvertToManaged(target);

                int referenceCountAfter = FakeInspectable.GetReferenceCount(target);

                Assert.IsNotNull(wrapper, "Marshalling a native object should always produce a managed wrapper.");

                // The wrapper owns a reference to 'target', so it has to stay alive until after the measurement
                GC.KeepAlive(wrapper);

                return referenceCountAfter - referenceCountBefore;
            }
            finally
            {
                FakeInspectable.Release(target);

                if (nested is not null)
                {
                    FakeInspectable.Release(nested);
                }
            }
        }
    }
}

file sealed unsafe class IUnknownOnlyComWrappers : ComWrappers
{
    protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
    {
        count = 0;

        return null;
    }

    protected override object CreateObject(nint externalComObject, CreateObjectFlags flags)
    {
        throw new NotSupportedException();
    }

    protected override void ReleaseObjects(IEnumerable objects)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// A minimal native <c>IInspectable</c> object with a hand-written vtable, used to observe reference counts
/// and to control what happens while <c>ComWrappers</c> is creating a wrapper for it.
/// </summary>
file static unsafe class FakeInspectable
{
    /// <summary>The runtime class name reported by these objects (it intentionally matches no projected type).</summary>
    private const string RuntimeClassName = "UnitTest.FakeInspectable";

    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IID_IInspectable = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");
    private static readonly Guid IID_IAgileObject = new("94EA2B94-E9CC-49E0-C0FF-EE64CA8F5B90");

    /// <summary>The shared vtable for all instances (allocated once, never freed).</summary>
    private static readonly void** Vftbl = CreateVftbl();
    private static readonly void** UnknownVftbl = CreateUnknownVftbl();

    private struct UnknownInterface
    {
        public void** Vftbl;
        public Instance* Owner;
    }

    /// <summary>The instance state, laid out so that the vtable pointer comes first (as COM requires).</summary>
    private struct Instance
    {
        public void** Vftbl;
        public UnknownInterface Unknown;
        public int ReferenceCount;
        public int InspectableQueryHResult;
        public int SuccessfulInspectableQueriesBeforeFailure;
        public int InspectableQueryCount;
        public int InvalidInspectableCallCount;

        /// <summary>An object to marshal from <c>GetRuntimeClassName</c>, to force a re-entrant marshalling operation.</summary>
        public nint Nested;
    }

    /// <summary>Creates a new instance with a reference count of 1.</summary>
    public static void* Create(void* nested, int inspectableQueryHResult = 0, int successfulInspectableQueriesBeforeFailure = 0)
    {
        Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));

        instance->Vftbl = Vftbl;
        instance->Unknown.Vftbl = UnknownVftbl;
        instance->Unknown.Owner = instance;
        instance->ReferenceCount = 1;
        instance->InspectableQueryHResult = inspectableQueryHResult;
        instance->SuccessfulInspectableQueriesBeforeFailure = successfulInspectableQueriesBeforeFailure;
        instance->Nested = (nint)nested;

        return instance;
    }

    public static void* GetIUnknown(void* thisPtr)
    {
        return &((Instance*)thisPtr)->Unknown;
    }

    public static int GetInspectableQueryCount(void* thisPtr)
    {
        return Volatile.Read(ref ((Instance*)thisPtr)->InspectableQueryCount);
    }

    public static int GetInvalidInspectableCallCount(void* thisPtr)
    {
        return Volatile.Read(ref ((Instance*)thisPtr)->InvalidInspectableCallCount);
    }

    /// <summary>Gets the current reference count of a given instance.</summary>
    public static int GetReferenceCount(void* thisPtr)
    {
        return Volatile.Read(ref ((Instance*)thisPtr)->ReferenceCount);
    }

    /// <summary>Releases a reference, freeing the instance when the count reaches 0.</summary>
    public static void Release(void* thisPtr)
    {
        _ = ReleaseCore((Instance*)thisPtr);
    }

    private static void** CreateVftbl()
    {
        void** vftbl = (void**)NativeMemory.Alloc(6, (nuint)sizeof(void*));

        vftbl[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        vftbl[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        vftbl[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&ReleaseUnmanaged;
        vftbl[3] = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>)&GetIids;
        vftbl[4] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetRuntimeClassName;
        vftbl[5] = (delegate* unmanaged[MemberFunction]<void*, int*, int>)&GetTrustLevel;

        return vftbl;
    }

    private static void** CreateUnknownVftbl()
    {
        // Pad the IUnknown vtable with a trap so a regression fails an assertion rather than crashing
        // the test host by calling past the three valid slots. This does not expose IInspectable.
        void** vftbl = (void**)NativeMemory.AllocZeroed(6, (nuint)sizeof(void*));

        vftbl[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterfaceUnknown;
        vftbl[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRefUnknown;
        vftbl[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&ReleaseUnknown;
        vftbl[4] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&InvalidGetRuntimeClassName;

        return vftbl;
    }

    private static uint AddRefCore(Instance* instance)
    {
        return (uint)Interlocked.Increment(ref instance->ReferenceCount);
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
        return QueryInterfaceCore((Instance*)thisPtr, iid, ppvObject);
    }

    private static int QueryInterfaceCore(Instance* instance, Guid* iid, void** ppvObject)
    {
        *ppvObject = null;

        // Respond to 'IAgileObject' as well, so that the object is treated as free-threaded. This keeps the
        // marshalling path deterministic, and independent of the COM apartment the test happens to run on.
        if (*iid == IID_IUnknown || *iid == IID_IAgileObject)
        {
            _ = AddRefCore(instance);

            *ppvObject = &instance->Unknown;

            return 0; // S_OK
        }

        if (*iid == IID_IInspectable)
        {
            int queryCount = Interlocked.Increment(ref instance->InspectableQueryCount);

            if (instance->InspectableQueryHResult < 0 && queryCount > instance->SuccessfulInspectableQueriesBeforeFailure)
            {
                return instance->InspectableQueryHResult;
            }

            _ = AddRefCore(instance);

            *ppvObject = instance;

            return 0; // S_OK
        }

        return unchecked((int)0x80004002); // E_NOINTERFACE
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterfaceUnknown(void* thisPtr, Guid* iid, void** ppvObject)
    {
        return QueryInterfaceCore(((UnknownInterface*)thisPtr)->Owner, iid, ppvObject);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRefUnknown(void* thisPtr)
    {
        return AddRefCore(((UnknownInterface*)thisPtr)->Owner);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint ReleaseUnknown(void* thisPtr)
    {
        return ReleaseCore(((UnknownInterface*)thisPtr)->Owner);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int InvalidGetRuntimeClassName(void* thisPtr, void** className)
    {
        _ = Interlocked.Increment(ref ((UnknownInterface*)thisPtr)->Owner->InvalidInspectableCallCount);
        *className = null;

        return unchecked((int)0x80004001); // E_NOTIMPL
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* thisPtr)
    {
        return AddRefCore((Instance*)thisPtr);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint ReleaseUnmanaged(void* thisPtr)
    {
        return ReleaseCore((Instance*)thisPtr);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetIids(void* thisPtr, uint* iidCount, Guid** iids)
    {
        *iidCount = 0;
        *iids = null;

        return 0; // S_OK
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetRuntimeClassName(void* thisPtr, void** className)
    {
        Instance* instance = (Instance*)thisPtr;

        // This runs inside the outer 'CreateObject' call, so marshalling another native object here re-enters
        // the marshalling infrastructure exactly like a marshaller that also marshals nested objects would.
        // Only do it once, so that the nested marshalling operation does not recurse indefinitely.
        void* nested = (void*)Interlocked.Exchange(ref instance->Nested, 0);

        if (nested is not null)
        {
            _ = WindowsRuntimeMarshal.ConvertToManaged(nested);
        }

        *className = HStringMarshaller.ConvertToUnmanaged(RuntimeClassName.AsSpan());

        return 0; // S_OK
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTrustLevel(void* thisPtr, int* trustLevel)
    {
        *trustLevel = 0; // BaseTrust

        return 0; // S_OK
    }
}
