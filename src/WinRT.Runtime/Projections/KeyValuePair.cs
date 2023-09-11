// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{
    [Guid("02B51929-C1C4-4A7E-8940-0312B5C18500")]
    interface IKeyValuePair<K, V>
    {
        K Key { get; }
        V Value { get; }
    }
}

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::WinRT.Interop;

#if EMBED
    internal
#else
    public
#endif
    static class KeyValuePairMethods<K, V>
    {
        // These function pointers will be set by IKeyValuePairMethods<K, KAbi, V, VAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal unsafe static delegate*<IntPtr, K> _GetKey;
        internal unsafe static delegate*<IntPtr, V> _GetValue;

        private static IntPtr abiToProjectionVftablePtr;
        internal static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
        }

        public static K Abi_get_Key_0(IntPtr thisPtr)
        {
            return KeyValuePair<K, V>.FindAdapter(thisPtr).Key;
        }

        public static V Abi_get_Value_1(IntPtr thisPtr)
        {
            return KeyValuePair<K, V>.FindAdapter(thisPtr).Value;
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class KeyValuePairMethods<K, KAbi, V, VAbi> where KAbi : unmanaged where VAbi : unmanaged
    {
        private static bool RcwHelperInitialized { get; } = InitRcwHelper();

        private unsafe static bool InitRcwHelper()
        {
            KeyValuePairMethods<K, V>._GetKey = &get_Key;
            KeyValuePairMethods<K, V>._GetValue = &get_Value;

#if NET
            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.KeyValuePair<K, V>),
                KeyValuePair<K, V>.CreateRcw);
#endif
            return true;
        }

        public static bool EnsureRcwHelperInitialized()
        {
            return RcwHelperInitialized;
        }

        private static unsafe K get_Key(IntPtr ptr)
        {
            KAbi keyAbi = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)ptr)[6](ptr, &keyAbi));
                return Marshaler<K>.FromAbi(keyAbi);
            }
            finally
            {
                Marshaler<K>.DisposeAbi(keyAbi);
            }
        }

        private static unsafe V get_Value(IntPtr ptr)
        {
            VAbi valueAbi = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)ptr)[7](ptr, &valueAbi));
                return Marshaler<V>.FromAbi(valueAbi);
            }
            finally
            {
                Marshaler<V>.DisposeAbi(valueAbi);
            }
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, KAbi*, int> getKey,
            delegate* unmanaged[Stdcall]<IntPtr, VAbi*, int> getValue)
        {
            if (KeyValuePairMethods<K, V>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

#if NET
            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 2));
#else
            var abiToProjectionVftablePtr = (IntPtr)Marshal.AllocCoTaskMem((sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 2));
#endif
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, KAbi*, int>*)abiToProjectionVftablePtr)[6] = getKey;
            ((delegate* unmanaged[Stdcall]<IntPtr, VAbi*, int>*)abiToProjectionVftablePtr)[7] = getValue;

            if (!KeyValuePairMethods<K, V>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
#if NET
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
#else
                Marshal.FreeCoTaskMem(abiToProjectionVftablePtr);
#endif
                return false;
            }

            return true;
        }

        private static global::System.Delegate[] DelegateCache;

        internal static unsafe void InitFallbackCCWVtable()
        {
            Type get_Key_0_Type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(KAbi*), typeof(int) });
            Type get_Value_1_Type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(VAbi*), typeof(int) });

            DelegateCache = new global::System.Delegate[]
            {
                global::System.Delegate.CreateDelegate(get_Key_0_Type, typeof(KeyValuePairMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_get_Key_0), BindingFlags.NonPublic | BindingFlags.Static)),
                global::System.Delegate.CreateDelegate(get_Value_1_Type, typeof(KeyValuePairMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_get_Value_1), BindingFlags.NonPublic | BindingFlags.Static)),
            };

#if NET
            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 2));
#else
            var abiToProjectionVftablePtr = (IntPtr)Marshal.AllocCoTaskMem((sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 2));
