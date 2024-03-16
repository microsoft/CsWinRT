// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Microsoft.UI.Xaml.Interop
{
    [global::WinRT.WindowsRuntimeType]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.System.Collections.IEnumerable))]
    internal interface IBindableIterable
    {
        IBindableIterator First();
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Interop.IBindableIterator))]
    internal interface IBindableIterator
    {
        bool MoveNext();
        // GetMany is not implemented by IBindableIterator, but it is here
        // for compat purposes with WinUI where there are scenarios they do
        // reinterpret_cast from IBindableIterator to IIterable<object>.  It is
        // the last function in the vftable and shouldn't be called by anyone.
        // If called, it will return NotImplementedException.
        uint GetMany(ref object[] items);
        object Current { get; }
        bool HasCurrent { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
    internal interface IBindableVector : IEnumerable
    {
        object GetAt(uint index);
        IBindableVectorView GetView();
        bool IndexOf(object value, out uint index);
        void SetAt(uint index, object value);
        void InsertAt(uint index, object value);
        void RemoveAt(uint index);
        void Append(object value);
        void RemoveAtEnd();
        void Clear();
        uint Size { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Interop.IBindableVectorView))]
    internal interface IBindableVectorView : IEnumerable
    {
        object GetAt(uint index);
        bool IndexOf(object value, out uint index);
        uint Size { get; }
    }
}

namespace ABI.Microsoft.UI.Xaml.Interop
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    internal unsafe class IBindableIterator : global::Microsoft.UI.Xaml.Interop.IBindableIterator
    {
        [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _get_Current_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_Current_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_Current_0; set => _get_Current_0 = value; }
            private void* _get_HasCurrent_1;
            public delegate* unmanaged[Stdcall]<IntPtr, byte*, int> get_HasCurrent_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)_get_HasCurrent_1; set => _get_HasCurrent_1 = value; }
            private void* _MoveNext_2;
            public delegate* unmanaged[Stdcall]<IntPtr, byte*, int> MoveNext_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, byte*, int>)_MoveNext_2; set => _MoveNext_2 = value; }
            // Note this may not be a valid address and should not be called.
            private void* _GetMany_3;
            public delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int> GetMany_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int>)_GetMany_3; set => _GetMany_3 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            private static readonly Delegate[] DelegateCache = new Delegate[4];

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _get_Current_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new IBindableIterator_Delegates.get_Current_0(Do_Abi_get_Current_0)).ToPointer(),
                    _get_HasCurrent_1 = Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IBindableIterator_Delegates.get_HasCurrent_1(Do_Abi_get_HasCurrent_1)).ToPointer(),
                    _MoveNext_2 = Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new IBindableIterator_Delegates.MoveNext_2(Do_Abi_MoveNext_2)).ToPointer(),
                    _GetMany_3 = Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new IBindableIterator_Delegates.GetMany_3(Do_Abi_GetMany_3)).ToPointer(),
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }


            private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, byte* result)
            {
                bool __result = default;
                *result = default;
                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableIterator>(thisPtr).MoveNext();
                    *result = (byte)(__result ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, uint* result)
            {
                *result = default;

                try
                {
                    // Should never be called.
                    throw new NotImplementedException();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
            }


            private static unsafe int Do_Abi_get_Current_0(IntPtr thisPtr, IntPtr* value)
            {
                object __value = default;
                *value = default;
                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableIterator>(thisPtr).Current;
                    *value = MarshalInspectable<object>.FromManaged(__value);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, byte* value)
            {
                bool __value = default;
                *value = default;
                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableIterator>(thisPtr).HasCurrent;
                    *value = (byte)(__value ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IBindableIterator(IObjectReference obj) => (obj != null) ? new IBindableIterator(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IBindableIterator(IObjectReference obj) : this(obj.As<Vftbl>()) {}
        internal IBindableIterator(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe bool MoveNext()
        {
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.MoveNext_2(ThisPtr, &__retval));
            return __retval != 0;
        }

        public unsafe object Current
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Current_0(ThisPtr, &__retval));
                    return MarshalInspectable<object>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInspectable<object>.DisposeAbi(__retval);
                }
            }
        }

        public unsafe bool HasCurrent
        {
            get
            {
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasCurrent_1(ThisPtr, &__retval));
                return __retval != 0;
            }
        }

        public unsafe uint GetMany(ref object[] items)
        {
            // Should never be called.
            throw new NotImplementedException();
        }

    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IBindableIterator_Delegates
    {
        public unsafe delegate int get_Current_0(IntPtr thisPtr, IntPtr* result);
        public unsafe delegate int get_HasCurrent_1(IntPtr thisPtr, byte* result);
        public unsafe delegate int MoveNext_2(IntPtr thisPtr, byte* result);
        public unsafe delegate int GetMany_3(IntPtr thisPtr, int itemSize, IntPtr items, uint* result);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
    internal unsafe class IBindableVectorView : global::Microsoft.UI.Xaml.Interop.IBindableVectorView
    {
        [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _GetAt_0;
            public delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int> GetAt_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)_GetAt_0; set => _GetAt_0 = value; }
            private void* _get_Size_1;
            public delegate* unmanaged[Stdcall]<IntPtr, uint*, int> get_Size_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)_get_Size_1; set => _get_Size_1 = value; }
            private void* _IndexOf_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int> IndexOf_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>)_IndexOf_2; set => _IndexOf_2 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            private static readonly Delegate[] DelegateCache = new Delegate[3];

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _GetAt_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new IBindableVectorView_Delegates.GetAt_0(Do_Abi_GetAt_0)).ToPointer(),
                    _get_Size_1 = Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IBindableVectorView_Delegates.get_Size_1(Do_Abi_get_Size_1)).ToPointer(),
                    _IndexOf_2 = Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new IBindableVectorView_Delegates.IndexOf_2(Do_Abi_IndexOf_2)).ToPointer(),

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 3);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }


            private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, IntPtr* result)
            {
                object __result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>(thisPtr).GetAt(index);
                    *result = MarshalInspectable<object>.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_IndexOf_2(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue)
            {
                bool __returnValue = default;

                *index = default;
                *returnValue = default;
                uint __index = default;

                try
                {
                    __returnValue = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>(thisPtr).IndexOf(MarshalInspectable<object>.FromAbi(value), out __index);
                    *index = __index;
                    *returnValue = (byte)(__returnValue ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* value)
            {
                uint __value = default;

                *value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>(thisPtr).Size;
                    *value = __value;

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IBindableVectorView(IObjectReference obj) => (obj != null) ? new IBindableVectorView(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IBindableVectorView(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IBindableVectorView(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _iterableToEnumerable = new ABI.System.Collections.IEnumerable.FromAbiHelper(ObjRef);
        }
        ABI.System.Collections.IEnumerable.FromAbiHelper _iterableToEnumerable;

        public unsafe object GetAt(uint index)
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetAt_0(ThisPtr, index, &__retval));
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        public unsafe bool IndexOf(object value, out uint index)
        {
            ObjectReferenceValue __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IndexOf_2(ThisPtr, MarshalInspectable<object>.GetAbi(__value), &__index, &__retval));
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, &__retval));
                return __retval;
            }
        }
        public IEnumerator GetEnumerator() => _iterableToEnumerable.GetEnumerator();
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IBindableVectorView_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, IntPtr* result);
        public unsafe delegate int get_Size_1(IntPtr thisPtr, uint* result);
        public unsafe delegate int IndexOf_2(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue);
    }
}

