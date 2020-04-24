using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    [Guid("02B51929-C1C4-4A7E-8940-0312B5C18500")]
    public class KeyValuePair<K, V> : global::Windows.Foundation.Collections.IKeyValuePair<K, V>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.KeyValuePair<K, V> obj) =>
            MarshalInterface<global::System.Collections.Generic.KeyValuePair<K, V>>.CreateMarshaler(obj);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static object CreateRcw(IInspectable obj)
        {
            var pair = new KeyValuePair<K, V>(obj.As<Vftbl>());
            return (object)new global::System.Collections.Generic.KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        public static global::System.Collections.Generic.KeyValuePair<K, V> FromAbi(IntPtr thisPtr)
        {
            var pair = new KeyValuePair<K, V>(KeyValuePair<K, V>._FromAbi(thisPtr));
            return new global::System.Collections.Generic.KeyValuePair<K, V>(pair.Key, pair.Value);
        }

        public static IntPtr FromManaged(global::System.Collections.Generic.KeyValuePair<K, V> obj) =>
            CreateMarshaler(obj)?.GetRef() ?? IntPtr.Zero;

        public static (int length, IntPtr data) FromManagedArray(global::System.Collections.Generic.KeyValuePair<K, V>[] array) =>
            MarshalInterfaceHelper<global::System.Collections.Generic.KeyValuePair<K, V>>.FromManagedArray(array, (o) => FromManaged(o));

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

        [Guid("02B51929-C1C4-4A7E-8940-0312B5C18500")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate get_Key_0;
            public global::System.Delegate get_Value_1;
            public static Guid PIID = GuidGenerator.CreateIID(typeof(KeyValuePair<K, V>));
            private static readonly Type get_Key_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<K>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type get_Value_1_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<V>.AbiType.MakeByRefType(), typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                get_Key_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], get_Key_0_Type);
                get_Value_1 = Marshal.GetDelegateForFunctionPointer(vftbl[7], get_Value_1_Type);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    get_Key_0 = global::System.Delegate.CreateDelegate(get_Key_0_Type, typeof(Vftbl).GetMethod("Do_Abi_get_Key_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<K>.AbiType)),
                    get_Value_1 = global::System.Delegate.CreateDelegate(get_Value_1_Type, typeof(Vftbl).GetMethod("Do_Abi_get_Value_1", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<V>.AbiType))
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Key_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Value_1);

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<object, ToIKeyValuePair> _adapterTable = 
                new ConditionalWeakTable<object, ToIKeyValuePair>();

            private static ToIKeyValuePair FindAdapter(IntPtr thisPtr)
            {
                var __this = (global::System.Collections.Generic.KeyValuePair<K, V>)global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                return _adapterTable.GetValue(__this, (pair) => new ToIKeyValuePair(ref __this));
            }

            private static unsafe int Do_Abi_get_Key_0<KAbi>(void* thisPtr, out KAbi __return_value__)
            {
                K ____return_value__ = default;
                __return_value__ = default;
                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).Key;
                    __return_value__ = (KAbi)Marshaler<K>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Value_1<VAbi>(void* thisPtr, out VAbi __return_value__)
            {
                V ____return_value__ = default;
                __return_value__ = default;
                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).Value;
                    __return_value__ = (VAbi)Marshaler<V>.FromManaged(____return_value__);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> _FromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT.IInspectableVftbl.IUnknownVftbl, vftblT);
        }

        public static Guid PIID = Vftbl.PIID;

        public static implicit operator KeyValuePair<K, V>(IObjectReference obj) => (obj != null) ? new KeyValuePair<K, V>(obj) : null;
        public static implicit operator KeyValuePair<K, V>(ObjectReference<Vftbl> obj) => (obj != null) ? new KeyValuePair<K, V>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public KeyValuePair(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public KeyValuePair(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe K Key
        {
            get
            {
                var __params = new object[] { ThisPtr, null };
                try
                {
                    _obj.Vftbl.get_Key_0.DynamicInvokeAbi(__params);
                    return Marshaler<K>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<K>.DisposeAbi(__params[1]);
                }
            }
        }

        public unsafe V Value
        {
            get
            {
                var __params = new object[] { ThisPtr, null };
                try
                {
                    _obj.Vftbl.get_Value_1.DynamicInvokeAbi(__params);
                    return Marshaler<V>.FromAbi(__params[1]);
                }
                finally
                {
                    Marshaler<V>.DisposeAbi(__params[1]);
                }
            }
        }
    }
}
