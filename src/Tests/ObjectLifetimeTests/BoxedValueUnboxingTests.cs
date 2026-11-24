using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace ObjectLifetimeTests;

[TestClass]
public sealed unsafe class BoxedValueUnboxingTests
{
    [TestMethod]
    public void GenericDelegateUnboxingReleasesNativeReferences()
    {
        void* value = NativeInspectable.Create(runtimeClassName: null);
        void* box = NativeInspectable.CreateReference(
            runtimeClassName: "Windows.Foundation.IReference`1<Windows.Foundation.EventHandler`1<Object>>",
            value: value);

        try
        {
            AssertConvertsTo(box, typeof(EventHandler<object>));

            new AsyncQueue(((ObjectLifetimeTests.Lifted.App)Application.Current).m_window.DispatcherQueue)
                .CollectUntilTrue(
                    fn: () => NativeInspectable.GetReferenceCount(value) == 1,
                    timeoutMessage: "Delegate unboxing retained a native reference after collection")
                .Run();

            Assert.AreEqual(1, NativeInspectable.GetReferenceCount(box));
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
        void* element = NativeInspectable.Create(runtimeClassName: null);
        void* box = NativeInspectable.CreateReferenceArray(
            runtimeClassName: "Windows.Foundation.IReferenceArray`1<Object>",
            element: element,
            count: 2);

        try
        {
            AssertConvertsTo(box, typeof(object[]));

            new AsyncQueue(((ObjectLifetimeTests.Lifted.App)Application.Current).m_window.DispatcherQueue)
                .CollectUntilTrue(
                    fn: () => NativeInspectable.GetReferenceCount(element) == 1,
                    timeoutMessage: "Array unboxing retained a native reference after collection")
                .Run();

            Assert.AreEqual(1, NativeInspectable.GetReferenceCount(box));
        }
        finally
        {
            NativeInspectable.Release(box);
            NativeInspectable.Release(element);
        }
    }

    // Keeps the converted object out of the test method frame, so it can be collected
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertConvertsTo(void* box, Type expectedType)
    {
        Assert.IsInstanceOfType(WindowsRuntimeObjectMarshaller.ConvertToManaged(box), expectedType);
    }

    /// <summary>
    /// Provides a native, reference-counted <c>IInspectable</c>, optionally acting as an <c>IReference`1</c> or
    /// <c>IReferenceArray`1</c> box whose <c>get_Value</c> returns owned references to another such object.
    /// </summary>
    /// <remarks>
    /// The object answers every <c>QueryInterface</c> except for reference tracking, custom marshalling and weak
    /// references. Its state is allocated outside the managed heap, so the count measures native ownership.
    /// </remarks>
    private static class NativeInspectable
    {
        private static readonly Guid[] UnsupportedIids =
        [
            new("11D3B13A-180E-4789-A8BE-7712882893E6"), // IReferenceTracker
            new("00000003-0000-0000-C000-000000000046"), // IMarshal
            new("00000038-0000-0000-C000-000000000046"), // IWeakReferenceSource
        ];

        [FixedAddressValueType]
        private static readonly IInspectableVftbl InspectableVtable = new()
        {
            QueryInterface = &QueryInterface,
            AddRef = &AddRef,
            Release = &ReleaseUnmanaged,
            GetIids = &GetIids,
            GetRuntimeClassName = &GetRuntimeClassName,
            GetTrustLevel = &GetTrustLevel
        };

        [FixedAddressValueType]
        private static readonly IReferenceVftbl ReferenceVtable = new()
        {
            QueryInterface = &QueryInterface,
            AddRef = &AddRef,
            Release = &ReleaseUnmanaged,
            GetIids = &GetIids,
            GetRuntimeClassName = &GetRuntimeClassName,
            GetTrustLevel = &GetTrustLevel,
            get_Value = &GetValue
        };

        [FixedAddressValueType]
        private static readonly ReferenceArrayVftbl ReferenceArrayVtable = new()
        {
            QueryInterface = &QueryInterface,
            AddRef = &AddRef,
            Release = &ReleaseUnmanaged,
            GetIids = &GetIids,
            GetRuntimeClassName = &GetRuntimeClassName,
            GetTrustLevel = &GetTrustLevel,
            get_Value = &GetArray
        };

        /// <summary>
        /// Binding type for the <c>IReferenceArray`1</c> vtable, for arrays of interface pointers.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct ReferenceArrayVftbl
        {
            public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
            public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
            public delegate* unmanaged[MemberFunction]<void*, uint> Release;
            public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int> GetIids;
            public delegate* unmanaged[MemberFunction]<void*, void**, int> GetRuntimeClassName;
            public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, int> GetTrustLevel;
            public delegate* unmanaged[MemberFunction]<void*, uint*, void***, int> get_Value;
        }

        private struct Instance
        {
            public void* Vtable;
            public int ReferenceCount;
            public char* RuntimeClassName;
            public int RuntimeClassNameLength;
            public Instance* Value;
            public uint Count;
        }

        [DllImport("combase.dll", ExactSpelling = true)]
        private static extern int WindowsCreateString(char* sourceString, uint length, void** hstring);

        public static void* Create(string runtimeClassName)
        {
            return Create(runtimeClassName, Unsafe.AsPointer(in InspectableVtable));
        }

        public static void* CreateReference(string runtimeClassName, void* value)
        {
            Instance* box = Create(runtimeClassName, Unsafe.AsPointer(in ReferenceVtable));
            box->Value = (Instance*)value;
            return box;
        }

        public static void* CreateReferenceArray(string runtimeClassName, void* element, uint count)
        {
            Instance* box = Create(runtimeClassName, Unsafe.AsPointer(in ReferenceArrayVtable));
            box->Value = (Instance*)element;
            box->Count = count;
            return box;
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

        private static Instance* Create(string runtimeClassName, void* vtable)
        {
            Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
            instance->Vtable = vtable;
            instance->ReferenceCount = 1;

            if (runtimeClassName is not null)
            {
                instance->RuntimeClassName = (char*)Marshal.StringToHGlobalUni(runtimeClassName);
                instance->RuntimeClassNameLength = runtimeClassName.Length;
            }

            return instance;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int QueryInterface(void* pointer, Guid* iid, void** result)
        {
            *result = null;

            if (Array.IndexOf(UnsupportedIids, *iid) >= 0)
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

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetIids(void* pointer, uint* count, Guid** iids)
        {
            *count = 0;
            *iids = null;
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetRuntimeClassName(void* pointer, void** name)
        {
            Instance* instance = (Instance*)pointer;

            *name = null;

            return instance->RuntimeClassName is null
                ? 0
                : WindowsCreateString(instance->RuntimeClassName, (uint)instance->RuntimeClassNameLength, name);
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetTrustLevel(void* pointer, TrustLevel* trustLevel)
        {
            *trustLevel = TrustLevel.BaseTrust;
            return 0;
        }

        // Returns an owned reference, as required for 'get_Value' outputs
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetValue(void* pointer, void* value)
        {
            Instance* instance = (Instance*)pointer;

            _ = Interlocked.Increment(ref instance->Value->ReferenceCount);
            *(void**)value = instance->Value;
            return 0;
        }

        // Returns an owned buffer of owned element references, as required for 'get_Value' outputs
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
        private static int GetArray(void* pointer, uint* count, void*** value)
        {
            Instance* instance = (Instance*)pointer;
            void** array = (void**)Marshal.AllocCoTaskMem((int)instance->Count * sizeof(void*));

            for (uint i = 0; i < instance->Count; i++)
            {
                _ = Interlocked.Increment(ref instance->Value->ReferenceCount);
                array[i] = instance->Value;
            }

            *count = instance->Count;
            *value = array;
            return 0;
        }

        private static uint ReleaseCore(Instance* instance)
        {
            int count = Interlocked.Decrement(ref instance->ReferenceCount);

            if (count == 0)
            {
                Marshal.FreeHGlobal((nint)instance->RuntimeClassName);
                NativeMemory.Free(instance);
            }

            return (uint)count;
        }
    }
}
