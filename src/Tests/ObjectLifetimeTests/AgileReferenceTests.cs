using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;

namespace ObjectLifetimeTests;

[TestClass]
public sealed unsafe class AgileReferenceTests
{
    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern int CoInitializeEx(nint reserved, uint flags);

    [DllImport("ole32.dll", ExactSpelling = true)]
    private static extern void CoUninitialize();

    [TestMethod]
    public void CrossContextResolutionReleasesNativeReferences()
    {
        AsyncQueue queue = new(
            ((ObjectLifetimeTests.Lifted.App)Application.Current).m_window.DispatcherQueue);
        nint pointer = 0;
        WindowsRuntimeObjectReference reference = null;
        Exception workerError = null;
        using ManualResetEvent completed = new(false);

        try
        {
            queue
                .CallFromUIThread(() =>
                {
                    pointer = (nint)NativeUnknown.Create();
                    reference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                        externalComObject: (void*)pointer,
                        iid: in NativeUnknown.Iid,
                        wrapperFlags: out _);
                    Assert.IsFalse(reference.IsFreeThreaded);
                    Assert.AreEqual(2, NativeUnknown.GetReferenceCount((void*)pointer));

                    Thread worker = new(() =>
                    {
                        int hresult = CoInitializeEx(0, 0);

                        try
                        {
                            Marshal.ThrowExceptionForHR(hresult);
                            reference.AddRefUnsafe();

                            try
                            {
                                nint resolved = (nint)reference.GetThisPtrUnsafe();
                                Assert.AreNotEqual(nint.Zero, resolved);
                                Assert.AreNotEqual(pointer, resolved);
                            }
                            finally
                            {
                                reference.ReleaseUnsafe();
                            }

                            reference.Dispose();
                        }
                        catch (Exception e)
                        {
                            workerError = e;
                        }
                        finally
                        {
                            if (hresult >= 0)
                            {
                                CoUninitialize();
                            }

                            completed.Set();
                        }
                    }) { IsBackground = true };
                    worker.Start();
                })
                .WaitForHandle(
                    handle: completed,
                    timeoutErrorMessage: "Cross-context resolution did not complete",
                    timeout: 15000)
                .CallFromUIThread(() =>
                {
                    Assert.IsNull(workerError);
                    reference = null;
                })
                .CollectUntilTrue(
                    fn: () => NativeUnknown.GetReferenceCount((void*)pointer) == 1,
                    timeoutMessage: "Cross-context resolution retained a native reference after collection")
                .Run();
        }
        finally
        {
            queue.CallFromUIThread(() =>
            {
                reference?.Dispose();
                NativeUnknown.Release((void*)pointer);
            }).Run();
        }
    }

    /// <summary>
    /// Provides a native, reference-counted <c>IUnknown</c> using standard COM apartment marshalling.
    /// </summary>
    /// <remarks>
    /// The object exposes neither <c>IAgileObject</c> nor <c>IMarshal</c>. Its state is allocated outside the managed
    /// heap, so the count measures native ownership rather than managed reachability. The test retains the initial
    /// reference until all observations and queued releases complete.
    /// </remarks>
    private static class NativeUnknown
    {
        public static readonly Guid Iid = new("00000000-0000-0000-C000-000000000046");

        [FixedAddressValueType]
        private static readonly IUnknownVftbl Vtable = new()
        {
            QueryInterface = &QueryInterface,
            AddRef = &AddRef,
            Release = &ReleaseUnmanaged
        };

        private struct Instance
        {
            public IUnknownVftbl* Vtable;
            public int ReferenceCount;
        }

        public static void* Create()
        {
            Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
            instance->Vtable = (IUnknownVftbl*)Unsafe.AsPointer(in Vtable);
            instance->ReferenceCount = 1;
            return instance;
        }

        public static int GetReferenceCount(void* pointer) =>
            Volatile.Read(ref ((Instance*)pointer)->ReferenceCount);

        public static void Release(void* pointer)
        {
            if (pointer is not null)
            {
                _ = ReleaseCore((Instance*)pointer);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int QueryInterface(void* pointer, Guid* iid, void** result)
        {
            *result = null;

            if (*iid != Iid)
            {
                return unchecked((int)0x80004002);
            }

            _ = Interlocked.Increment(ref ((Instance*)pointer)->ReferenceCount);
            *result = pointer;
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint AddRef(void* pointer) =>
            (uint)Interlocked.Increment(ref ((Instance*)pointer)->ReferenceCount);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint ReleaseUnmanaged(void* pointer) => ReleaseCore((Instance*)pointer);

        private static uint ReleaseCore(Instance* instance)
        {
            int count = Interlocked.Decrement(ref instance->ReferenceCount);

            if (count == 0)
            {
                NativeMemory.Free(instance);
            }

            return (uint)count;
        }
    }
}
