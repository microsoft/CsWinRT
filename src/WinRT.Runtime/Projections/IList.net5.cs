// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

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
            global::System.Threading.Interlocked.CompareExchange(ref __iListObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IListMethods<T>.PIID), null);
            return __iListObjRef;
        }
        private IObjectReference iListObjRef => __iListObjRef ?? Make_IListObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumerableMethods<T>.PIID), null);
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
    using global::System.Diagnostics.CodeAnalysis;
    using global::System.Runtime.CompilerServices;

#if EMBED
    internal
#else
    public
#endif
    static class IListMethods<T> 
    {
        unsafe static IListMethods()
        {
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IList<T>), typeof(global::ABI.System.Collections.Generic.IList<T>));

            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return;
            }

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            InitRcwHelperFallbackIfNeeded();
#pragma warning restore IL3050

#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
#if NET
            [SuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void InitRcwHelperFallbackIfNeeded()
            {
                // Handle the compat scenario where the source generator wasn't used and IDIC hasn't been used yet
                // and due to that the function pointers haven't been initialized.
                if (!IVectorMethods<T>._RcwHelperInitialized)
                {
                    var initRcwHelperFallback = (Func<bool>)typeof(IListMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                        GetMethod("InitRcwHelperFallback", BindingFlags.NonPublic | BindingFlags.Static).
                        CreateDelegate(typeof(Func<bool>));
                    initRcwHelperFallback();
                }
            }
        }

        public static int get_Count(IObjectReference obj)
        {
            uint size = IVectorMethods<T>.get_Size(obj);
            if (((uint)int.MaxValue) < size)
            {
                throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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
                throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_IndexOutOfArrayBounds);

            if (array.Length - arrayIndex < get_Count(obj))
                throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);


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
                throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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
                throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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

        private static IntPtr abiToProjectionVftablePtr;
        public static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
        }

        public static T Abi_GetAt_0(IntPtr thisPtr, uint index)
        {
            return IList<T>.FindAdapter(thisPtr).GetAt(index);
        }

        public static global::System.Collections.Generic.IReadOnlyList<T> Abi_GetView_2(IntPtr thisPtr)
        {
            return IList<T>.FindAdapter(thisPtr).GetView();
        }

        public static bool Abi_IndexOf_3(IntPtr thisPtr, T value, out uint index)
        {
            return IList<T>.FindAdapter(thisPtr).IndexOf(value, out index);
        }

        public static void Abi_SetAt_4(IntPtr thisPtr, uint index, T value)
        {
            IList<T>.FindAdapter(thisPtr).SetAt(index, value);
        }

        public static void Abi_InsertAt_5(IntPtr thisPtr, uint index, T value)
        {
            IList<T>.FindAdapter(thisPtr).InsertAt(index, value);
        }

        public static void Abi_RemoveAt_6(IntPtr thisPtr, uint index)
        {
            IList<T>.FindAdapter(thisPtr).RemoveAt(index);
        }

        public static void Abi_Append_7(IntPtr thisPtr, T value)
        {
            IList<T>.FindAdapter(thisPtr).Append(value);
        }

        public static void Abi_RemoveAtEnd_8(IntPtr thisPtr)
        {
            IList<T>.FindAdapter(thisPtr).RemoveAtEnd();
        }

        public static void Abi_Clear_9(IntPtr thisPtr)
        {
            IList<T>.FindAdapter(thisPtr)._Clear();
        }

        public static uint Abi_GetMany_10(IntPtr thisPtr, uint startIndex, ref T[] items)
        {
            return IList<T>.FindAdapter(thisPtr).GetMany(startIndex, ref items);
        }

        public static void Abi_ReplaceAll_11(IntPtr thisPtr, T[] items)
        {
            IList<T>.FindAdapter(thisPtr).ReplaceAll(items);
        }

        public static uint Abi_get_Size_1(IntPtr thisPtr)
        {
            return IList<T>.FindAdapter(thisPtr).Size;
        }

        internal readonly static Guid PIID = GuidGenerator.CreateIID(typeof(IList<T>));
        public static Guid IID => PIID;

        internal unsafe static bool EnsureEnumerableInitialized()
        {
            return IVectorMethods<T>._EnsureEnumerableInitialized();
        }
    }

#if EMBED
    internal
#else
    public
