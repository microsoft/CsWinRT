// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{
    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
    interface IVectorView<T> : IIterable<T>
    {
        T GetAt(uint index);
        bool IndexOf(T value, out uint index);
        uint GetMany(uint startIndex, ref T[] items);
        uint Size { get; }
    }
}

namespace System.Collections.Generic
{
    internal sealed class IReadOnlyListImpl<T> : IReadOnlyList<T>, IWinRTObject
    {
        private readonly IObjectReference _inner;

        private volatile IObjectReference __iReadOnlyListObjRef;
        private IObjectReference Make_IListObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iReadOnlyListObjRef, _inner.As<ABI.System.Collections.Generic.IReadOnlyList<T>.Vftbl>(), null);
            return __iReadOnlyListObjRef;
        }

        private IObjectReference iReadOnlyListObjRef => __iReadOnlyListObjRef ?? Make_IListObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<ABI.System.Collections.Generic.IEnumerable<T>.Vftbl>(), null);
            return __iEnumerableObjRef;
        }
        private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

        internal IReadOnlyListImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        public static IReadOnlyListImpl<T> CreateRcw(IInspectable obj) => new(obj.ObjRef);

        IObjectReference IWinRTObject.NativeObject => _inner;

        bool IWinRTObject.HasUnwrappableNativeObject => true;

        private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
        private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
            return _queryInterfaceCache;
        }
        global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
        private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
        private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
            return _additionalTypeData;
        }
        global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();

        public T this[int index] => ABI.System.Collections.Generic.IReadOnlyListMethods<T>.Indexer_Get(iReadOnlyListObjRef, index);

        public int Count => ABI.System.Collections.Generic.IReadOnlyListMethods<T>.get_Count(iReadOnlyListObjRef);

        public IEnumerator<T> GetEnumerator() => ABI.System.Collections.Generic.IEnumerableMethods<T>.GetEnumerator(iEnumerableObjRef);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace ABI.Windows.Foundation.Collections
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    internal static class IVectorViewMethods<T>
    {
        public static unsafe T GetAt(IObjectReference obj, uint index)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;

            var __params = new object[] { ThisPtr, index, null };
            try
            {
                _obj.Vftbl.GetAt_0.DynamicInvokeAbi(__params);
                return Marshaler<T>.FromAbi(__params[2]);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(__params[2]);
            }
        }

        public static unsafe bool IndexOf(IObjectReference obj, T value, out uint index)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;

            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.IndexOf_2.DynamicInvokeAbi(__params);
                index = (uint)__params[2];
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe uint GetMany(IObjectReference obj, uint startIndex, ref T[] items)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyList<T>.Vftbl>)obj; 
            var ThisPtr = _obj.ThisPtr;

            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_3(ThisPtr, startIndex, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public static unsafe uint get_Size(IObjectReference obj)
        {
            var _obj = (ObjectReference<ABI.System.Collections.Generic.IReadOnlyList<T>.Vftbl>)obj; 
            var ThisPtr = _obj.ThisPtr;

            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSize_1(ThisPtr, out __retval));
            return __retval;
        }
    }
}

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;
    using global::System.Diagnostics.CodeAnalysis;

#if EMBED
    internal
#else
    public
