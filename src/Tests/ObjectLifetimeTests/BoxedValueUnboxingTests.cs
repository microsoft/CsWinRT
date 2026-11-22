using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace ObjectLifetimeTests;

[TestClass]
public sealed unsafe class BoxedValueUnboxingTests
{
    [TestMethod]
    public void GenericDelegateUnboxingReleasesNativeReferences()
    {
        NativeInspectable* value = NativeInspectable.Create(runtimeClassName: null);
        NativeInspectable* box = NativeInspectable.CreateDelegateBox(
            "Windows.Foundation.IReference`1<Windows.Foundation.EventHandler`1<Object>>",
            value);

        try
        {
            AssertConvertsTo(box, typeof(EventHandler<object>));

            CollectUntil(() => value->ReferenceCount == 1, () => $"box={box->ReferenceCount}, value={value->ReferenceCount}");
            Assert.AreEqual(1, box->ReferenceCount);
        }
        finally
        {
            NativeInspectable.Release(box);
            NativeInspectable.Release(value);
        }
    }

    [TestMethod]
    public void ArrayUnboxingReleasesNativeReferences()
    {
        NativeInspectable* element = NativeInspectable.Create(runtimeClassName: null);
        NativeInspectable* box = NativeInspectable.CreateArrayBox("Windows.Foundation.IReferenceArray`1<Object>", element, count: 2);

        try
        {
            AssertConvertsTo(box, typeof(object[]));

            CollectUntil(() => element->ReferenceCount == 1, () => $"box={box->ReferenceCount}, element={element->ReferenceCount}");
            Assert.AreEqual(1, box->ReferenceCount);
        }
        finally
        {
            NativeInspectable.Release(box);
            NativeInspectable.Release(element);
        }
    }

    // Keeps the converted object out of the test method frame, so it can be collected
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertConvertsTo(NativeInspectable* box, Type expectedType)
    {
        Assert.IsInstanceOfType(WindowsRuntimeObjectMarshaller.ConvertToManaged(box), expectedType);
    }

    private static void CollectUntil(Func<bool> condition, Func<string> state)
    {
        for (int i = 0; i < 20 && !condition(); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.IsTrue(condition(), $"Unboxing retained a native reference after collection ({state()})");
    }

    /// <summary>
    /// Provides a native, reference-counted <c>IInspectable</c>, optionally acting as an <c>IReference`1</c> or
    /// <c>IReferenceArray`1</c> box whose <c>get_Value</c> returns owned references to another such object.
    /// </summary>
    /// <remarks>
    /// The object answers every <c>QueryInterface</c> except for reference tracking, custom marshalling and weak
    /// references. Its state is allocated outside the managed heap, so the count measures native ownership.
    /// </remarks>
    private struct NativeInspectable
    {
        private static readonly Guid[] UnsupportedIids =
        [
            new("11D3B13A-180E-4789-A8BE-7712882893E6"), // IReferenceTracker
            new("00000003-0000-0000-C000-000000000046"), // IMarshal
            new("00000038-0000-0000-C000-000000000046"), // IWeakReferenceSource
        ];

        private void** _vtable;
        public int ReferenceCount;
        private char* _runtimeClassName;
        private NativeInspectable* _value;
        private uint _count;

        [DllImport("combase.dll", ExactSpelling = true)]
        private static extern int WindowsCreateString(char* sourceString, uint length, void** hstring);

        public static NativeInspectable* Create(string runtimeClassName)
        {
            void** vtable = (void**)NativeMemory.AllocZeroed((nuint)(7 * sizeof(void*)));
            vtable[0] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, Guid*, void**, int>)&QueryInterface;
            vtable[1] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, uint>)&AddRef;
            vtable[2] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, uint>)&ReleaseUnmanaged;
            vtable[3] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, uint*, Guid**, int>)&GetIids;
            vtable[4] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, void**, int>)&GetRuntimeClassName;
            vtable[5] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, int*, int>)&GetTrustLevel;

            NativeInspectable* instance = (NativeInspectable*)NativeMemory.AllocZeroed((nuint)sizeof(NativeInspectable));
            instance->_vtable = vtable;
            instance->ReferenceCount = 1;
            instance->_runtimeClassName = runtimeClassName is null ? null : (char*)Marshal.StringToHGlobalUni(runtimeClassName);
            return instance;
        }

        public static NativeInspectable* CreateDelegateBox(string runtimeClassName, NativeInspectable* value)
        {
            NativeInspectable* box = Create(runtimeClassName);
            box->_vtable[6] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, void**, int>)&GetValue;
            box->_value = value;
            return box;
        }

        public static NativeInspectable* CreateArrayBox(string runtimeClassName, NativeInspectable* element, uint count)
        {
            NativeInspectable* box = Create(runtimeClassName);
            box->_vtable[6] = (delegate* unmanaged[MemberFunction]<NativeInspectable*, uint*, void***, int>)&GetArray;
            box->_value = element;
            box->_count = count;
            return box;
        }

        public static void Release(NativeInspectable* instance)
        {
            if (instance is not null)
            {
                _ = ReleaseCore(instance);
            }
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int QueryInterface(NativeInspectable* instance, Guid* iid, void** result)
        {
            *result = null;

            if (Array.IndexOf(UnsupportedIids, *iid) >= 0)
            {
                return unchecked((int)0x80004002);
            }

            _ = Interlocked.Increment(ref instance->ReferenceCount);
            *result = instance;
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint AddRef(NativeInspectable* instance) =>
            (uint)Interlocked.Increment(ref instance->ReferenceCount);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static uint ReleaseUnmanaged(NativeInspectable* instance) => ReleaseCore(instance);

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetIids(NativeInspectable* instance, uint* count, Guid** iids)
        {
            *count = 0;
            *iids = null;
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetRuntimeClassName(NativeInspectable* instance, void** name)
        {
            *name = null;

            return instance->_runtimeClassName is null
                ? 0
                : WindowsCreateString(instance->_runtimeClassName, (uint)new string(instance->_runtimeClassName).Length, name);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetTrustLevel(NativeInspectable* instance, int* trustLevel)
        {
            *trustLevel = 0;
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetValue(NativeInspectable* instance, void** value)
        {
            _ = Interlocked.Increment(ref instance->_value->ReferenceCount);
            *value = instance->_value;
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetArray(NativeInspectable* instance, uint* count, void*** value)
        {
            void** array = (void**)Marshal.AllocCoTaskMem((int)instance->_count * sizeof(void*));

            for (uint i = 0; i < instance->_count; i++)
            {
                _ = Interlocked.Increment(ref instance->_value->ReferenceCount);
                array[i] = instance->_value;
            }

            *count = instance->_count;
            *value = array;
            return 0;
        }

        private static uint ReleaseCore(NativeInspectable* instance)
        {
            int count = Interlocked.Decrement(ref instance->ReferenceCount);

            if (count == 0)
            {
                Marshal.FreeHGlobal((nint)instance->_runtimeClassName);
                NativeMemory.Free(instance->_vtable);
                NativeMemory.Free(instance);
            }

            return (uint)count;
        }
    }
}