#endif
    static class IListMethods<T, TAbi> where TAbi : unmanaged
    {
        public unsafe static bool InitRcwHelper(
            delegate*<IObjectReference, uint, T> getAt,
            delegate*<IObjectReference, global::System.Collections.Generic.IReadOnlyList<T>> getView,
            delegate*<IObjectReference, T, out uint, bool> indexOf,
            delegate*<IObjectReference, uint, T, void> setAt,
            delegate*<IObjectReference, uint, T, void> insertAt,
            delegate*<IObjectReference, T, void> append,
            delegate*<IObjectReference, uint, T[], uint> getMany,
            delegate*<IObjectReference, T[], void> replaceAll)
        {
            if (IVectorMethods<T>._RcwHelperInitialized)
            {
                return true;
            }

            IVectorMethods<T>._GetAt = getAt;
            IVectorMethods<T>._GetView = getView;
            IVectorMethods<T>._IndexOf = indexOf;
            IVectorMethods<T>._SetAt = setAt;
            IVectorMethods<T>._InsertAt = insertAt;
            IVectorMethods<T>._Append = append;
            IVectorMethods<T>._GetMany = getMany;
            IVectorMethods<T>._ReplaceAll = replaceAll;

            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.IList<T>),
                IListImpl<T>.CreateRcw);
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IList<T>), typeof(global::ABI.System.Collections.Generic.IList<T>));

            IVectorMethods<T>._RcwHelperInitialized = true;
            return true;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private unsafe static bool InitRcwHelperFallback()
        {
            return InitRcwHelper(
                &GetAtDynamic,
                null,
                &IndexOfDynamic,
                &SetAtDynamic,
                &InsertAtDynamic,
                &AppendDynamic,
                null,
                null);
        }

        private static unsafe T GetAtDynamic(IObjectReference obj, uint index)
        {
            var ThisPtr = obj.ThisPtr;
            TAbi result = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, void*, int>**)ThisPtr)[6](ThisPtr, index, &result));
                return Marshaler<T>.FromAbi(result);
            }
            finally
            {
                Marshaler<T>.DisposeAbi(result);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe bool IndexOfDynamic(IObjectReference obj, T value, out uint index)
        {
            var ThisPtr = obj.ThisPtr;
            byte found;
            uint _index;
            object __value = default;
            var __params = new object[] { ThisPtr, null, (IntPtr)(void*)&_index, (IntPtr)(void*)&found };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                DelegateHelper.Get(obj).IndexOf.DynamicInvokeAbi(__params);
                index = _index;
                return found != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe void SetAtDynamic(IObjectReference obj, uint index, T value)
        {
            var ThisPtr = obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                DelegateHelper.Get(obj).SetAt.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe void InsertAtDynamic(IObjectReference obj, uint index, T value)
        {
            var ThisPtr = obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, index, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[2] = Marshaler<T>.GetAbi(__value);
                DelegateHelper.Get(obj).InsertAt.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static unsafe void AppendDynamic(IObjectReference obj, T value)
        {
            var ThisPtr = obj.ThisPtr;
            object __value = default;
            var __params = new object[] { ThisPtr, null };
            try
            {
                __value = Marshaler<T>.CreateMarshaler2(value);
                __params[1] = Marshaler<T>.GetAbi(__value);
                DelegateHelper.Get(obj).Append.DynamicInvokeAbi(__params);
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi*, int> getAt,
            delegate* unmanaged[Stdcall]<IntPtr, uint*, int> getSize,
            delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> getView,
            delegate* unmanaged[Stdcall]<IntPtr, TAbi, uint*, byte*, int> indexOf,
            delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi, int> setAt,
            delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi, int> insertAt,
            delegate* unmanaged[Stdcall]<IntPtr, uint, int> removeAt,
            delegate* unmanaged[Stdcall]<IntPtr, TAbi, int> append,
            delegate* unmanaged[Stdcall]<IntPtr, int> removeAtEnd,
            delegate* unmanaged[Stdcall]<IntPtr, int> clear,
            delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int> getMany,
            delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int> replaceAll)
        {
            if (IListMethods<T>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 12));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi*, int>*)abiToProjectionVftablePtr)[6] = getAt;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)abiToProjectionVftablePtr)[7] = getSize;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)abiToProjectionVftablePtr)[8] = getView;
            ((delegate* unmanaged[Stdcall]<IntPtr, TAbi, uint*, byte*, int>*)abiToProjectionVftablePtr)[9] = indexOf;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi, int>*)abiToProjectionVftablePtr)[10] = setAt;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi, int>*)abiToProjectionVftablePtr)[11] = insertAt;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, int>*)abiToProjectionVftablePtr)[12] = removeAt;
            ((delegate* unmanaged[Stdcall]<IntPtr, TAbi, int>*)abiToProjectionVftablePtr)[13] = append;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)abiToProjectionVftablePtr)[14] = removeAtEnd;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)abiToProjectionVftablePtr)[15] = clear;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>*)abiToProjectionVftablePtr)[16] = getMany;
            ((delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int>*)abiToProjectionVftablePtr)[17] = replaceAll;

            if (!IListMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
                return false;
            }

            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IReadOnlyList<T>), typeof(global::ABI.System.Collections.Generic.IReadOnlyList<T>));

            return true;
        }

        private static global::System.Delegate[] DelegateCache;

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        internal static unsafe void InitFallbackCCWVtable()
        {
            Type getAt_0_type = Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(uint), typeof(TAbi*), typeof(int) });

            DelegateCache = new global::System.Delegate[]
            {
                global::System.Delegate.CreateDelegate(getAt_0_type, typeof(IListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_GetAt_0), BindingFlags.NonPublic | BindingFlags.Static)),
                new _get_PropertyAsUInt32_Abi(Do_Abi_get_Size_1),
                new IList_Delegates.GetView_2_Abi(Do_Abi_GetView_2),
                global::System.Delegate.CreateDelegate(IndexOf_3_Type, typeof(IListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_IndexOf_3), BindingFlags.NonPublic | BindingFlags.Static)),
                global::System.Delegate.CreateDelegate(SetAtInsertAt_Type, typeof(IListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_SetAt_4), BindingFlags.NonPublic | BindingFlags.Static)),
                global::System.Delegate.CreateDelegate(SetAtInsertAt_Type, typeof(IListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_InsertAt_5), BindingFlags.NonPublic | BindingFlags.Static)),
                new IList_Delegates.RemoveAt_6(Do_Abi_RemoveAt_6),
                global::System.Delegate.CreateDelegate(Append_7_Type, typeof(IListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_Append_7), BindingFlags.NonPublic | BindingFlags.Static)),
                new IList_Delegates.RemoveAtEnd_8(Do_Abi_RemoveAtEnd_8),
                new IList_Delegates.Clear_9(Do_Abi_Clear_9),
                new IList_Delegates.GetMany_10_Abi(Do_Abi_GetMany_10),
                new IList_Delegates.ReplaceAll_11(Do_Abi_ReplaceAll_11)
            };

            InitCcw(
                (delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[0]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[1]),
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[2]),
                (delegate* unmanaged[Stdcall]<IntPtr, TAbi, uint*, byte*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[3]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[4]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[5]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[6]),
                (delegate* unmanaged[Stdcall]<IntPtr, TAbi, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[7]),
                (delegate* unmanaged[Stdcall]<IntPtr, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[8]),
                (delegate* unmanaged[Stdcall]<IntPtr, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[9]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[10]),
                (delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[11])
            );
        }

        private static unsafe int Do_Abi_GetAt_0(void* thisPtr, uint index, TAbi* __return_value__)
        {
            T ____return_value__ = default;
            *__return_value__ = default;
            try
            {
                ____return_value__ = IList<T>.FindAdapter(new IntPtr(thisPtr)).GetAt(index);
                *__return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, IntPtr* __return_value__)
        {
            global::System.Collections.Generic.IReadOnlyList<T> ____return_value__ = default;
            *__return_value__ = default;

            try
            {
                ____return_value__ = IList<T>.FindAdapter(thisPtr).GetView();
                *__return_value__ = MarshalInterface<global::System.Collections.Generic.IReadOnlyList<T>>.FromManaged(____return_value__);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_IndexOf_3(void* thisPtr, TAbi value, uint* index, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *index = default;
            *__return_value__ = default;
            uint __index = default;

            try
            {
                ____return_value__ = IList<T>.FindAdapter(new IntPtr(thisPtr)).IndexOf(Marshaler<T>.FromAbi(value), out __index);
                *index = __index;
                *__return_value__ = (byte)(____return_value__ ? 1 : 0);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_SetAt_4(void* thisPtr, uint index, TAbi value)
        {
            try
            {
                IList<T>.FindAdapter(new IntPtr(thisPtr)).SetAt(index, Marshaler<T>.FromAbi(value));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_InsertAt_5(void* thisPtr, uint index, TAbi value)
        {
            try
            {
                IList<T>.FindAdapter(new IntPtr(thisPtr)).InsertAt(index, Marshaler<T>.FromAbi(value));
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
                IList<T>.FindAdapter(thisPtr).RemoveAt(index);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_Append_7(void* thisPtr, TAbi value)
        {
            try
            {
                IList<T>.FindAdapter(new IntPtr(thisPtr)).Append(Marshaler<T>.FromAbi(value));
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
                IList<T>.FindAdapter(thisPtr).RemoveAtEnd();
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
                IList<T>.FindAdapter(thisPtr)._Clear();
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, uint* __return_value__)
        {
            uint ____return_value__ = default;

            *__return_value__ = default;
            T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

            try
            {
                ____return_value__ = IList<T>.FindAdapter(thisPtr).GetMany(startIndex, ref __items);
                Marshaler<T>.CopyManagedArray(__items, items);
                *__return_value__ = ____return_value__;

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
                IList<T>.FindAdapter(thisPtr).ReplaceAll(Marshaler<T>.FromAbiArray((__itemsSize, items)));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
        {
            uint ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IList<T>.FindAdapter(thisPtr).Size;
                *__return_value__ = ____return_value__;
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static Type _indexOf_3_type;
        private static Type IndexOf_3_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _indexOf_3_type ?? MakeIndexOfType();
        }


#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static Type MakeIndexOfType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _indexOf_3_type, Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(TAbi), typeof(uint*), typeof(byte*), typeof(int) }), null);
            return _indexOf_3_type;
        }

        private static Type _setAtInsertAt_Type;
        private static Type SetAtInsertAt_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _setAtInsertAt_Type ?? MakeSetAtInsertAtType();
        }


#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static Type MakeSetAtInsertAtType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _setAtInsertAt_Type, Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(uint), typeof(TAbi), typeof(int) }), null);
            return _setAtInsertAt_Type;
        }

        private static Type _append_7_Type;
        private static Type Append_7_Type
        {
#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
            get => _append_7_Type ?? MakeAppendType();
        }


#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private static Type MakeAppendType()
        {
            global::System.Threading.Interlocked.CompareExchange(ref _append_7_Type, Projections.GetAbiDelegateType(new Type[] { typeof(void*), typeof(TAbi), typeof(int) }), null);
            return _append_7_Type;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private sealed class DelegateHelper
        {
            private readonly IntPtr _ptr;

            private Delegate _indexOfDelegate;
            public Delegate IndexOf => _indexOfDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _indexOfDelegate, IndexOf_3_Type, 9);

            private Delegate _setAtDelegate;
            public Delegate SetAt => _setAtDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _setAtDelegate, SetAtInsertAt_Type, 10);

            private Delegate _insertAtDelegate;
            public Delegate InsertAt => _insertAtDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _insertAtDelegate, SetAtInsertAt_Type, 11);

            private Delegate _appendDelegate;
            public Delegate Append => _appendDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _appendDelegate, Append_7_Type, 13);

            private DelegateHelper(IntPtr ptr)
            {
                _ptr = ptr;
            }

            public static DelegateHelper Get(IObjectReference obj)
            {
                return (DelegateHelper)GenericDelegateHelper.DelegateTable.GetValue(obj, static (objRef) => new DelegateHelper(objRef.ThisPtr));
            }
        }
    }

    internal static class IVectorMethods<T>
    {
        // These function pointers will be set by IListMethods<T,TAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal volatile unsafe static delegate*<IObjectReference, uint, T> _GetAt;
        internal volatile unsafe static delegate*<IObjectReference, global::System.Collections.Generic.IReadOnlyList<T>> _GetView;
        internal volatile unsafe static delegate*<IObjectReference, T, out uint, bool> _IndexOf;
        internal volatile unsafe static delegate*<IObjectReference, uint, T, void> _SetAt;
        internal volatile unsafe static delegate*<IObjectReference, uint, T, void> _InsertAt;
        internal volatile unsafe static delegate*<IObjectReference, T, void> _Append;
        internal volatile unsafe static delegate*<IObjectReference, uint, T[], uint> _GetMany;
        internal volatile unsafe static delegate*<IObjectReference, T[], void> _ReplaceAll;
        internal volatile static bool _RcwHelperInitialized;
        internal volatile unsafe static delegate*<bool> _EnsureEnumerableInitialized;

        public static unsafe uint get_Size(IObjectReference obj)
        {
            IntPtr ThisPtr = obj.ThisPtr;
            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
            return __retval;
        }

        public static unsafe T GetAt(IObjectReference obj, uint index)
        {
            return _GetAt(obj, index);
        }

        public static unsafe global::System.Collections.Generic.IReadOnlyList<T> GetView(IObjectReference obj)
        {
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return _GetView(obj);
            }

            if (_GetView != null)
            {
                return _GetView(obj);
            }
            else
            {
                var ThisPtr = obj.ThisPtr;
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[8](ThisPtr, &__retval));
                    return MarshalInterface<global::System.Collections.Generic.IReadOnlyList<T>>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInterface<global::Windows.Foundation.Collections.IVectorView<T>>.DisposeAbi(__retval);
                }
            }
        }

        public static unsafe bool IndexOf(IObjectReference obj, T value, out uint index)
        {
            return _IndexOf(obj, value, out index);
        }

        public static unsafe void SetAt(IObjectReference obj, uint index, T value)
        {
            _SetAt(obj, index, value);
        }

        public static unsafe void InsertAt(IObjectReference obj, uint index, T value)
        {
            _InsertAt(obj, index, value);
        }

        public static unsafe void RemoveAt(IObjectReference obj, uint index)
        {
            var ThisPtr = obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, int>**)ThisPtr)[12](ThisPtr, index));
        }

        public static unsafe void Append(IObjectReference obj, T value)
        {
            _Append(obj, value);
        }

        public static unsafe void RemoveAtEnd(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[14](ThisPtr));
        }

        public static unsafe void Clear(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[15](ThisPtr));
        }

        public static unsafe uint GetMany(IObjectReference obj, uint startIndex, ref T[] items)
        {
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return _GetMany(obj, startIndex, items);
            }

            if (_GetMany != null)
            {
                return _GetMany(obj, startIndex, items);
            }
            else
            {
                var ThisPtr = obj.ThisPtr;
                object __items = default;
                int __items_length = default;
                IntPtr __items_data = default;
                uint __retval = default;
                try
                {
                    __items = Marshaler<T>.CreateMarshalerArray(items);
                    (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>**)ThisPtr)[16](ThisPtr, startIndex, __items_length, __items_data, &__retval));
                    items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                    return __retval;
                }
                finally
                {
                    Marshaler<T>.DisposeMarshalerArray(__items);
                }
            }
        }

        public static unsafe void ReplaceAll(IObjectReference obj, T[] items)
        {
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                _ReplaceAll(obj, items);

                return;
            }

            if (_ReplaceAll != null)
            {
                _ReplaceAll(obj, items);
            }
            else
            {
                var ThisPtr = obj.ThisPtr;
                object __items = default;
                int __items_length = default;
                IntPtr __items_data = default;
                try
                {
                    __items = Marshaler<T>.CreateMarshalerArray(items);
                    (__items_length, __items_data) = Marshaler<T>.GetAbiArray(__items);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, int>**)ThisPtr)[17](ThisPtr, __items_length, __items_data));
                }
                finally
                {
                    Marshaler<T>.DisposeMarshalerArray(__items);
                }
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
    interface IList<T> : global::System.Collections.Generic.IList<T>, global::Windows.Foundation.Collections.IVector<T>
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IList<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(obj, PIID);

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Generic.IList<T> obj) => 
            ComWrappersSupport.CreateCCWForObjectForMarshaling(obj, PIID);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static IntPtr FromManaged(global::System.Collections.Generic.IList<T> value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler2(value).Detach();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<global::Windows.Foundation.Collections.IVector<T>>.DisposeAbi(abi);

        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList<T>));

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IVector<T>
#pragma warning restore CA2257
        {
            private readonly global::System.Collections.Generic.IList<T> _list;

            public ToAbiHelper(global::System.Collections.Generic.IList<T> list) => _list = list;

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

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IList()
        {
            if (RuntimeFeature.IsDynamicCodeCompiled)
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
                [SuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void InitFallbackCCWVTableIfNeeded()
                {
                    if (IListMethods<T>.AbiToProjectionVftablePtr == default)
                    {
                        // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                        var initFallbackCCWVtable = (Action)typeof(IListMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                            GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                            CreateDelegate(typeof(Action));
                        initFallbackCCWVtable();
                    }
                }
            }

            AbiToProjectionVftablePtr = IListMethods<T>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("913337E9-11A1-4345-A3A2-4E7F956E222D")]
#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public unsafe struct Vftbl
#pragma warning restore CA2257
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.IList<T>.AbiToProjectionVftablePtr;

            public static Guid PIID = ABI.System.Collections.Generic.IList<T>.PIID;
        }

        private readonly static ConditionalWeakTable<global::System.Collections.Generic.IList<T>, ToAbiHelper> _adapterTable = new();

        internal static global::Windows.Foundation.Collections.IVector<T> FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IList<T>>(thisPtr);
            return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
        }

        public static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);
        }

        public static Guid PIID = IListMethods<T>.PIID;
        
        unsafe T global::Windows.Foundation.Collections.IVector<T>.GetAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IVectorMethods<T>.GetAt(_obj, index);
        }

        unsafe global::System.Collections.Generic.IReadOnlyList<T> global::Windows.Foundation.Collections.IVector<T>.GetView()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IVectorMethods<T>.GetView(_obj);
        }

        unsafe bool global::Windows.Foundation.Collections.IVector<T>.IndexOf(T value, out uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IVectorMethods<T>.IndexOf(_obj, value, out index);
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.SetAt(uint index, T value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.SetAt(_obj, index, value);
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.InsertAt(uint index, T value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.InsertAt(_obj, index, value);
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.RemoveAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.RemoveAt(_obj, index);
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.Append(T value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.Append(_obj, value);
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.RemoveAtEnd()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.RemoveAtEnd(_obj);
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>._Clear()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.Clear(_obj);
        }

        unsafe uint global::Windows.Foundation.Collections.IVector<T>.GetMany(uint startIndex, ref T[] items)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IVectorMethods<T>.GetMany(_obj, startIndex, ref items);           
        }

        unsafe void global::Windows.Foundation.Collections.IVector<T>.ReplaceAll(T[] items)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IVectorMethods<T>.ReplaceAll(_obj, items);
        }

        unsafe uint global::Windows.Foundation.Collections.IVector<T>.Size
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
                return IVectorMethods<T>.get_Size(_obj);
            }
        }

        int global::System.Collections.Generic.ICollection<T>.Count
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
                return IListMethods<T>.get_Count(_obj);
            }
        }

        bool global::System.Collections.Generic.ICollection<T>.IsReadOnly
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
                return IListMethods<T>.get_IsReadOnly(_obj);
            }
        }

        T global::System.Collections.Generic.IList<T>.this[int index] 
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
                return IListMethods<T>.Indexer_Get(_obj, index);
            } 
            set
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
                IListMethods<T>.Indexer_Set(_obj, index, value);
            }
        }

        int global::System.Collections.Generic.IList<T>.IndexOf(T item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IListMethods<T>.IndexOf(_obj, item);
        }

        void global::System.Collections.Generic.IList<T>.Insert(int index, T item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IListMethods<T>.Insert(_obj, index, item);
        }

        void global::System.Collections.Generic.IList<T>.RemoveAt(int index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IListMethods<T>.RemoveAt(_obj, index);
        }

        void global::System.Collections.Generic.ICollection<T>.Add(T item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IListMethods<T>.Add(_obj, item);
        }

        void global::System.Collections.Generic.ICollection<T>.Clear()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IListMethods<T>.Clear(_obj);
        }

        bool global::System.Collections.Generic.ICollection<T>.Contains(T item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IListMethods<T>.Contains(_obj, item);
        }

        void global::System.Collections.Generic.ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            IListMethods<T>.CopyTo(_obj, array, arrayIndex);
        }

        bool global::System.Collections.Generic.ICollection<T>.Remove(T item)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IList<T>).TypeHandle);
            return IListMethods<T>.Remove(_obj, item);
        }
        
        global::System.Collections.Generic.IEnumerator<T> global::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
        {
            ((IWinRTObject)this).IsInterfaceImplemented(typeof(global::System.Collections.Generic.IEnumerable<T>).TypeHandle, true);
            var _objEnumerable = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IEnumerable<T>).TypeHandle);
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
        internal unsafe delegate int GetView_2_Abi(IntPtr thisPtr, IntPtr* __return_value__);
        public unsafe delegate int RemoveAt_6(IntPtr thisPtr, uint index);
        public unsafe delegate int RemoveAtEnd_8(IntPtr thisPtr);
        public unsafe delegate int Clear_9(IntPtr thisPtr);
        public unsafe delegate int GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);
        internal unsafe delegate int GetMany_10_Abi(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, uint* __return_value__);
        public unsafe delegate int ReplaceAll_11(IntPtr thisPtr, int __itemsSize, IntPtr items);
    }
}