#endif
    static class IReadOnlyListMethods<T>
    {
        public static int get_Count(IObjectReference obj)
        {
            uint size = ABI.Windows.Foundation.Collections.IVectorViewMethods<T>.get_Size(obj);
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            }
            return (int)size;
        }

        public static T Indexer_Get(IObjectReference obj, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            try
            {
                return ABI.Windows.Foundation.Collections.IVectorViewMethods<T>.GetAt(obj, (uint)index);
                // We delegate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                    DynamicallyAccessedMemberTypes.NonPublicMethods |
                                    DynamicallyAccessedMemberTypes.PublicNestedTypes |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        internal static Type implType = typeof(IReadOnlyListImpl<T>);

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                    DynamicallyAccessedMemberTypes.NonPublicMethods |
                                    DynamicallyAccessedMemberTypes.PublicNestedTypes |
                                    DynamicallyAccessedMemberTypes.PublicFields)]
        public static Type InitImplType() 
        {
            ComWrappersSupport.ImplTypesDict.Add(typeof(IReadOnlyListImpl<>), implType);
            return implType;
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
    interface IReadOnlyList<T> : global::System.Collections.Generic.IReadOnlyList<T>, global::Windows.Foundation.Collections.IVectorView<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyList<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<Vftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IReadOnlyList<T> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IReadOnlyList<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IReadOnlyList<T>));

        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IVectorView<T>
        {
            private readonly global::System.Collections.Generic.IReadOnlyList<T> _list;

            internal ToAbiHelper(global::System.Collections.Generic.IReadOnlyList<T> list) => _list = list;

            global::System.Collections.Generic.IEnumerator<T> global::Windows.Foundation.Collections.IIterable<T>.First() => _list.GetEnumerator();

            private static void EnsureIndexInt32(uint index, int limit = int.MaxValue)
            {
                // We use '<=' and not '<' because int.MaxValue == index would imply
                // that Size > int.MaxValue:
                if (((uint)int.MaxValue) <= index || index >= (uint)limit)
                {
                    Exception e = new ArgumentOutOfRangeException(nameof(index), WinRTRuntimeErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }
            }

            public T GetAt(uint index)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    return _list[(int)index];
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw;
                }
            }

            public uint Size => (uint)_list.Count;

            public bool IndexOf(T value, out uint index)
            {
                int ind = -1;
                int max = _list.Count;
                for (int i = 0; i < max; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(value, _list[i]))
                    {
                        ind = i;
                        break;
                    }
                }

                if (-1 == ind)
                {
                    index = 0;
                    return false;
                }

                index = (uint)ind;
                return true;
            }

            public uint GetMany(uint startIndex, ref T[] items)
            {
                // Spec says "calling GetMany with startIndex equal to the length of the vector
                // (last valid index + 1) and any specified capacity will succeed and return zero actual
                // elements".
                if (startIndex == _list.Count)
                    return 0;

                EnsureIndexInt32(startIndex, _list.Count);

                if (items == null)
                {
                    return 0;
                }

                uint itemCount = Math.Min((uint)items.Length, (uint)_list.Count - startIndex);

                for (uint i = 0; i < itemCount; ++i)
                {
                    items[i] = _list[(int)(i + startIndex)];
                }

                if (typeof(T) == typeof(string))
                {
                    string[] stringItems = (items as string[])!;

                    // Fill in the rest of the array with string.Empty to avoid marshaling failure
                    for (uint i = itemCount; i < items.Length; ++i)
                        stringItems[i] = string.Empty;
                }

                return itemCount;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
        internal static Type delegateHelperType = typeof(AbiDelegateHelper<T>);

        internal static class AbiDelegateHelper<TAbi> 
        {
            private static unsafe int Do_Abi_GetAt_0(void* thisPtr, uint index, out TAbi __return_value__)
            {
                T ____return_value__ = default;
                __return_value__ = default;
                try
                {
                    ____return_value__ = Vftbl.FindAdapter(new IntPtr(thisPtr)).GetAt(index);
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_2(void* thisPtr, TAbi value, out uint index, out byte __return_value__)
            {
                bool ____return_value__ = default;

                index = default;
                __return_value__ = default;
                uint __index = default;

                try
                {
                    ____return_value__ = Vftbl.FindAdapter(new IntPtr(thisPtr)).IndexOf(Marshaler<T>.FromAbi(value), out __index);
                    index = __index;
                    __return_value__ = (byte)(____return_value__ ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }


        [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate GetAt_0;
            private void* _get_Size_1;
            internal delegate* unmanaged[Stdcall]<IntPtr, out uint, int> GetSize_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)_get_Size_1; set => _get_Size_1 = (void*)value; }
            public global::System.Delegate IndexOf_2;
            private void* _getMany_3;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, out uint, int> GetMany_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, out uint, int>)_getMany_3; set => _getMany_3 = (void*)value; }

            public static Guid PIID = GuidGenerator.CreateIID(typeof(IReadOnlyList<T>));
            private static readonly IDelegateHelper GetAt_0_Type_HelperType = Projections.GetAbiDelegateHelper(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly IDelegateHelper IndexOf_2_Type_HelperType = Projections.GetAbiDelegateHelper(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) });
            private static Type GetAt_0_Type = GetAt_0_Type_HelperType.DelegateType;
            private static Type IndexOf_2_Type = IndexOf_2_Type_HelperType.DelegateType;

            internal unsafe Vftbl(IntPtr thisPtr) : this()
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                // might need another getfunctionpointer function
                GetAt_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], GetAt_0_Type);
                GetSize_1 = (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)vftbl[7];
                IndexOf_2 = Marshal.GetDelegateForFunctionPointer(vftbl[8], IndexOf_2_Type);
                GetMany_3 = (delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, out uint, int>)vftbl[9];
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            private static readonly Delegate[] DelegateCache = new Delegate[2];

            // 

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = global::System.Delegate.CreateDelegate(GetAt_0_Type, typeof(AbiDelegateHelper<>).MakeGenericType(new Type[] { typeof(T), Marshaler<T>.AbiType }).GetMethod("Do_Abi_GetAt_0", BindingFlags.NonPublic | BindingFlags.Static)),
                    _get_Size_1 = (void*)Marshal.GetFunctionPointerForDelegate<_get_PropertyAsUInt32>(new _get_PropertyAsUInt32(Do_Abi_get_Size_1)),
                    IndexOf_2 = global::System.Delegate.CreateDelegate(IndexOf_2_Type, typeof(AbiDelegateHelper<>).MakeGenericType(new Type[] { typeof(T), Marshaler<T>.AbiType }).GetMethod("Do_Abi_IndexOf_2", BindingFlags.NonPublic | BindingFlags.Static)),
                    _getMany_3 = (void*)Marshal.GetFunctionPointerForDelegate<IReadOnlyList_Delegates.GetMany_3>(new IReadOnlyList_Delegates.GetMany_3(Do_Abi_GetMany_3)),
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);

                //nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetAt_0);
                nativeVftbl[6] = GetAt_0_Type_HelperType.GetFunctionPointer(AbiToProjectionVftable.GetAt_0);    
                nativeVftbl[7] = (IntPtr)AbiToProjectionVftable.GetSize_1;
                //Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.IndexOf_2);
                nativeVftbl[8] = IndexOf_2_Type_HelperType.GetFunctionPointer(AbiToProjectionVftable.IndexOf_2);
                
                nativeVftbl[9] = (IntPtr)AbiToProjectionVftable.GetMany_3;

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyList<T>, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyList<T>, ToAbiHelper>();

            internal static ToAbiHelper FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IReadOnlyList<T>>(thisPtr);
                return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
            }

            
            private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).GetMany(startIndex, ref __items);
                    Marshaler<T>.CopyManagedArray(__items, items);
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).Size;
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        public static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            var vftblT = new Vftbl(thisPtr);
            return ObjectReference<Vftbl>.FromAbi(thisPtr, vftblT);
        }
        public static Guid PIID = Vftbl.PIID;

        int global::System.Collections.Generic.IReadOnlyCollection<T>.Count
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyList<T>).TypeHandle));
                return IReadOnlyListMethods<T>.get_Count(_obj);
            }
        }

        T global::System.Collections.Generic.IReadOnlyList<T>.this[int index]
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyList<T>).TypeHandle));
                return IReadOnlyListMethods<T>.Indexer_Get(_obj, index);
            }
        }

        global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            ((IWinRTObject)this).IsInterfaceImplemented(typeof(global::System.Collections.Generic.IEnumerable<T>).TypeHandle, true);
            var _objEnumerable = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<T>).TypeHandle));
            return IEnumerableMethods<T>.GetEnumerator(_objEnumerable);
        }

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

#if EMBED
    internal
#else
    public
#endif
    static class IReadOnlyList_Delegates
    {
        public unsafe delegate int GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);
    }
}
