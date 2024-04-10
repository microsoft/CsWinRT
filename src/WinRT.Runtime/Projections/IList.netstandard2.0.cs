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

#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.Foundation.Collections
{

    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
#if EMBED
    internal
#endif
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

namespace ABI.System.Collections.Generic
{
    using global::System;
    using global::System.Runtime.CompilerServices;

    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
#if EMBED
    internal
#else
    public
#endif
    class IList<T> : global::System.Collections.Generic.IList<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IList<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<Vftbl>(obj, GuidGenerator.GetIID(typeof(IList<T>)));

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IList<T> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, GuidGenerator.GetIID(typeof(IList<T>)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Generic.IList<T> FromAbi(IntPtr thisPtr) =>
            thisPtr == IntPtr.Zero ? null : new IList<T>(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.Generic.IList<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IVector<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList<T>));

        public class FromAbiHelper : global::System.Collections.Generic.IList<T>
        {
            private readonly global::ABI.System.Collections.Generic.IList<T> _vector;
            private readonly global::ABI.System.Collections.Generic.IEnumerable<T> _enumerable;

            public FromAbiHelper(IObjectReference obj) :
                this(new global::ABI.System.Collections.Generic.IList<T>(obj))
            {
            }

            public FromAbiHelper(global::ABI.System.Collections.Generic.IList<T> vector)
            {
                _vector = vector;
                _enumerable = new ABI.System.Collections.Generic.IEnumerable<T>(vector.ObjRef);
            }

            public int Count
            {
                get
                {
                    uint size = _vector.Size;
                    if (((uint)int.MaxValue) < size)
                    {
                        throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                    }

                    return (int)size;
                }
            }

            public bool IsReadOnly { get => false; }

            public void Add(T item) => _vector.Append(item);

            public void Clear() => _vector._Clear();

            public bool Contains(T item) => _vector.IndexOf(item, out _);

            public void CopyTo(T[] array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if (arrayIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                if (array.Length <= arrayIndex && Count > 0)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_IndexOutOfArrayBounds);

                if (array.Length - arrayIndex < Count)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);


                int count = Count;
                for (int i = 0; i < count; i++)
                {
                    array[i + arrayIndex] = FromAbiHelper.GetAt(_vector, (uint)i);
                }
            }

            public bool Remove(T item)
            {
                uint index;
                bool exists = _vector.IndexOf(item, out index);

                if (!exists)
                    return false;

                if (((uint)int.MaxValue) < index)
                {
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                FromAbiHelper.RemoveAtHelper(_vector, index);
                return true;
            }

            public T this[int index] { get => Indexer_Get(index); set => Indexer_Set(index, value); }

            private T Indexer_Get(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return GetAt(_vector, (uint)index);
            }

            private void Indexer_Set(int index, T value)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                SetAt(_vector, (uint)index, value);
            }

            public int IndexOf(T item)
            {
                uint index;
                bool exists = _vector.IndexOf(item, out index);

                if (!exists)
                    return -1;

                if (((uint)int.MaxValue) < index)
                {
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)index;
            }

            public void Insert(int index, T item)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                InsertAtHelper(_vector, (uint)index, item);
            }

            public void RemoveAt(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                RemoveAtHelper(_vector, (uint)index);
            }

            internal static T GetAt(global::ABI.System.Collections.Generic.IList<T> _this, uint index)
            {
                try
                {
                    return _this.GetAt(index);

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

            private static void SetAt(global::ABI.System.Collections.Generic.IList<T> _this, uint index, T value)
            {
                try
                {
                    _this.SetAt(index, value);

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

            private static void InsertAtHelper(global::ABI.System.Collections.Generic.IList<T> _this, uint index, T item)
            {
                try
                {
                    _this.InsertAt(index, item);

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

            internal static void RemoveAtHelper(global::ABI.System.Collections.Generic.IList<T> _this, uint index)
            {
                try
                {
                    _this.RemoveAt(index);

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

            public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();

            global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IVector<T>
        {
            private global::System.Collections.Generic.IList<T> _list;

            public ToAbiHelper(global::System.Collections.Generic.IList<T> list) => _list = list;

            global::System.Collections.Generic.IEnumerator<T> global::Windows.Foundation.Collections.IIterable<T>.First() =>
                _list.GetEnumerator();

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
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, WinRTRuntimeErrorStrings.ArgumentOutOfRange_Index);
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
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, WinRTRuntimeErrorStrings.ArgumentOutOfRange_Index);
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
                    Exception e = new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CannotRemoveLastFromEmptyCollection);
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
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public global::System.Delegate GetAt_0;
            internal _get_PropertyAsUInt32 get_Size_1;
            public IList_Delegates.GetView_2 GetView_2;
            public global::System.Delegate IndexOf_3;
            public global::System.Delegate SetAt_4;
            public global::System.Delegate InsertAt_5;
            public IList_Delegates.RemoveAt_6 RemoveAt_6;
            public global::System.Delegate Append_7;
            public IList_Delegates.RemoveAtEnd_8 RemoveAtEnd_8;
            public IList_Delegates.Clear_9 Clear_9;
            public IList_Delegates.GetMany_10 GetMany_10;
            public IList_Delegates.ReplaceAll_11 ReplaceAll_11;
            public static Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IList<T>));
            private static readonly Type GetAt_0_Type = Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType.MakeByRefType(), typeof(int) });
            private static readonly Type IndexOf_3_Type = Projections.GetAbiDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) });
            private static readonly Type SetAt_4_Type = Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType, typeof(int) });
            private static readonly Type InsertAt_5_Type = Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(uint), Marshaler<T>.AbiType, typeof(int) });
            private static readonly Type Append_7_Type = Projections.GetAbiDelegateType(new Type[] { typeof(void*), Marshaler<T>.AbiType, typeof(int) });

            internal unsafe Vftbl(IntPtr thisPtr)
            {
                var vftblPtr = *(void***)thisPtr;
                var vftbl = (IntPtr*)vftblPtr;
                IInspectableVftbl = *(IInspectable.Vftbl*)vftblPtr;
                GetAt_0 = Marshal.GetDelegateForFunctionPointer(vftbl[6], GetAt_0_Type);
                get_Size_1 = Marshal.GetDelegateForFunctionPointer<_get_PropertyAsUInt32>(vftbl[7]);
                GetView_2 = Marshal.GetDelegateForFunctionPointer<IList_Delegates.GetView_2>(vftbl[8]);
                IndexOf_3 = Marshal.GetDelegateForFunctionPointer(vftbl[9], IndexOf_3_Type);
                SetAt_4 = Marshal.GetDelegateForFunctionPointer(vftbl[10], SetAt_4_Type);
                InsertAt_5 = Marshal.GetDelegateForFunctionPointer(vftbl[11], InsertAt_5_Type);
                RemoveAt_6 = Marshal.GetDelegateForFunctionPointer<IList_Delegates.RemoveAt_6>(vftbl[12]);
                Append_7 = Marshal.GetDelegateForFunctionPointer(vftbl[13], Append_7_Type);
                RemoveAtEnd_8 = Marshal.GetDelegateForFunctionPointer<IList_Delegates.RemoveAtEnd_8>(vftbl[14]);
                Clear_9 = Marshal.GetDelegateForFunctionPointer<IList_Delegates.Clear_9>(vftbl[15]);
                GetMany_10 = Marshal.GetDelegateForFunctionPointer<IList_Delegates.GetMany_10>(vftbl[16]);
                ReplaceAll_11 = Marshal.GetDelegateForFunctionPointer<IList_Delegates.ReplaceAll_11>(vftbl[17]);
            }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = global::System.Delegate.CreateDelegate(GetAt_0_Type, typeof(Vftbl).GetMethod("Do_Abi_GetAt_0", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    get_Size_1 = Do_Abi_get_Size_1,
                    GetView_2 = Do_Abi_GetView_2,
                    IndexOf_3 = global::System.Delegate.CreateDelegate(IndexOf_3_Type, typeof(Vftbl).GetMethod("Do_Abi_IndexOf_3", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    SetAt_4 = global::System.Delegate.CreateDelegate(SetAt_4_Type, typeof(Vftbl).GetMethod("Do_Abi_SetAt_4", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    InsertAt_5 = global::System.Delegate.CreateDelegate(InsertAt_5_Type, typeof(Vftbl).GetMethod("Do_Abi_InsertAt_5", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    RemoveAt_6 = Do_Abi_RemoveAt_6,
                    Append_7 = global::System.Delegate.CreateDelegate(Append_7_Type, typeof(Vftbl).GetMethod("Do_Abi_Append_7", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(Marshaler<T>.AbiType)),
                    RemoveAtEnd_8 = Do_Abi_RemoveAtEnd_8,
                    Clear_9 = Do_Abi_Clear_9,
                    GetMany_10 = Do_Abi_GetMany_10,
                    ReplaceAll_11 = Do_Abi_ReplaceAll_11
                };
                var nativeVftbl = (IntPtr*)Marshal.AllocCoTaskMem(Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 12);
                Marshal.StructureToPtr(AbiToProjectionVftable.IInspectableVftbl, (IntPtr)nativeVftbl, false);
                nativeVftbl[6] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetAt_0);
                nativeVftbl[7] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.get_Size_1);
                nativeVftbl[8] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetView_2);
                nativeVftbl[9] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.IndexOf_3);
                nativeVftbl[10] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.SetAt_4);
                nativeVftbl[11] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.InsertAt_5);
                nativeVftbl[12] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.RemoveAt_6);
                nativeVftbl[13] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Append_7);
                nativeVftbl[14] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.RemoveAtEnd_8);
                nativeVftbl[15] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.Clear_9);
                nativeVftbl[16] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.GetMany_10);
                nativeVftbl[17] = Marshal.GetFunctionPointerForDelegate(AbiToProjectionVftable.ReplaceAll_11);

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

        public static implicit operator IList<T>(IObjectReference obj) => (obj != null) ? new IList<T>(obj) : null;
        public static implicit operator IList<T>(ObjectReference<Vftbl> obj) => (obj != null) ? new IList<T>(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }

        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IList(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public IList(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _FromVector = new FromAbiHelper(this);
        }
        FromAbiHelper _FromVector;


        public unsafe T GetAt(uint index)
        {
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

        internal unsafe global::Windows.Foundation.Collections.IVectorView<T> GetView()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_2(ThisPtr, out __retval));
                return MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(__retval);
            }
        }

        public unsafe bool IndexOf(T value, out uint index)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, null, null, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
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

        public unsafe void SetAt(uint index, T value)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.SetAt_4.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void InsertAt(uint index, T value)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.InsertAt_5.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void RemoveAt(uint index)
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAt_6(ThisPtr, index));
        }

        public unsafe void Append(T value)
        {
            object __value = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                _obj.Vftbl.Append_7.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public unsafe void RemoveAtEnd()
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAtEnd_8(ThisPtr));
        }

        public unsafe void _Clear()
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Clear_9(ThisPtr));
        }

        public unsafe uint GetMany(uint startIndex, ref T[] items)
        {
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

        public unsafe void ReplaceAll(T[] items)
        {
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

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
                return __retval;
            }
        }

        public int Count => _FromVector.Count;
        public bool IsReadOnly => _FromVector.IsReadOnly;
        public T this[int index] { get => _FromVector[index]; set => _FromVector[index] = value; }
        public int IndexOf(T item) => _FromVector.IndexOf(item);
        public void Insert(int index, T item) => _FromVector.Insert(index, item);
        public void RemoveAt(int index) => _FromVector.RemoveAt(index);
        public void Add(T item) => _FromVector.Add(item);
        public void Clear() => _FromVector.Clear();
        public bool Contains(T item) => _FromVector.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _FromVector.CopyTo(array, arrayIndex);
        public bool Remove(T item) => _FromVector.Remove(item);
        public global::System.Collections.Generic.IEnumerator<T> GetEnumerator() => _FromVector.GetEnumerator();
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
