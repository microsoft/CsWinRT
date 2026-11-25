// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;

namespace UnitTest;

/// <summary>
/// Tests for the native <c>IWeakReference</c> objects that CCWs for managed objects hand out through <c>IWeakReferenceSource</c>.
/// </summary>
[TestClass]
public unsafe class WeakReferenceTests
{
    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_POINTER = unchecked((int)0x80004003);

    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IID_IWeakReference = new("00000037-0000-0000-C000-000000000046");
    private static readonly Guid IID_IWeakReferenceSource = new("00000038-0000-0000-C000-000000000046");
    private static readonly Guid IID_IInspectable = new("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90");

    [TestMethod]
    public void GetWeakReference_ResolvesToSameCcw()
    {
        object target = new();

        void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(target);
        void* weakReference = GetWeakReference(ccw);

        try
        {
            void* identity = QueryInterface(ccw, IID_IUnknown);
            void* resolved = Resolve(weakReference, IID_IUnknown);
            void* resolvedInspectable = Resolve(weakReference, IID_IInspectable);

            Assert.AreEqual((nint)identity, (nint)resolved);
            Assert.AreNotEqual(0, (nint)resolvedInspectable);

            _ = Release(identity);
            _ = Release(resolved);
            _ = Release(resolvedInspectable);
        }
        finally
        {
            _ = Release(weakReference);
            _ = Release(ccw);
        }

        GC.KeepAlive(target);
    }

    [TestMethod]
    public void WeakReference_QueryInterfaceAndReferenceCounting()
    {
        object target = new();

        void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(target);
        void* weakReference = GetWeakReference(ccw);

        try
        {
            // Only 'IUnknown' and 'IWeakReference' are supported, and they both return the same object
            Assert.AreEqual((nint)weakReference, (nint)QueryInterface(weakReference, IID_IUnknown));
            Assert.AreEqual((nint)weakReference, (nint)QueryInterface(weakReference, IID_IWeakReference));

            Guid unknown = Guid.NewGuid();
            void* result = (void*)1;

            Assert.AreEqual(E_NOINTERFACE, ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)weakReference)[0])(weakReference, &unknown, &result));
            Assert.AreEqual(0, (nint)result);

            // The reference count is 3 here: the initial reference, plus one for each successful 'QueryInterface' above
            Assert.AreEqual(2u, Release(weakReference));
            Assert.AreEqual(1u, Release(weakReference));
            Assert.AreEqual(2u, ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)weakReference)[1])(weakReference));
            Assert.AreEqual(1u, Release(weakReference));
        }
        finally
        {
            _ = Release(weakReference);
            _ = Release(ccw);
        }

        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Resolve_NullOutPointer_ReturnsPointerError()
    {
        object target = new();

        void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(target);
        void* weakReference = GetWeakReference(ccw);

        try
        {
            Guid iid = IID_IUnknown;

            Assert.AreEqual(E_POINTER, Resolve(weakReference, &iid, null));
        }
        finally
        {
            _ = Release(weakReference);
            _ = Release(ccw);
        }

        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Resolve_AfterTargetIsCollected_SucceedsWithNullPointer()
    {
        void* weakReference = CreateWeakReferenceToCollectedTarget(out WeakReference weakTarget);

        for (int i = 0; i < 4 && weakTarget.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.IsFalse(weakTarget.IsAlive);

        try
        {
            Guid iid = IID_IUnknown;
            void* result = (void*)1;

            Assert.AreEqual(S_OK, Resolve(weakReference, &iid, &result));
            Assert.AreEqual(0, (nint)result);
        }
        finally
        {
            Assert.AreEqual(0u, Release(weakReference));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void* CreateWeakReferenceToCollectedTarget(out WeakReference weakTarget)
    {
        object target = new();

        weakTarget = new WeakReference(target);

        void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(target);
        void* weakReference = GetWeakReference(ccw);

        // Releasing the last native reference to the CCW means nothing keeps the target alive anymore
        _ = Release(ccw);

        return weakReference;
    }

    private static void* GetWeakReference(void* ccw)
    {
        void* weakReferenceSource = QueryInterface(ccw, IID_IWeakReferenceSource);
        void* weakReference;

        int hresult = ((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)weakReferenceSource)[3])(weakReferenceSource, &weakReference);

        _ = Release(weakReferenceSource);

        Marshal.ThrowExceptionForHR(hresult);

        return weakReference;
    }

    private static void* QueryInterface(void* thisPtr, Guid iid)
    {
        void* result;

        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)thisPtr)[0])(thisPtr, &iid, &result));

        return result;
    }

    private static void* Resolve(void* weakReference, Guid iid)
    {
        void* result;

        Marshal.ThrowExceptionForHR(Resolve(weakReference, &iid, &result));

        return result;
    }

    private static int Resolve(void* weakReference, Guid* iid, void** result)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)weakReference)[3])(weakReference, iid, result);
    }

    private static uint Release(void* thisPtr)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)thisPtr)[2])(thisPtr);
    }
}
