// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
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
            global::System.Threading.Interlocked.CompareExchange(ref __iReadOnlyListObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IReadOnlyListMethods<T>.PIID), null);
            return __iReadOnlyListObjRef;
        }
        private IObjectReference iReadOnlyListObjRef => __iReadOnlyListObjRef ?? Make_IListObjRef();

        private volatile IObjectReference __iEnumerableObjRef;
        private IObjectReference Make_IEnumerableObjRef()
        {
            global::System.Threading.Interlocked.CompareExchange(ref __iEnumerableObjRef, _inner.As<IUnknownVftbl>(ABI.System.Collections.Generic.IEnumerableMethods<T>.PIID), null);
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
    using global::System.Diagnostics.CodeAnalysis;
    using global::System.Runtime.CompilerServices;

    internal static class IVectorViewMethods<T>
    {
        // These function pointers will be set by IReadOnlyListMethods<T,TAbi>
        // when it is called by the source generated type or by the fallback
        // mechanism if the source generated type wasn't used.
        internal volatile unsafe static delegate*<IObjectReference, uint, T> _GetAt;
        internal volatile unsafe static delegate*<IObjectReference, T, out uint, bool> _IndexOf;
        internal volatile unsafe static delegate*<IObjectReference, uint, T[], uint> _GetMany;
        internal volatile static bool _RcwHelperInitialized;

        internal static unsafe void EnsureInitialized()
        {
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                // Simple invocation guarded by a direct runtime feature check to help the linker.
                // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
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
                        var initRcwHelperFallback = (Func<bool>)typeof(ABI.System.Collections.Generic.IReadOnlyListMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                            GetMethod("InitRcwHelperFallback", BindingFlags.NonPublic | BindingFlags.Static).
                            CreateDelegate(typeof(Func<bool>));
                        initRcwHelperFallback();
                    }
                }
            }

            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                if (!_RcwHelperInitialized)
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static void ThrowNotInitialized()
                    {
                        throw new NotImplementedException(
                            $"'{typeof(global::System.Collections.Generic.IReadOnlyList<T>)}' was called without initializing the RCW methods using 'IReadOnlyListMethods.InitRcwHelper'. " +
                            $"If using IDynamicCastableInterface support to do a dynamic cast to this interface, ensure InitRcwHelper is called.");
                    }

                    ThrowNotInitialized();
                }
            }
        }

        public static unsafe T GetAt(IObjectReference obj, uint index)
        {
            EnsureInitialized();
            return _GetAt(obj, index);
        }

        public static unsafe uint get_Size(IObjectReference obj)
        {
            var ThisPtr = obj.ThisPtr;

            uint __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
            GC.KeepAlive(obj);
            return __retval;
        }

        public static unsafe bool IndexOf(IObjectReference obj, T value, out uint index)
        {
            EnsureInitialized();
            return _IndexOf(obj, value, out index);
        }

        public static unsafe uint GetMany(IObjectReference obj, uint startIndex, ref T[] items)
        {
            // Early return to ensure things are trimmed correctly on NAOT.
            // See https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                EnsureInitialized();
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
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>**)ThisPtr)[9](ThisPtr, startIndex, __items_length, __items_data, &__retval));
                    GC.KeepAlive(obj);
                    items = Marshaler<T>.FromAbiArray((__items_length, __items_data));
                    return __retval;
                }
                finally
                {
                    Marshaler<T>.DisposeMarshalerArray(__items);
                }
            }
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
    static class IReadOnlyListMethods<T>
    {
        static IReadOnlyListMethods()
        {
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IReadOnlyList<T>), typeof(global::ABI.System.Collections.Generic.IReadOnlyList<T>));
        }

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

        private static IntPtr abiToProjectionVftablePtr;
        public static IntPtr AbiToProjectionVftablePtr => abiToProjectionVftablePtr;

        internal static bool TryInitCCWVtable(IntPtr ptr)
        {
            return global::System.Threading.Interlocked.CompareExchange(ref abiToProjectionVftablePtr, ptr, IntPtr.Zero) == IntPtr.Zero;
        }

        public static T Abi_GetAt_0(IntPtr thisPtr, uint index)
        {
            return IReadOnlyList<T>.FindAdapter(thisPtr).GetAt(index);
        }

        public static bool Abi_IndexOf_2(IntPtr thisPtr, T value, out uint index)
        {
            return IReadOnlyList<T>.FindAdapter(thisPtr).IndexOf(value, out index);
        }

        public static uint Abi_GetMany_3(IntPtr thisPtr, uint startIndex, ref T[] items)
        {
            return IReadOnlyList<T>.FindAdapter(thisPtr).GetMany(startIndex, ref items);
        }

        public static uint Abi_get_Size_1(IntPtr thisPtr)
        {
            return IReadOnlyList<T>.FindAdapter(thisPtr).Size;
        }

        internal static readonly Guid PIID = GuidGenerator.CreateIIDUnsafe(typeof(IReadOnlyList<T>));
        public static Guid IID => PIID;
    }