namespace ABI.System.Collections
{
    using global::Microsoft.UI.Xaml.Interop;
    using global::System;
    using global::System.Runtime.CompilerServices;

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
#if EMBED
    internal
#else
    public
#endif
    unsafe class IEnumerable : global::System.Collections.IEnumerable, IBindableIterable
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerable));

        public class FromAbiHelper : global::System.Collections.IEnumerable
        {
            private readonly global::ABI.System.Collections.IEnumerable _iterable;

            public FromAbiHelper(IObjectReference obj) :
                this(new global::ABI.System.Collections.IEnumerable(obj))
            {
            }

            public FromAbiHelper(global::ABI.System.Collections.IEnumerable iterable)
            {
                _iterable = iterable;
            }

            public global::System.Collections.IEnumerator GetEnumerator() =>
                new Generic.IEnumerator<object>.FromAbiHelper(new NonGenericToGenericIterator(((IBindableIterable)_iterable).First()));

            private sealed class NonGenericToGenericIterator : global::Windows.Foundation.Collections.IIterator<object>
            {
                private readonly IBindableIterator iterator;

                public NonGenericToGenericIterator(IBindableIterator iterator) => this.iterator = iterator;

                public object _Current => iterator.Current;
                public bool HasCurrent => iterator.HasCurrent;
                public bool _MoveNext() { return iterator.MoveNext(); }
                public uint GetMany(ref object[] items) => throw new NotSupportedException();
            }
        }

        public class ToAbiHelper : IBindableIterable
        {
            private readonly IEnumerable m_enumerable;

            internal ToAbiHelper(IEnumerable enumerable) => m_enumerable = enumerable;

            IBindableIterator IBindableIterable.First() => MakeBindableIterator(m_enumerable.GetEnumerator());

            internal static IBindableIterator MakeBindableIterator(IEnumerator enumerator) =>
                new Generic.IEnumerator<object>.ToAbiHelper(new NonGenericToGenericEnumerator(enumerator));

            private sealed class NonGenericToGenericEnumerator : IEnumerator<object>
            {
                private readonly IEnumerator enumerator;

                public NonGenericToGenericEnumerator(IEnumerator enumerator) => this.enumerator = enumerator; 

                public object Current => enumerator.Current;
                public bool MoveNext() { return enumerator.MoveNext(); }
                public void Reset() { enumerator.Reset(); }
                public void Dispose() { }
            }
        }

        [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _First_0;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> First_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_First_0; set => _First_0 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            private static readonly IEnumerable_Delegates.First_0 DelegateCache;

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _First_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache = Do_Abi_First_0).ToPointer()

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }


            private static unsafe int Do_Abi_First_0(IntPtr thisPtr, IntPtr* result)
            {
                *result = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IEnumerable>(thisPtr);
                    var iterator = ToAbiHelper.MakeBindableIterator(__this.GetEnumerator());
                    *result = MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableIterator>.FromManaged(iterator);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<Vftbl>.FromAbi(thisPtr);
        }

        public static implicit operator IEnumerable(IObjectReference obj) => (obj != null) ? new IEnumerable(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IEnumerable(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IEnumerable(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _FromIterable = new FromAbiHelper(this);
        }
        FromAbiHelper _FromIterable;

        unsafe global::Microsoft.UI.Xaml.Interop.IBindableIterator IBindableIterable.First()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.First_0(ThisPtr, &__retval));
                return MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableIterator>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableIterator>.DisposeAbi(__retval);
            }
        }

        public IEnumerator GetEnumerator() => _FromIterable.GetEnumerator();
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    static class IEnumerable_Delegates
    {
        public unsafe delegate int First_0(IntPtr thisPtr, IntPtr* result);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
#if EMBED
    internal
#else
    public
#endif 
    unsafe class IList : global::System.Collections.IList
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList));

        public class FromAbiHelper : global::System.Collections.IList
        {
            private readonly global::ABI.System.Collections.IList _vector;
            private readonly global::ABI.System.Collections.IEnumerable _enumerable;

            public FromAbiHelper(IObjectReference obj) :
                this(new global::ABI.System.Collections.IList(obj))
            {
            }

            public FromAbiHelper(global::ABI.System.Collections.IList vector)
            {
                _vector = vector;
                _enumerable = new ABI.System.Collections.IEnumerable(vector.ObjRef);
            }

            public bool IsSynchronized => false;

            public object SyncRoot { get => this; }

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

            public void CopyTo(Array array, int arrayIndex)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                // ICollection expects the destination array to be single-dimensional.
                if (array.Rank != 1)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Arg_RankMultiDimNotSupported);

                int destLB = array.GetLowerBound(0);
                int srcLen = Count;
                int destLen = array.GetLength(0);

                if (arrayIndex < destLB)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                // Does the dimension in question have sufficient space to copy the expected number of entries?
                // We perform this check before valid index check to ensure the exception message is in sync with
                // the following snippet that uses regular framework code:
                //
                // ArrayList list = new ArrayList();
                // list.Add(1);
                // Array items = Array.CreateInstance(typeof(object), new int[] { 1 }, new int[] { -1 });
                // list.CopyTo(items, 0);

                if (srcLen > (destLen - (arrayIndex - destLB)))
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                if (arrayIndex - destLB > destLen)
                    throw new ArgumentException(WinRTRuntimeErrorStrings.Argument_IndexOutOfArrayBounds);

                // We need to verify the index as we;
                for (uint i = 0; i < srcLen; i++)
                {
                    array.SetValue(_vector.GetAt(i), i + arrayIndex);
                }
            }

            public object this[int index]
            {
                get => Indexer_Get(index);
                set => Indexer_Set(index, value);
            }

            internal object Indexer_Get(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return GetAt(_vector, (uint)index);
            }

            internal void Indexer_Set(int index, object value)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                SetAt(_vector, (uint)index, value);
            }

            public int Add(object value)
            {
                _vector.Append(value);

                uint size = _vector.Size;
                if (((uint)int.MaxValue) < size)
                {
                    throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                }

                return (int)(size - 1);
            }

            public bool Contains(object item)
            {
                return _vector.IndexOf(item, out _);
            }

            public void Clear()
            {
                _vector._Clear();
            }

            public bool IsFixedSize { get => false; }

            public bool IsReadOnly { get => false; }

            public int IndexOf(object item)
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

            public void Insert(int index, object item)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                InsertAtHelper(_vector, (uint)index, item);
            }

            public void Remove(object item)
            {
                uint index;
                bool exists = _vector.IndexOf(item, out index);

                if (exists)
                {
                    if (((uint)int.MaxValue) < index)
                    {
                        throw new InvalidOperationException(WinRTRuntimeErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
                    }

                    RemoveAtHelper(_vector, index);
                }
            }

            public void RemoveAt(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                RemoveAtHelper(_vector, (uint)index);
            }

            private static object GetAt(global::ABI.System.Collections.IList _this, uint index)
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

            private static void SetAt(global::ABI.System.Collections.IList _this, uint index, object value)
            {
                try
                {
                    _this.SetAt(index, value);

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

            private static void InsertAtHelper(global::ABI.System.Collections.IList _this, uint index, object item)
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

            private static void RemoveAtHelper(global::ABI.System.Collections.IList _this, uint index)
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

            public IEnumerator GetEnumerator() => _enumerable.GetEnumerator();
        }

        public sealed class ToAbiHelper : IBindableVector
        {
            private global::System.Collections.IList _list;

            public ToAbiHelper(global::System.Collections.IList list) => _list = list;

            public object GetAt(uint index)
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

            public uint Size { get => (uint)_list.Count; }
            
            IBindableVectorView IBindableVector.GetView()
            {
                return new ListToBindableVectorViewAdapter(_list);
            }

            public bool IndexOf(object value, out uint index)
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

            public void SetAt(uint index, object value)
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

            public void InsertAt(uint index, object value)
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

            public void Append(object value)
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

            public void Clear()
            {
                _list.Clear();
            }

            private static void EnsureIndexInt32(uint index, int listCapacity)
            {
                // We use '<=' and not '<' becasue int.MaxValue == index would imply
                // that Size > int.MaxValue:
                if (((uint)int.MaxValue) <= index || index >= (uint)listCapacity)
                {
                    Exception e = new ArgumentOutOfRangeException(nameof(index), WinRTRuntimeErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                    e.SetHResult(ExceptionHelpers.E_BOUNDS);
                    throw e;
                }
            }

            public IEnumerator GetEnumerator() => _list.GetEnumerator();

            /// A Windows Runtime IBindableVectorView implementation that wraps around a managed IList exposing
            /// it to Windows runtime interop.
            internal sealed class ListToBindableVectorViewAdapter : IBindableVectorView
            {
                private readonly global::System.Collections.IList list;

                internal ListToBindableVectorViewAdapter(global::System.Collections.IList list)
                {
                    if (list == null)
                        throw new ArgumentNullException(nameof(list));
                    this.list = list;
                }

                private static void EnsureIndexInt32(uint index, int listCapacity)
                {
                    // We use '<=' and not '<' becasue int.MaxValue == index would imply
                    // that Size > int.MaxValue:
                    if (((uint)int.MaxValue) <= index || index >= (uint)listCapacity)
                    {
                        Exception e = new ArgumentOutOfRangeException(nameof(index), WinRTRuntimeErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                        e.SetHResult(ExceptionHelpers.E_BOUNDS);
                        throw e;
                    }
                }

                public IBindableIterator First() =>
                    IEnumerable.ToAbiHelper.MakeBindableIterator(list.GetEnumerator());

                public object GetAt(uint index)
                {
                    EnsureIndexInt32(index, list.Count);

                    try
                    {
                        return list[(int)index];
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, WinRTRuntimeErrorStrings.ArgumentOutOfRange_Index);
                    }
                }

                public uint Size => (uint)list.Count;

                public bool IndexOf(object value, out uint index)
                {
                    int ind = list.IndexOf(value);

                    if (-1 == ind)
                    {
                        index = 0;
                        return false;
                    }

                    index = (uint)ind;
                    return true;
                }

                public IEnumerator GetEnumerator() => list.GetEnumerator();
            }
        }

        [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _GetAt_0;
            private void* _get_Size_1;
            private void* _GetView_2;
            private void* _IndexOf_3;
            private void* _SetAt_4;
            private void* _InsertAt_5;
            private void* _RemoveAt_6;
            private void* _Append_7;
            private void* _RemoveAtEnd_8;
            private void* _Clear_9;

            public delegate* unmanaged[Stdcall]<IntPtr , uint , IntPtr* , int> GetAt_0 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr * , int >)_GetAt_0; set => _GetAt_0 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , uint* , int> get_Size_1 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint * , int >)_get_Size_1; set => _get_Size_1 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , IntPtr* , int> GetView_2 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr * , int >)_GetView_2; set => _GetView_2 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , IntPtr , uint* , byte* , int> IndexOf_3 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint * , byte * , int >)_IndexOf_3; set => _IndexOf_3 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , uint , IntPtr , int> SetAt_4 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int >)_SetAt_4; set => _SetAt_4 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , uint , IntPtr , int> InsertAt_5 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int >)_InsertAt_5; set => _InsertAt_5 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , uint , int> RemoveAt_6 { get => (delegate* unmanaged[Stdcall]<IntPtr, uint, int >)_RemoveAt_6; set => _RemoveAt_6 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , IntPtr , int> Append_7 { get => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int >)_Append_7; set => _Append_7 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , int> RemoveAtEnd_8 { get => (delegate* unmanaged[Stdcall]<IntPtr, int >)_RemoveAtEnd_8; set => _RemoveAtEnd_8 = value; }
            public delegate* unmanaged[Stdcall]<IntPtr , int> Clear_9 { get => (delegate* unmanaged[Stdcall]<IntPtr, int >)_Clear_9; set => _Clear_9 = value; }

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;

            private static readonly Delegate[] DelegateCache = new Delegate[10];

            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                    _GetAt_0 = Marshal.GetFunctionPointerForDelegate(DelegateCache[0] = new IList_Delegates.GetAt_0(Do_Abi_GetAt_0)).ToPointer(),
                    _get_Size_1 = Marshal.GetFunctionPointerForDelegate(DelegateCache[1] = new IList_Delegates.get_Size_1(Do_Abi_get_Size_1)).ToPointer(),
                    _GetView_2 = Marshal.GetFunctionPointerForDelegate(DelegateCache[2] = new IList_Delegates.GetView_2(Do_Abi_GetView_2)).ToPointer(),
                    _IndexOf_3 = Marshal.GetFunctionPointerForDelegate(DelegateCache[3] = new IList_Delegates.IndexOf_3(Do_Abi_IndexOf_3)).ToPointer(),
                    _SetAt_4 = Marshal.GetFunctionPointerForDelegate(DelegateCache[4] = new IList_Delegates.SetAt_4(Do_Abi_SetAt_4)).ToPointer(),
                    _InsertAt_5 = Marshal.GetFunctionPointerForDelegate(DelegateCache[5] = new IList_Delegates.InsertAt_5(Do_Abi_InsertAt_5)).ToPointer(),
                    _RemoveAt_6 = Marshal.GetFunctionPointerForDelegate(DelegateCache[6] = new IList_Delegates.RemoveAt_6(Do_Abi_RemoveAt_6)).ToPointer(),
                    _Append_7 = Marshal.GetFunctionPointerForDelegate(DelegateCache[7] = new IList_Delegates.Append_7(Do_Abi_Append_7)).ToPointer(),
                    _RemoveAtEnd_8 = Marshal.GetFunctionPointerForDelegate(DelegateCache[8] = new IList_Delegates.RemoveAtEnd_8(Do_Abi_RemoveAtEnd_8)).ToPointer(),
                    _Clear_9 = Marshal.GetFunctionPointerForDelegate(DelegateCache[9] = new IList_Delegates.Clear_9(Do_Abi_Clear_9)).ToPointer(),

                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 10);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.IList, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.IList, ToAbiHelper>();

            private static IBindableVector FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IList>(thisPtr);
                return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
            }


            private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, IntPtr* result)
            {
                object __result = default;
                *result = default;
                try
                {
                    __result = FindAdapter(thisPtr).GetAt(index);
                    *result = MarshalInspectable<object>.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, IntPtr* result)
            {
                global::Microsoft.UI.Xaml.Interop.IBindableVectorView __result = default;
                *result = default;
                try
                {
                    __result = FindAdapter(thisPtr).GetView();
                    *result = MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>.FromManaged(__result);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_IndexOf_3(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue)
            {
                bool __returnValue = default;
                *index = default;
                *returnValue = default;
                uint __index = default;
                try
                {
                    __returnValue = FindAdapter(thisPtr).IndexOf(MarshalInspectable<object>.FromAbi(value), out __index);
                    *index = __index;
                    *returnValue = (byte)(__returnValue ? 1 : 0);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_SetAt_4(IntPtr thisPtr, uint index, IntPtr value)
            {
                try
                {
                    FindAdapter(thisPtr).SetAt(index, MarshalInspectable<object>.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_InsertAt_5(IntPtr thisPtr, uint index, IntPtr value)
            {
                try
                {
                    FindAdapter(thisPtr).InsertAt(index, MarshalInspectable<object>.FromAbi(value));
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

            private static unsafe int Do_Abi_Append_7(IntPtr thisPtr, IntPtr value)
            {
                try
                {
                    FindAdapter(thisPtr).Append(MarshalInspectable<object>.FromAbi(value));
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
                    FindAdapter(thisPtr).Clear();
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }

            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* value)
            {
                uint __value = default;

                *value = default;

                try
                {
                    __value = FindAdapter(thisPtr).Size;
                    *value = __value;
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
        }
        internal static ObjectReference<Vftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<Vftbl>.FromAbi(thisPtr);
        }

        public static implicit operator IList(IObjectReference obj) => (obj != null) ? new IList(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IList(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IList(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
            _vectorToList = new ABI.System.Collections.IList.FromAbiHelper(this);
        }
        ABI.System.Collections.IList.FromAbiHelper _vectorToList;

        public unsafe object GetAt(uint index)
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetAt_0(ThisPtr, index, &__retval));
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        internal unsafe global::Microsoft.UI.Xaml.Interop.IBindableVectorView GetView()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_2(ThisPtr, &__retval));
                return MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Microsoft.UI.Xaml.Interop.IBindableVectorView>.DisposeAbi(__retval);
            }
        }

        public unsafe bool IndexOf(object value, out uint index)
        {
            ObjectReferenceValue __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IndexOf_3(ThisPtr, MarshalInspectable<object>.GetAbi(__value), &__index, &__retval));
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        public unsafe void SetAt(uint index, object value)
        {
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.SetAt_4(ThisPtr, index, MarshalInspectable<object>.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        public unsafe void InsertAt(uint index, object value)
        {
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.InsertAt_5(ThisPtr, index, MarshalInspectable<object>.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        public unsafe void RemoveAt(uint index)
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAt_6(ThisPtr, index));
        }

        public unsafe void Append(object value)
        {
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Append_7(ThisPtr, MarshalInspectable<object>.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
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

        public unsafe uint Size
        {
            get
            {
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, &__retval));
                return __retval;
            }
        }

        public object this[int index]
        {
            get => _vectorToList[index];
            set => _vectorToList[index] = value;
        }

        public bool IsFixedSize => _vectorToList.IsFixedSize;

        public bool IsReadOnly => _vectorToList.IsReadOnly;

        public int Count => _vectorToList.Count;

        public bool IsSynchronized => _vectorToList.IsSynchronized;

        public object SyncRoot => _vectorToList.SyncRoot;

        public int Add(object value) => _vectorToList.Add(value);

        public void Clear() => _vectorToList.Clear();

        public bool Contains(object value) => _vectorToList.Contains(value);

        public int IndexOf(object value) => _vectorToList.IndexOf(value);

        public void Insert(int index, object value) => _vectorToList.Insert(index, value);

        public void Remove(object value) => _vectorToList.Remove(value);

        public void RemoveAt(int index) => _vectorToList.RemoveAt(index);

        public void CopyTo(Array array, int index) => _vectorToList.CopyTo(array, index);

        public IEnumerator GetEnumerator() => _vectorToList.GetEnumerator();
    }
    internal static class IList_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, IntPtr* result);
        public unsafe delegate int get_Size_1(IntPtr thisPtr, uint* result);
        public unsafe delegate int GetView_2(IntPtr thisPtr, IntPtr* result);
        public unsafe delegate int IndexOf_3(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue);
        public unsafe delegate int SetAt_4(IntPtr thisPtr, uint index, IntPtr value);
        public unsafe delegate int InsertAt_5(IntPtr thisPtr, uint index, IntPtr value);
        public unsafe delegate int RemoveAt_6(IntPtr thisPtr, uint index);
        public unsafe delegate int Append_7(IntPtr thisPtr, IntPtr value);
        public unsafe delegate int RemoveAtEnd_8(IntPtr thisPtr);
        public unsafe delegate int Clear_9(IntPtr thisPtr);
    }
}
