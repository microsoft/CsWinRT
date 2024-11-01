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
    using global::System.ComponentModel;
    using global::System.Diagnostics.CodeAnalysis;
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
        internal volatile unsafe static delegate*<IObjectReference, K> _GetKey;
        internal volatile unsafe static delegate*<IObjectReference, V> _GetValue;
        internal volatile static bool _RcwHelperInitialized;

        static KeyValuePairMethods()
        {
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.KeyValuePair<K, V>), typeof(global::ABI.System.Collections.Generic.KeyValuePair<K, V>));
        }

        internal static unsafe bool EnsureInitialized()
        {
#if NET
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            // Here we just always return true and rely on the AOT generator doing what's needed.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return true;
            }
#endif
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            InitRcwHelperFallbackIfNeeded();
#pragma warning restore IL3050

#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void InitRcwHelperFallbackIfNeeded()
            {
                // Handle the compat scenario where the source generator wasn't used and IDIC hasn't been used yet
                // and due to that the function pointers haven't been initialized.
                if (!_RcwHelperInitialized)
                {
                    var initRcwHelperFallback = (Func<bool>)typeof(KeyValuePairMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                        GetMethod("InitRcwHelperFallback", BindingFlags.NonPublic | BindingFlags.Static).
                        CreateDelegate(typeof(Func<bool>));
                    initRcwHelperFallback();
                }
            }

            return true;
        }

        internal static unsafe K GetKey(IObjectReference obj)
        {
            EnsureInitialized();
            return _GetKey(obj);
        }

        internal static unsafe V GetValue(IObjectReference obj)
        {
            EnsureInitialized();
            return _GetValue(obj);
        }

        internal static readonly Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(KeyValuePair<K, V>));
        public static Guid IID => PIID;

        private static IntPtr abiToProjectionVftablePtr;
        public static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

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
        public unsafe static bool InitRcwHelper(
            delegate*<IObjectReference, K> getKey,
            delegate*<IObjectReference, V> getValue)
        {
            if (KeyValuePairMethods<K, V>._RcwHelperInitialized)
            {
                return true;
            }

            KeyValuePairMethods<K, V>._GetKey = getKey;
            KeyValuePairMethods<K, V>._GetValue = getValue;

#if NET
            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.KeyValuePair<K, V>),
                KeyValuePair<K, V>.CreateRcw);
#endif
            KeyValuePairMethods<K, V>._RcwHelperInitialized = true;
            return true;
        }

        private unsafe static bool InitRcwHelperFallback()
        {
            return InitRcwHelper(&get_Key, &get_Value);
        }

        private static unsafe K get_Key(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            KAbi keyAbi = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)ThisPtr)[6](ThisPtr, &keyAbi));
                GC.KeepAlive(obj);
                return Marshaler<K>.FromAbi(keyAbi);
            }
            finally
            {
                Marshaler<K>.DisposeAbi(keyAbi);
            }
        }

        private static unsafe V get_Value(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            VAbi valueAbi = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, void*, int>**)ThisPtr)[7](ThisPtr, &valueAbi));
                GC.KeepAlive(obj);
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

            KeyValuePairHelper.TryAddKeyValuePairCCW(
                typeof(global::System.Collections.Generic.KeyValuePair<K, V>),
                KeyValuePairMethods<K, V>.PIID,
                KeyValuePairMethods<K, V>.AbiToProjectionVftablePtr);

            return true;
        }

        private static global::System.Delegate[] DelegateCache;

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        internal static unsafe void InitFallbackCCWVtable()
        {
            InitRcwHelperFallback();

            Type get_Key_0_Type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(KAbi*), typeof(int) });
            Type get_Value_1_Type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(VAbi*), typeof(int) });

            DelegateCache = new global::System.Delegate[]
            {
                global::System.Delegate.CreateDelegate(get_Key_0_Type, typeof(KeyValuePairMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_get_Key_0), BindingFlags.NonPublic | BindingFlags.Static)),
                global::System.Delegate.CreateDelegate(get_Value_1_Type, typeof(KeyValuePairMethods<K, KAbi, V, VAbi>).GetMethod(nameof(Do_Abi_get_Value_1), BindingFlags.NonPublic | BindingFlags.Static)),
            };

            InitCcw(
                (delegate* unmanaged[Stdcall]<IntPtr, KAbi*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[0]),
                (delegate* unmanaged[Stdcall]<IntPtr, VAbi*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[1])
            );
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

        public static void CopyAbiArray(global::System.Collections.Generic.KeyValuePair<K, V>[] array, object box) => MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.CopyAbiArray(array, box, FromAbi);

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
#if NET
            if (RuntimeFeature.IsDynamicCodeCompiled)
#endif
            {
                // Simple invocation guarded by a direct runtime feature check to help the linker.
                // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                InitFallbackCCWVTableIfNeeded();
#pragma warning restore IL3050

#if NET8_0_OR_GREATER
                [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
#if NET
                [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void InitFallbackCCWVTableIfNeeded()
                {
                    if (KeyValuePairMethods<K, V>.AbiToProjectionVftablePtr == default)
                    {
                        // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                        var initFallbackCCWVtable = (Action)typeof(KeyValuePairMethods<,,,>).MakeGenericType(typeof(K), Marshaler<K>.AbiType, typeof(V), Marshaler<V>.AbiType).
                            GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                            CreateDelegate(typeof(Action));
                        initFallbackCCWVtable();
                    }
                }
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

            public static readonly Guid PIID = ABI.System.Collections.Generic.KeyValuePair<K, V>.PIID;
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
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, PIID);
        }

        public static readonly Guid PIID = KeyValuePairMethods<K, V>.IID;

#if !NET
        public static implicit operator KeyValuePair<K, V>(IObjectReference obj) => (obj != null) ? new KeyValuePair<K, V>(obj) : null;
        public static implicit operator KeyValuePair<K, V>(ObjectReference<IUnknownVftbl> obj) => (obj != null) ? new KeyValuePair<K, V>(obj) : null;
#endif
        protected readonly ObjectReference<IUnknownVftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;

        public KeyValuePair(IObjectReference obj) : this(obj.As<IUnknownVftbl>(PIID)) { }
        public KeyValuePair(ObjectReference<IUnknownVftbl> obj)
        {
            _obj = obj;
        }

        public unsafe K Key
        {
            get
            {
                return KeyValuePairMethods<K, V>.GetKey(_obj);
            }
        }

        public unsafe V Value
        {
            get
            {
                return KeyValuePairMethods<K, V>.GetValue(_obj);
            }
        }
    }
}