#if EMBED
    internal
#else
    public
#endif
    static class IReadOnlyListMethods<T,TAbi> where TAbi: unmanaged
    {
        public unsafe static bool InitRcwHelper(
            delegate*<IObjectReference, uint, T> getAt,
            delegate*<IObjectReference, T, out uint, bool> indexOf,
            delegate*<IObjectReference, uint, T[], uint> getMany)
        {
            if (ABI.Windows.Foundation.Collections.IVectorViewMethods<T>._RcwHelperInitialized)
            {
                return true;
            }

            ABI.Windows.Foundation.Collections.IVectorViewMethods<T>._GetAt = getAt;
            ABI.Windows.Foundation.Collections.IVectorViewMethods<T>._IndexOf = indexOf;
            ABI.Windows.Foundation.Collections.IVectorViewMethods<T>._GetMany = getMany;

            ComWrappersSupport.RegisterTypedRcwFactory(
                typeof(global::System.Collections.Generic.IReadOnlyList<T>),
                IReadOnlyListImpl<T>.CreateRcw);
            ComWrappersSupport.RegisterHelperType(typeof(global::System.Collections.Generic.IReadOnlyList<T>), typeof(global::ABI.System.Collections.Generic.IReadOnlyList<T>));

            ABI.Windows.Foundation.Collections.IVectorViewMethods<T>._RcwHelperInitialized = true;
            return true;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private unsafe static bool InitRcwHelperFallback()
        {
            return InitRcwHelper(&GetAtDynamic, &IndexOfDynamic, null);
        }

        private unsafe static T GetAtDynamic(IObjectReference obj, uint index)
        {
            var ThisPtr = obj.ThisPtr;
            TAbi result = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, void*, int>**)ThisPtr)[6](ThisPtr, index, &result));
                GC.KeepAlive(obj);
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
                GC.KeepAlive(obj);
                GC.KeepAlive(__params);
                index = _index;
                return found != 0;
            }
            finally
            {
                Marshaler<T>.DisposeMarshaler(__value);
            }
        }

        public static unsafe bool InitCcw(
            delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi*, int> getAt,
            delegate* unmanaged[Stdcall]<IntPtr, uint*, int> getSize,
            delegate* unmanaged[Stdcall]<IntPtr, TAbi, uint*, byte*, int> indexOf,
            delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int> getMany)
        {
            if (IReadOnlyListMethods<T>.AbiToProjectionVftablePtr != default)
            {
                return false;
            }

            var abiToProjectionVftablePtr = (IntPtr)NativeMemory.AllocZeroed((nuint)(sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 4));
            *(IInspectable.Vftbl*)abiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi*, int>*)abiToProjectionVftablePtr)[6] = getAt;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)abiToProjectionVftablePtr)[7] = getSize;
            ((delegate* unmanaged[Stdcall]<IntPtr, TAbi, uint*, byte*, int>*)abiToProjectionVftablePtr)[8] = indexOf;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>*)abiToProjectionVftablePtr)[9] = getMany;
            
            if (!IReadOnlyListMethods<T>.TryInitCCWVtable(abiToProjectionVftablePtr))
            {
                NativeMemory.Free((void*)abiToProjectionVftablePtr);
                return false;
            }

            return true;
        }

        private static global::System.Delegate[] DelegateCache;

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        internal static unsafe void InitFallbackCCWVtable()
        {
            Type getAt_0_type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(uint), typeof(TAbi*), typeof(int) });

            DelegateCache = new global::System.Delegate[]
            {
                global::System.Delegate.CreateDelegate(getAt_0_type, typeof(IReadOnlyListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_GetAt_0), BindingFlags.NonPublic | BindingFlags.Static)),
                new _get_PropertyAsUInt32_Abi(Do_Abi_get_Size_1),
                global::System.Delegate.CreateDelegate(DelegateHelper.IndexOf_2_Type, typeof(IReadOnlyListMethods<T,TAbi>).GetMethod(nameof(Do_Abi_IndexOf_2), BindingFlags.NonPublic | BindingFlags.Static)),
                new IReadOnlyList_Delegates.GetMany_3_Abi(Do_Abi_GetMany_3)
            };

            InitCcw(
                (delegate* unmanaged[Stdcall]<IntPtr, uint, TAbi*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[0]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[1]),
                (delegate* unmanaged[Stdcall]<IntPtr, TAbi, uint*, byte*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[2]),
                (delegate* unmanaged[Stdcall]<IntPtr, uint, int, IntPtr, uint*, int>)Marshal.GetFunctionPointerForDelegate(DelegateCache[3])
            );
        }

        private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, TAbi* __return_value__)
        {
            T ____return_value__ = default;
            *__return_value__ = default;
            try
            {
                ____return_value__ = IReadOnlyList<T>.FindAdapter(thisPtr).GetAt(index);
                *__return_value__ = (TAbi)Marshaler<T>.FromManaged(____return_value__);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        private static unsafe int Do_Abi_IndexOf_2(IntPtr thisPtr, TAbi value, uint* index, byte* __return_value__)
        {
            bool ____return_value__ = default;

            *index = default;
            *__return_value__ = default;
            uint __index = default;

            try
            {
                ____return_value__ = IReadOnlyList<T>.FindAdapter(thisPtr).IndexOf(Marshaler<T>.FromAbi(value), out __index);
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

        private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, uint* __return_value__)
        {
            uint ____return_value__ = default;

            *__return_value__ = default;
            T[] __items = Marshaler<T>.FromAbiArray((__itemsSize, items));

            try
            {
                ____return_value__ = IReadOnlyList<T>.FindAdapter(thisPtr).GetMany(startIndex, ref __items);
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

        private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
        {
            uint ____return_value__ = default;

            *__return_value__ = default;

            try
            {
                ____return_value__ = IReadOnlyList<T>.FindAdapter(thisPtr).Size;
                *__return_value__ = ____return_value__;

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

#if NET8_0_OR_GREATER
        [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
        private sealed class DelegateHelper
        {
            internal static Type IndexOf_2_Type = Projections.GetAbiDelegateType(new Type[] { typeof(IntPtr), typeof(TAbi), typeof(uint*), typeof(byte*), typeof(int) });

            private readonly IntPtr _ptr;

            private Delegate _indexOfDelegate;
            public Delegate IndexOf => _indexOfDelegate ?? GenericDelegateHelper.CreateDelegate(_ptr, ref _indexOfDelegate, IndexOf_2_Type, 8);

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

    [DynamicInterfaceCastableImplementation]
    [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
#pragma warning disable CA2256 // Not implementing IVectorView<T> for [DynamicInterfaceCastableImplementation], as we don't expect to need IDIC for WinRT types
    interface IReadOnlyList<T> : global::System.Collections.Generic.IReadOnlyList<T>, global::Windows.Foundation.Collections.IVectorView<T>
#pragma warning restore CA2256
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.Generic.IReadOnlyList<T> obj) =>
            obj is null ? null : ComWrappersSupport.CreateCCWForObject<IUnknownVftbl>(obj, PIID);

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

#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public sealed class ToAbiHelper : global::Windows.Foundation.Collections.IVectorView<T>
#pragma warning restore CA2257
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

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IReadOnlyList()
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
                [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = AttributeMessages.AbiTypesNeverHaveConstructors)]
#endif
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void InitFallbackCCWVTableIfNeeded()
                {
                    if (IReadOnlyListMethods<T>.AbiToProjectionVftablePtr == default)
                    {
                        // Handle the compat scenario where the source generator wasn't used or IDIC was used.
                        var initFallbackCCWVtable = (Action)typeof(IReadOnlyListMethods<,>).MakeGenericType(typeof(T), Marshaler<T>.AbiType).
                            GetMethod("InitFallbackCCWVtable", BindingFlags.NonPublic | BindingFlags.Static).
                            CreateDelegate(typeof(Action));
                        initFallbackCCWVtable();
                    }
                }
            }

            AbiToProjectionVftablePtr = IReadOnlyListMethods<T>.AbiToProjectionVftablePtr;
        }

        // This is left here for backwards compat purposes where older generated
        // projections can be using FindVftblType and using this to cast.
        [Guid("BBE1FA4C-B0E3-4583-BAEF-1F1B2E483E56")]
#pragma warning disable CA2257 // This member is a type (so it cannot be invoked)
        public unsafe struct Vftbl
#pragma warning restore CA2257
        {
            internal IInspectable.Vftbl IInspectableVftbl;

            public static readonly IntPtr AbiToProjectionVftablePtr = ABI.System.Collections.Generic.IReadOnlyList<T>.AbiToProjectionVftablePtr;

            public static readonly Guid PIID = ABI.System.Collections.Generic.IReadOnlyList<T>.PIID;
        }

        private static readonly ConditionalWeakTable<global::System.Collections.Generic.IReadOnlyList<T>, ToAbiHelper> _adapterTable = new();

        internal static ToAbiHelper FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.Generic.IReadOnlyList<T>>(thisPtr);
            return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
        }

        public static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr, PIID);
        }

        public static readonly Guid PIID = IReadOnlyListMethods<T>.PIID;

        int global::System.Collections.Generic.IReadOnlyCollection<T>.Count
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyList<T>).TypeHandle);
                return IReadOnlyListMethods<T>.get_Count(_obj);
            }
        }

        T global::System.Collections.Generic.IReadOnlyList<T>.this[int index]
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.Generic.IReadOnlyList<T>).TypeHandle);
                return IReadOnlyListMethods<T>.Indexer_Get(_obj, index);
            }
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
    static class IReadOnlyList_Delegates
    {
        public unsafe delegate int GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, out uint __return_value__);

        internal unsafe delegate int GetMany_3_Abi(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, uint* __return_value__);
    }
}
