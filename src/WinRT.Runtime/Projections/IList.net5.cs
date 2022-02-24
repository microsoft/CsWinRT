// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
using System.Collections.Concurrent;

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{

    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
    interface IVector<T> : IIterable<T>
    {
        T GetAt(uint index);
        IReadOnlyList<T> GetView(); // Combining IVector & IReadOnlyList needs redesign
        bool IndexOf(T value, out uint index);
        void SetAt(uint index, T value);
        void InsertAt(uint index, T value);
        void RemoveAt(uint index);
        void Append(T value);
        void RemoveAtEnd();
        void _Clear();
        uint GetMany(uint startIndex, ref T[] items);
        void ReplaceAll(T[] items);
        uint Size { get; }
    }
}

namespace System.Collections.Generic
{
    internal sealed class IListImpl<T> : global::System.Collections.Generic.IList<T>, IWinRTObject
    {
        private readonly IObjectReference _inner;

        private volatile IObjectReference __iListObjRef;
        private IObjectReference Make_IListObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iListObjRef, _inner.As<ABI.System.Collections.Generic.IList<T>.Vftbl>(), null);
            return __iListObjRef;
        }
        private IObjectReference iListObjRef => __iListObjRef ?? Make_IListObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<ABI.System.Collections.Generic.IEnumerable<T>.Vftbl>(), null);
            return __iEnumerableObjRef;
        }
        private IObjectReference iEnumerableObjRef => __iEnumerableObjRef ?? Make_IEnumerableObjRef();

        internal IListImpl(IObjectReference _inner)
        {
            this._inner = _inner;
        }

        public static IListImpl<T> CreateRcw(IInspectable obj) => new(obj.ObjRef);

        public T this[int index] 
        {
            get => ABI.System.Collections.Generic.IListMethods<T>.Indexer_Get(iListObjRef, index);
            set => ABI.System.Collections.Generic.IListMethods<T>.Indexer_Set(iListObjRef, index, value); 
        }

        public int Count => ABI.System.Collections.Generic.IListMethods<T>.get_Count(iListObjRef);

        public bool IsReadOnly => ABI.System.Collections.Generic.IListMethods<T>.get_IsReadOnly(iListObjRef);

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

        public void Add(T item)
        {
            ABI.System.Collections.Generic.IListMethods<T>.Add(iListObjRef, item);
        }

        public void Clear()
        {
            ABI.System.Collections.Generic.IListMethods<T>.Clear(iListObjRef);
        }

        public bool Contains(T item)
        {
            return ABI.System.Collections.Generic.IListMethods<T>.Contains(iListObjRef, item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ABI.System.Collections.Generic.IListMethods<T>.CopyTo(iListObjRef, array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ABI.System.Collections.Generic.IEnumerableMethods<T>.GetEnumerator(iEnumerableObjRef);
        }

        public int IndexOf(T item)
        {
            return ABI.System.Collections.Generic.IListMethods<T>.IndexOf(iListObjRef, item);
        }

        public void Insert(int index, T item)
        {
            ABI.System.Collections.Generic.IListMethods<T>.Insert(iListObjRef, index, item);
        }

        public bool Remove(T item)
        {
            return ABI.System.Collections.Generic.IListMethods<T>.Remove(iListObjRef, item);
        }

        public void RemoveAt(int index)
        {
            ABI.System.Collections.Generic.IListMethods<T>.RemoveAt(iListObjRef, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

#if EMBED
    internal
#else
    public
#endif
    static class IListMethods<T> {
        
        public static int get_Count(IObjectReference obj)
        {
            uint size = IVectorMethods<T>.get_Size(obj);
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)size;
        }

        public static bool get_IsReadOnly(IObjectReference obj) 
        {
            return false;
        }

        public static void Add(IObjectReference obj, T item) => IVectorMethods<T>.Append(obj, item);

        public static void Clear(IObjectReference obj) => IVectorMethods<T>.Clear(obj);

        public static bool Contains(IObjectReference obj, T item) => IVectorMethods<T>.IndexOf(obj, item, out _);

        public static void CopyTo(IObjectReference obj, T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length <= arrayIndex && get_Count(obj) > 0)
                throw new ArgumentException(ErrorStrings.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < get_Count(obj))
                throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);


            int count = get_Count(obj);
            for (int i = 0; i < count; i++)
            {
                array[i + arrayIndex] = GetAtHelper(obj, (uint)i);
            }
        }

        public static bool Remove(IObjectReference obj, T item)
        {
            uint index;
            bool exists = IVectorMethods<T>.IndexOf(obj, item, out index);

            if (!exists)
                return false;

            if (((uint)int.MaxValue) < index)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            }

            RemoveAtHelper(obj, index);
            return true;
        }

        public static T Indexer_Get(IObjectReference obj, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            return GetAtHelper(obj, (uint)index);
        }

        public static void Indexer_Set(IObjectReference obj, int index, T value)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            SetAtHelper(obj, (uint)index, value);
        }

        public static int IndexOf(IObjectReference obj, T item)
        {
            uint index;
            bool exists = IVectorMethods<T>.IndexOf(obj, item, out index);

            if (!exists)
                return -1;

            if (((uint)int.MaxValue) < index)
            {
                throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
            }

            return (int)index;
        }

        public static void Insert(IObjectReference obj, int index, T item)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            InsertAtHelper(obj, (uint)index, item);
        }

        public static void RemoveAt(IObjectReference obj, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            RemoveAtHelper(obj, (uint)index);
        }

        internal static T GetAtHelper(IObjectReference obj, uint index)
        {
            try
            {
                return IVectorMethods<T>.GetAt(obj, index);

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

        private static void SetAtHelper(IObjectReference obj, uint index, T value)
        {
            try
            {
                IVectorMethods<T>.SetAt(obj, index, value);

                // We deligate bounds checking to the underlying collection and if it detected a fault,
                // we translate it to the right exception:
            }
            catch (Exception ex)
            {
                if (ExceptionHelpers.E_BOUNDS == ex.HResult)
                    throw new ArgumentOutOfRangeException(nameof(index));

                throw;
            }
        }

        private static void InsertAtHelper(IObjectReference obj, uint index, T item)
        {
            try
            {
                IVectorMethods<T>.InsertAt(obj, index, item);

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

        internal static void RemoveAtHelper(IObjectReference obj, uint index)
        {
            try
            {
                IVectorMethods<T>.RemoveAt(obj, index);

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

    }

    internal static class IVectorMethods<T>
    {
        public static unsafe uint get_Size(IObjectReference obj)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSize_1(_obj.ThisPtr, out __retval));
            return __retval;
        }

        public static unsafe T GetAt(IObjectReference obj, uint index)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
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

        public static unsafe global::System.Collections.Generic.IReadOnlyList<T> GetView(IObjectReference obj)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_2(ThisPtr, out __retval));
                return MarshalInterface<global::System.Collections.Generic.IReadOnlyList<T>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(__retval);
            }
        }

        public static unsafe bool IndexOf(IObjectReference obj, T value, out uint index)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.IndexOf_3.DynamicInvokeAbi(__params);
                index = (uint)__params[2];
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe void SetAt(IObjectReference obj, uint index, T value)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.SetAt_4.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe void InsertAt(IObjectReference obj, uint index, T value)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.InsertAt_5.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe void RemoveAt(IObjectReference obj, uint index)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAt_6(ThisPtr, index));
        }

        public static unsafe void Append(IObjectReference obj, T value)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.Append_7.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe void RemoveAtEnd(IObjectReference obj)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAtEnd_8(ThisPtr));
        }

        public static unsafe void Clear(IObjectReference obj)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_9(ThisPtr));
        }

        public static unsafe uint GetMany(IObjectReference obj, uint startIndex, ref T[] items)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_10(ThisPtr, startIndex, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        public static unsafe void ReplaceAll(IObjectReference obj, T[] items)
        {
            var _obj = (ObjectReference<IList<T>.Vftbl>)obj;
            var ThisPtr = _obj.ThisPtr;
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.ReplaceAll_11(ThisPtr, __items_length, __items_data));
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
    interface IList<T> : global::System.Collections.Generic.IList<T>, global::Windows.Foundation.Collections.IVector<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IList<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<Vftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IList<T> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IList<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).DetachRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IVector<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList<T>));

        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IVector<T>
        {
            private global::System.Collections.Generic.IList<T> _list;

            public ToAbiHelper(global::System.Collections.Generic.IList<T> list) => _list = list;

            global::System.Collections.Generic.IEnumerator<T> global::Windows.Foundation.Collections.IIterable<T>.First() => _list.GetEnumerator();

            private static void EnsureIndexInt32(uint index, int limit = int.MaxValue)
            {
                // We use '<=' and not '<' because int.MaxValue == index would imply
                // that Size > int.MaxValue:
                if (((uint)int.MaxValue) <= index || index >= (uint)limit)
                {
                    Exception e = new ArgumentOutOfRangeException(nameof(index), ErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
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
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
                }
            }

            public uint Size => (uint)_list.Count;

            global::System.Collections.Generic.IReadOnlyList<T> global::Windows.Foundation.Collections.IVector<T>.GetView()
            {
                // Note: This list is not really read-only - you could QI for a modifiable
                // list.  We gain some perf by doing this.  We believe this is acceptable.
                if (!(_list is global::System.Collections.Generic.IReadOnlyList<T> roList))
                {
                    roList = new ReadOnlyCollection<T>(_list);
                }
                return roList;
            }

            public bool IndexOf(T value, out uint index)
            {
                int ind = _list.IndexOf(value);

                if (-1 == ind)
                {
                    index = 0;
                    return false;
                }

                index = (uint)ind;
                return true;
            }

            public void SetAt(uint index, T value)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    _list[(int)index] = value;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
                }
            }

            public void InsertAt(uint index, T value)
            {
                // Inserting at an index one past the end of the list is equivalent to appending
                // so we need to ensure that we're within (0, count + 1).
                EnsureIndexInt32(index, _list.Count + 1);

                try
                {
                    _list.Insert((int)index, value);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Change error code to match what WinRT expects
                    ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw;
                }
            }

            public void RemoveAt(uint index)
            {
                EnsureIndexInt32(index, _list.Count);

                try
                {
                    _list.RemoveAt((int)index);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Change error code to match what WinRT expects
                    ex.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw;
                }
            }

            public void Append(T value)
            {
                _list.Add(value);
            }

            public void RemoveAtEnd()
            {
                if (_list.Count == 0)
                {
                    Exception e = new InvalidOperationException(ErrorStrings.InvalidOperation_CannotRemoveLastFromEmptyCollection);
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }

                uint size = (uint)_list.Count;
                RemoveAt(size - 1);
            }

            public void _Clear()
            {
                _list.Clear();
            }

            public uint GetMany(uint startIndex, ref T[] items)
            {
                return GetManyHelper(_list, startIndex, items);
            }

            public void ReplaceAll(T[] items)
            {
                _list.Clear();

                if (items != null)
                {
                    foreach (T item in items)
                    {
                        _list.Add(item);
                    }
                }
            }

            private static uint GetManyHelper(global::System.Collections.Generic.IList<T> sourceList, uint startIndex, T[] items)
            {
                // Calling GetMany with a start index equal to the size of the list should always
                // return 0 elements, regardless of the input item size
                if (startIndex == sourceList.Count)
                {
                    return 0;
                }

                EnsureIndexInt32(startIndex, sourceList.Count);

                if (items == null)
                {
                    return 0;
                }

                uint itemCount = Math.Min((uint)items.Length, (uint)sourceList.Count - startIndex);
                for (uint i = 0; i < itemCount; ++i)
                {
                    items[i] = sourceList[(int)(i + startIndex)];
                }

                if (typeof(T) == typeof(string))
                {
                    string[] stringItems = (items as string[])!;

                    // Fill in rest of the array with string.Empty to avoid marshaling failure
                    for (uint i = itemCount; i < items.Length; ++i)
                        stringItems[i] = string.Empty;
                }

                return itemCount;
            }
        }

        [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
        public unsafe struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate GetAt_0;
            private void* _get_Size_1;
            internal delegate* unmanaged[Stdcall]<IntPtr, out uint, int> GetSize_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)_get_Size_1; set => _get_Size_1 = (void*)value; }
            private void* _getView_2;
            public delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int> GetView_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)_getView_2; set => _getView_2 = (void*)value; }
            public global::System.Delegate IndexOf_3;
            public global::System.Delegate SetAt_4;
            public global::System.Delegate InsertAt_5;
            private void* _removeAt_6;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, int> RemoveAt_6 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, int>)_removeAt_6; set => _removeAt_6 = (void*)value; }
            public global::System.Delegate Append_7;
            private void* _removeAtEnd_8;
            public delegate* unmanaged[Stdcall]<IntPtr, int> RemoveAtEnd_8 { get => (delegate* unmanaged[Stdcall]<IntPtr, int>)_removeAtEnd_8; set => _removeAtEnd_8 = (void*)value; }
            private void* _clear_9;
            public delegate* unmanaged[Stdcall]<IntPtr, int> Clear_9 { get => (delegate* unmanaged[Stdcall]<IntPtr, int>)_clear_9; set => _clear_9 = (void*)value; }
            private void* _getMany_10;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, out uint, int> GetMany_10 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, out uint, int>)_getMany_10; set => _getMany_10 = (void*)value; }
            private void* _replaceAll_11;
            public delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int> ReplaceAll_11 { get => (delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int>)_replaceAll_11; set => _replaceAll_11 = (void*)value; }

            public static Guid PIID = GuidGenerator.CreateIID(typeof(IList<T>));
            private static readonly Type GetAt_0_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type IndexOf_3_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type SetAt_4_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType, typeof(int) });
            private static readonly Type InsertAt_5_Type = Expression.GetDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType, typeof(int) });
            private static readonly Type Append_7_Type = Expression.GetDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr) : this()
            {
                var vftblPtr = Marshal.PtrToStructure<VftblPtr>(thisPtr);
                var vftbl = (IntPtr*)vftblPtr.Vftbl;
                IInspectableVftbl = Marshal.PtrToStructure<IInspectable.Vftbl>(vftblPtr.Vftbl);
                GetAt_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], GetAt_0_Type);
                GetSize_1 = (delegate* unmanaged[Stdcall]<IntPtr, out uint, int>)vftbl[7];
                GetView_2 = (delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>)vftbl[8];
                IndexOf_3 = Marshal.GetDelegateForFunctionPointer(vftbl[9], IndexOf_3_Type);
                SetAt_4 = Marshal.GetDelegateForFunctionPointer(vftbl[10], SetAt_4_Type);
                InsertAt_5 = Marshal.GetDelegateForFunctionPointer(vftbl[11], InsertAt_5_Type);
                RemoveAt_6 = (delegate* unmanaged[Stdcall]<IntPtr, uint, int>)vftbl[12];
                Append_7 = Marshal.GetDelegateForFunctionPointer(vftbl[13], Append_7_Type);
                RemoveAtEnd_8 = (delegate* unmanaged[Stdcall]<IntPtr, int>)vftbl[14];
                Clear_9 = (delegate* unmanaged[Stdcall]<IntPtr, int>)vftbl[15];
                GetMany_10 = (delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, out uint, int>)vftbl[16];
                ReplaceAll_11 = (delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int>)vftbl[17];
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            private static readonly Delegate[] DelegateCache = new Delegate[7];

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = global::System.Delegate.CreateDelegate(GetAt_0_Type, typeof(Vftbl).GetMethod("Do_Abi_GetAt_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    _get_Size_1 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new _get_PropertyAsUInt32(Do_Abi_get_Size_1)),
                    _getView_2 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IList_Delegates.GetView_2(Do_Abi_GetView_2)),
                    IndexOf_3 = global::System.Delegate.CreateDelegate(IndexOf_3_Type, typeof(Vftbl).GetMethod("Do_Abi_IndexOf_3", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    SetAt_4 = global::System.Delegate.CreateDelegate(SetAt_4_Type, typeof(Vftbl).GetMethod("Do_Abi_SetAt_4", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    InsertAt_5 = global::System.Delegate.CreateDelegate(InsertAt_5_Type, typeof(Vftbl).GetMethod("Do_Abi_InsertAt_5", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    _removeAt_6 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new IList_Delegates.RemoveAt_6(Do_Abi_RemoveAt_6)),
                    Append_7 = global::System.Delegate.CreateDelegate(Append_7_Type, typeof(Vftbl).GetMethod("Do_Abi_Append_7", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    _removeAtEnd_8 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new IList_Delegates.RemoveAtEnd_8(Do_Abi_RemoveAtEnd_8)),
                    _clear_9 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[4] = new IList_Delegates.Clear_9(Do_Abi_Clear_9)),
                    _getMany_10 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[5] = new IList_Delegates.GetMany_10(Do_Abi_GetMany_10)),
                    _replaceAll_11 = (void*)Marshal.GetFunctionPointerForDelegate(DelegateCache[6] = new IList_Delegates.ReplaceAll_11(Do_Abi_ReplaceAll_11)),
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 12);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetAt_0);
                nativeVftbl[7] = (IntPtr)AbiToProjectionVftable.GetSize_1;
                nativeVftbl[8] = (IntPtr)AbiToProjectionVftable.GetView_2;
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.IndexOf_3);
                nativeVftbl[10] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.SetAt_4);
                nativeVftbl[11] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.InsertAt_5);
                nativeVftbl[12] = (IntPtr)AbiToProjectionVftable._removeAt_6;
                nativeVftbl[13] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Append_7);
                nativeVftbl[14] = (IntPtr)AbiToProjectionVftable._removeAtEnd_8;
                nativeVftbl[15] = (IntPtr)AbiToProjectionVftable._clear_9;
                nativeVftbl[16] = (IntPtr)AbiToProjectionVftable._getMany_10;
                nativeVftbl[17] = (IntPtr)AbiToProjectionVftable._replaceAll_11;

                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.Generic.IList<T>, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.Generic.IList<T>, ToAbiHelper>();

            private static global::Windows.Foundation.Collections.IVector<T> FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IList<T>>(thisPtr);
                return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
            }

            private static unsafe int Do_Abi_GetAt_0<TAbi>(void* thisPtr, uint index, out TAbi __return_value__)
            {
                T ____return_value__ = default;
                __return_value__ = default;
                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).GetAt(index);
                    __return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, out IntPtr __return_value__)
            {
                global::System.Collections.Generic.IReadOnlyList<T> ____return_value__ = default;
                __return_value__ = default;

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).GetView();
                    __return_value__ = MarshalInterface<global::System.Collections.Generic.IReadOnlyList<T>>.FromManaged(____return_value__);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_3<TAbi>(void* thisPtr, TAbi value, out uint index, out byte __return_value__)
            {
                bool ____return_value__ = default;

                index = default;
                __return_value__ = default;
                uint __index = default;

                try
                {
                    ____return_value__ = FindAdapter(new IntPtr(thisPtr)).IndexOf(Marshaler<T>.FromAbi(value), out __index); 
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
            private static unsafe int Do_Abi_SetAt_4<TAbi>(void* thisPtr, uint index, TAbi value)
            {
                try
                {
                    FindAdapter(new IntPtr(thisPtr)).SetAt(index, Marshaler<T>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_InsertAt_5<TAbi>(void* thisPtr, uint index, TAbi value)
            {


                try
                {
                    FindAdapter(new IntPtr(thisPtr)).InsertAt(index, Marshaler<T>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_RemoveAt_6(IntPtr thisPtr, uint index)
            {
                try
                {
                    FindAdapter(thisPtr).RemoveAt(index);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Append_7<TAbi>(void* thisPtr, TAbi value)
            {
                try
                {
                    FindAdapter(new IntPtr(thisPtr)).Append(Marshaler<T>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_RemoveAtEnd_8(IntPtr thisPtr)
            {
                try
                {
                    FindAdapter(thisPtr).RemoveAtEnd();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_Clear_9(IntPtr thisPtr)
            {
                try
                {
                    FindAdapter(thisPtr)._Clear();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__)
            {
                uint ____return_value__ = default;

                __return_value__ = default;
                T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

                try
                {
                    ____return_value__ = FindAdapter(thisPtr).GetMany(startIndex, ref __items); Marshaler<T>.CopyManagedArray(__items, items);
                    __return_value__ = ____return_value__;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_ReplaceAll_11(IntPtr thisPtr, int __itemsSize, IntPtr items)
            {
                try
                {
                    FindAdapter(thisPtr).ReplaceAll(Marshaler<T>.FromAbiArray((__itemsSize, items)));
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
        
        unsafe T global::Windows.Foundation.Collections.IVector<T>.GetAt(uint index)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
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

        unsafe global::System.Collections.Generic.IReadOnlyList<T> global::Windows.Foundation.Collections.IVector<T>.GetView()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_2(ThisPtr, out __retval));
                return MarshalInterface<global::System.Collections.Generic.IReadOnlyList<T>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(__retval);
            }
        }

        unsafe bool global::Windows.Foundation.Collections.IVector<T>.IndexOf(T value, out uint index)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.IndexOf_3.DynamicInvokeAbi(__params);
                index = (uint)__params[2];
                return (byte)__params[3] != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.SetAt(uint index, T value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.SetAt_4.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.InsertAt(uint index, T value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.InsertAt_5.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.RemoveAt(uint index)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAt_6(ThisPtr, index));
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.Append(T value)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.Append_7.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.RemoveAtEnd()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAtEnd_8(ThisPtr));
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>._Clear()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_9(ThisPtr));
        }

        unsafe uint global::Windows.Foundation.Collections.IVector<T>.GetMany(uint startIndex, ref T[] items)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            uint __retval = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetMany_10(ThisPtr, startIndex, __items_length, __items_data, out __retval));
                items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                return __retval;
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.ReplaceAll(T[] items)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            var ThisPtr = _obj.ThisPtr;
            object __items = default;
            int __items_length = default;
            IntPtr __items_data = default;
            try
            {
                __items = Marshaler<T>.CreateMarshalerArray(items);
                (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.ReplaceAll_11(ThisPtr, __items_length, __items_data));
            }
            finally
            {
                Marshaler<T>.DisposeMarshalerArray(__items);
            }
        }

        unsafe uint global::Windows.Foundation.Collections.IVector<T>.Size
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
                var ThisPtr = _obj.ThisPtr;
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetSize_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        int global::System.Collections.Generic.ICollection<T>.Count
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
                return IListMethods<T>.get_Count(_obj);
            }
        }

        bool global::System.Collections.Generic.ICollection<T>.IsReadOnly
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
                return IListMethods<T>.get_IsReadOnly(_obj);
            }
        }

        T global::System.Collections.Generic.IList<T>.this[int index] 
        {
            get
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
                return IListMethods<T>.Indexer_Get(_obj, index);
            } 
            set
            {
                var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
                IListMethods<T>.Indexer_Set(_obj, index, value);
            }
        }

        int global::System.Collections.Generic.IList<T>.IndexOf(T item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            return IListMethods<T>.IndexOf(_obj, item);
        }

        void global::System.Collections.Generic.IList<T>.Insert(int index, T item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            IListMethods<T>.Insert(_obj, index, item);
        }

        void global::System.Collections.Generic.IList<T>.RemoveAt(int index)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            IListMethods<T>.RemoveAt(_obj, index);
        }

        void global::System.Collections.Generic.ICollection<T>.Add(T item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            IListMethods<T>.Add(_obj, item);
        }

        void global::System.Collections.Generic.ICollection<T>.Clear()
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            IListMethods<T>.Clear(_obj);
        }

        bool global::System.Collections.Generic.ICollection<T>.Contains(T item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            return IListMethods<T>.Contains(_obj, item);
        }

        void global::System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            IListMethods<T>.CopyTo(_obj, array, arrayIndex);
        }

        bool global::System.Collections.Generic.ICollection<T>.Remove(T item)
        {
            var _obj = ((ObjectReference<Vftbl>)((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle));
            return IListMethods<T>.Remove(_obj, item);
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
    static class IList_Delegates
    {
        public unsafe delegate int GetView_2(IntPtr thisPtr, out IntPtr __return_value__);
        public unsafe delegate int RemoveAt_6(IntPtr thisPtr, uint index);
        public unsafe delegate int RemoveAtEnd_8(IntPtr thisPtr);
        public unsafe delegate int Clear_9(IntPtr thisPtr);
        public unsafe delegate int GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);
        public unsafe delegate int ReplaceAll_11(IntPtr thisPtr, int __itemsSize, IntPtr items);
    }
}
