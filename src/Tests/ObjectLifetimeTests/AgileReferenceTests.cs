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
        AsyncQueue queue = new(((ObjectLifetimeTests.Lifted.App)Application.Current).m_window.DispatcherQueue);
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
                        (void*)pointer, in NativeUnknown.Iid, out _);
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
                .WaitForHandle(completed, "Cross-context resolution did not complete", 15000)
                .CallFromUIThread(() =>
                {
                    Assert.IsNull(workerError);
                    reference = null;
                })
                .CollectUntilTrue(
                    () => NativeUnknown.GetReferenceCount((void*)pointer) == 1,
                    "Cross-context resolution retained a native reference after collection")
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

    private static class NativeUnknown
    {
        public static readonly Guid Iid = new("00000000-0000-0000-C000-000000000046");
        private static readonly void** Vtable = CreateVtable();

        private struct Instance
        {
            public void** Vtable;
            public int ReferenceCount;
        }

        public static void* Create()
        {
            Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
            instance->Vtable = Vtable;
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

        private static void** CreateVtable()
        {
            void** vtable = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(NativeUnknown), 3 * sizeof(void*));
            vtable[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
            vtable[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
            vtable[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&ReleaseUnmanaged;
            return vtable;
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