#endif
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((IntPtr*)abiToProjectionVftablePtr)[6] = Marshal.GetFunctionPointerForDelegate(DelegateCache[0]);
            ((IntPtr*)abiToProjectionVftablePtr)[7] = Marshal.GetFunctionPointerForDelegate(DelegateCache[1]);

            if (!KeyValuePairMethods<K, V>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
#if NET
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
#else
                Marshal.FreeCoTaskMem(abiToProjectionVftablePtr);
#endif
            }
        }

        private static unsafe int Do_Abi_get_Key_0(IntPtr thisPtr, KAbi* __return_value__)
        {
            K ____return_value__ = default;
            *__return_value__ = default;
            try
            {
                ____return_value__ = KeyValuePair<K, V>.FindAdapter(thisPtr).Key;
                *__return_value__ = (KAbi)Marshaler<K>.FromManaged(____return_value__);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_Value_1(IntPtr thisPtr, VAbi* __return_value__)
        {
            V ____return_value__ = default;
            *__return_value__ = default;
            try
            {
                ____return_value__ = KeyValuePair<K,V>.FindAdapter(thisPtr).Value;
                *__return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    [Guid("02B51929-C1C4-4A7E-8940-0312B5C18500")]
#if EMBED
    internal
#else
    public
#endif
    class KeyValuePair<K, V> : global::Windows.Foundation.Collections.IKeyValuePair<K, V>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.KeyValuePair<K, V> obj) =>
            MarshalInterface<global::System.Collections.Generic.KeyValuePair<K, V>>.CreateMarshaler(obj);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.KeyValuePair<K, V> obj) => 
            MarshalInterface<global::System.Collections.Generic.KeyValuePair<K, V>>.CreateMarshaler2(obj);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static object CreateRcw(IInspectable obj)
        {
            var pair = new KeyValuePair<K, V>(obj.ObjRef);
            return (object)new global::System.Collections.Generic.KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        public static global::System.Collections.Generic.KeyValuePair<K, V> FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return default;
            }
            var pair = new KeyValuePair<K, V>(_FromAbi(thisPtr));
            return new global::System.Collections.Generic.KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        public static IntPtr FromManaged(global::System.Collections.Generic.KeyValuePair<K, V> obj) => 
            CreateMarshaler2(obj).Detach();

        internal static unsafe void CopyManaged(global::System.Collections.Generic.KeyValuePair<K, V> o, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o).Detach();
        }

        internal static MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.MarshalerArray CreateMarshalerArray(global::System.Collections.Generic.KeyValuePair<K, V>[] array) =>
            MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));

        internal static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.GetAbiArray(box);

        internal static global::System.Collections.Generic.KeyValuePair<K, V>[] FromAbiArray(object box) =>
            MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.FromAbiArray(box, (o) => FromAbi(o));

        public static (int length, IntPtr data) FromManagedArray(global::System.Collections.Generic.KeyValuePair<K, V>[] array) =>
            MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.FromManagedArray(array, (o) => FromManaged(o));

        internal static unsafe void CopyManagedArray(global::System.Collections.Generic.KeyValuePair<K, V>[] array, IntPtr data) =>
            MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.CopyManagedArray(array, data, (o, dest) => CopyManaged(o, dest));

        public static void DisposeMarshaler(IObjectReference value) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(KeyValuePair<K, V>));

        internal sealed class ToIKeyValuePair : global::Windows.Foundation.Collections.IKeyValuePair<K, V>
        {
            private readonly global::System.Collections.Generic.KeyValuePair<K, V> _pair;

            public ToIKeyValuePair([In] ref global::System.Collections.Generic.KeyValuePair<K, V> pair) => _pair = pair;

            public K Key => _pair.Key;

            public V Value => _pair.Value;
        }

        internal struct Enumerator : global::System.Collections.Generic.IEnumerator<global::Windows.Foundation.Collections.IKeyValuePair<K, V>>
        {
            private readonly global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> _enum;

            internal Enumerator(global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<K, V>> enumerator) => _enum = enumerator;

            public bool MoveNext() => _enum.MoveNext();

            public global::Windows.Foundation.Collections.IKeyValuePair<K, V> Current
            {
                get
                {
                    var current = _enum.Current;
                    return new ToIKeyValuePair(ref current);
                }
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => _enum.Reset();

            public void Dispose() { }
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static KeyValuePair()
        {
            if (KeyValuePairMethods<K, V>.AbiToProjectionVftablePtr == default)
            {
                // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                var initFallbackCCWVtable = (Action)typeof(KeyValuePairMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                    GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                    CreateDelegate(typeof(Action));
                initFallbackCCWVtable();
            }

            AbiToProjectionVftablePtr = KeyValuePairMethods<K, V>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("02B51929-C1C4-4A7E-8940-0312B5C18500")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.KeyValuePair<K, V>.AbiToProjectionVftablePtr;

            public static Guid PIID = ABI.System.Collections.Generic.KeyValuePair<K, V>.PIID;
        }

        private static readonly ConditionalWeakTable<object, ToIKeyValuePair> _adapterTable = new();

        internal static ToIKeyValuePair FindAdapter(IntPtr thisPtr)
        {
            var __this = (global::System.Collections.Generic.KeyValuePair<K, V>)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
            return _adapterTable.GetValue(__this, (pair) => new ToIKeyValuePair(ref __this));
        }

        public static ObjectReference<IUnknownVftbl> _FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);
        }

        public static Guid PIID = GuidGenerator.CreateIID(typeof(KeyValuePair<K, V>));

        public static implicit operator KeyValuePair<K, V>(IObjectReference obj) => (obj != null) ? new KeyValuePair<K, V>(obj) : null;
        public static implicit operator KeyValuePair<K, V>(ObjectReference<IUnknownVftbl> obj) => (obj != null) ? new KeyValuePair<K, V>(obj) : null;
        protected readonly ObjectReference<IUnknownVftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public KeyValuePair(IObjectReference obj) : this(obj.As<IUnknownVftbl>(PIID)) { }
        public KeyValuePair(ObjectReference<IUnknownVftbl> obj)
        {
            _obj = obj;
        }

        public unsafe K Key
        {
            get
            {
                return KeyValuePairMethods<K, V>._GetKey(ThisPtr);
            }
        }

        public unsafe V Value
        {
            get
            {
                return KeyValuePairMethods<K, V>._GetValue(ThisPtr);
            }
        }
    }
}