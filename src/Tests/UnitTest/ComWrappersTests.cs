// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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

    [TestMethod]
    public void TestLeaseFreeCallThrowsAfterObjectReferenceIsDisposed()
    {
        void* target = FakeInspectable.Create(nested: null);

        try
        {
            object wrapper = WindowsRuntimeMarshal.ConvertToManaged(target);

            Assert.IsTrue(WindowsRuntimeComWrappersMarshal.TryUnwrapObjectReference(
                wrapper,
                out WindowsRuntimeObjectReference objectReference));

            // These conditions are the inputs to the lease-free predicate. The predicate itself is internal
            // to WinRT.Runtime, so together they verify this object exercises the lease-free call path.
            Assert.IsTrue(objectReference.IsFreeThreaded);
            Assert.AreEqual(IntPtr.Zero, (IntPtr)objectReference.GetReferenceTrackerPtrUnsafe());

            objectReference.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() => GetValueForCall(objectReference));

            GC.KeepAlive(wrapper);
        }
        finally
        {
            FakeInspectable.Release(target);
        }

        static void GetValueForCall(WindowsRuntimeObjectReference objectReference)
        {
            using WindowsRuntimeObjectReferenceValue _ = objectReference.AsValueForCall();
        }
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

    /// <summary>The instance state, laid out so that the vtable pointer comes first (as COM requires).</summary>
    private struct Instance
    {
        public void** Vftbl;
        public int ReferenceCount;

        /// <summary>An object to marshal from <c>GetRuntimeClassName</c>, to force a re-entrant marshalling operation.</summary>
        public nint Nested;
    }

    /// <summary>Creates a new instance with a reference count of 1.</summary>
    public static void* Create(void* nested)
    {
        Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));

        instance->Vftbl = Vftbl;
        instance->ReferenceCount = 1;
        instance->Nested = (nint)nested;

        return instance;
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
        // Respond to 'IAgileObject' as well, so that the object is treated as free-threaded. This keeps the
        // marshalling path deterministic, and independent of the COM apartment the test happens to run on.
        if (*iid == IID_IUnknown || *iid == IID_IInspectable || *iid == IID_IAgileObject)
        {
            _ = AddRefCore((Instance*)thisPtr);

            *ppvObject = thisPtr;

            return 0; // S_OK
        }

        *ppvObject = null;

        return unchecked((int)0x80004002); // E_NOINTERFACE
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
