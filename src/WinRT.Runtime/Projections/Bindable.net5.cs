// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace ABI.System.Collections
{
    using WUX = global::Windows.UI.Xaml.Interop;
    using MUX = global::Microsoft.UI.Xaml.Interop;
    using global::System;
    using global::System.Runtime.CompilerServices;
    using global::System.Diagnostics.CodeAnalysis;

#if EMBED
    internal
#else
    public
#endif
    static class IEnumerableMethods
    {
        public static global::System.Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0x08, 0x2C, 0x6D, 0x03, 0x29, 0xDF, 0xAF, 0x41, 0x8A, 0xA2, 0xD7, 0x74, 0xBE, 0x62, 0xBA, 0x6F }));

        public static IntPtr AbiToProjectionVftablePtr => IEnumerable.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    internal unsafe interface IEnumerable : global::System.Collections.IEnumerable, WUX.IBindableIterable, MUX.IBindableIterable
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IEnumerable));

        public sealed class AdaptiveFromAbiHelper : global::System.Collections.IEnumerable
        {
            private readonly Func<IWinRTObject, IEnumerator> _enumerator;
            private readonly IWinRTObject _winRTObject;

            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(IEnumerable<>))]
            [SuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.", Justification = "We explicitly preserve the type we're looking for with the DynamicDependency attribute.")]
            [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "We can't annotate this case (GetMethod on a type returned from GetInterface), so we use DynamicDependency to keep alive the one type we care about's public methods.")]
            public AdaptiveFromAbiHelper(
                Type runtimeType, IWinRTObject winRTObject)
                : this(winRTObject)
            {
                Type enumGenericType = (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ? 
                    runtimeType : runtimeType.GetInterface("System.Collections.Generic.IEnumerable`1");
                if(enumGenericType != null)
                {
                    var getEnumerator = enumGenericType.GetMethod("GetEnumerator");
                    _enumerator = (IWinRTObject obj) => (IEnumerator)getEnumerator.Invoke(obj, null);
                }
            }

            public AdaptiveFromAbiHelper(IWinRTObject winRTObject)
            {
                _winRTObject = winRTObject;
                if (winRTObject is WUX.IBindableIterable)
                {
                    _enumerator = (IWinRTObject obj) => new Generic.FromAbiEnumerator<object>(new NonGenericToGenericWuxIterator(((WUX.IBindableIterable)obj).First()));
                }
                else if (winRTObject is MUX.IBindableIterable)
                {
                    _enumerator = (IWinRTObject obj) => new Generic.FromAbiEnumerator<object>(new NonGenericToGenericMuxIterator(((MUX.IBindableIterable)obj).First()));
                }
            }

            public IEnumerator GetEnumerator() => _enumerator(_winRTObject);

            private sealed class NonGenericToGenericWuxIterator : global::Windows.Foundation.Collections.IIterator<object>
            {
                private readonly WUX.IBindableIterator iterator;

                public NonGenericToGenericWuxIterator(WUX.IBindableIterator iterator) => this.iterator = iterator;

                public object _Current => iterator.Current;
                public bool HasCurrent => iterator.HasCurrent;
                public bool _MoveNext() { return iterator.MoveNext(); }
                public uint GetMany(ref object[] items) => throw new NotSupportedException();
            }

            private sealed class NonGenericToGenericMuxIterator : global::Windows.Foundation.Collections.IIterator<object>
            {
                private readonly MUX.IBindableIterator iterator;

                public NonGenericToGenericMuxIterator(MUX.IBindableIterator iterator) => this.iterator = iterator;

                public object _Current => iterator.Current;
                public bool HasCurrent => iterator.HasCurrent;
                public bool _MoveNext() { return iterator.MoveNext(); }
                public uint GetMany(ref object[] items) => throw new NotSupportedException();
            }
        }

        private sealed class ToWuxAbiHelper : WUX.IBindableIterable
        {
            private readonly IEnumerable m_enumerable;

            internal ToWuxAbiHelper(IEnumerable enumerable) => m_enumerable = enumerable;

            WUX.IBindableIterator WUX.IBindableIterable.First() => MakeBindableIterator(m_enumerable.GetEnumerator());

            internal static WUX.IBindableIterator MakeBindableIterator(IEnumerator enumerator) =>
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

        private sealed class ToMuxAbiHelper(IEnumerable enumerable) : MUX.IBindableIterable
        {
            MUX.IBindableIterator MUX.IBindableIterable.First() => MakeBindableIterator(enumerable.GetEnumerator());

            internal static MUX.IBindableIterator MakeBindableIterator(IEnumerator enumerator) =>
                new Generic.IEnumerator<object>.ToAbiHelper(new NonGenericToGenericEnumerator(enumerator));

            private sealed class NonGenericToGenericEnumerator(IEnumerator enumerator) : IEnumerator<object>
            {
                public object Current => enumerator.Current;
                public bool MoveNext() { return enumerator.MoveNext(); }
                public void Reset() { enumerator.Reset(); }
                public void Dispose() { }
            }
        }

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IEnumerable()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IEnumerable), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 1);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = Projections.UiXamlModeSetting is Projections.UiXamlMode.WindowsUiXaml ? &Do_Wux_Abi_First_0 : &Do_Mux_Abi_First_0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Wux_Abi_First_0(IntPtr thisPtr, IntPtr* result)
        {
            *result = default;
            try
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IEnumerable>(thisPtr);
                var iterator = ToWuxAbiHelper.MakeBindableIterator(__this.GetEnumerator());
                *result = MarshalInterface<WUX.IBindableIterator>.FromManaged(iterator);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Mux_Abi_First_0(IntPtr thisPtr, IntPtr* result)
        {
            *result = default;
            try
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IEnumerable>(thisPtr);
                var iterator = ToMuxAbiHelper.MakeBindableIterator(__this.GetEnumerator());
                *result = MarshalInterface<MUX.IBindableIterator>.FromManaged(iterator);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        internal static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);
        }

        private static AdaptiveFromAbiHelper _AbiHelper(IWinRTObject _this)
        {
            return (AdaptiveFromAbiHelper)_this.GetOrCreateTypeHelperData(typeof(global::System.Collections.IEnumerable).TypeHandle,
                () => new AdaptiveFromAbiHelper(_this));
        }

        unsafe WUX.IBindableIterator WUX.IBindableIterable.First()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IEnumerable).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval));
                return MarshalInterface<WUX.IBindableIterator>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<WUX.IBindableIterator>.DisposeAbi(__retval);
            }
        }

        unsafe MUX.IBindableIterator MUX.IBindableIterable.First()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IEnumerable).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval));
                return MarshalInterface<MUX.IBindableIterator>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<MUX.IBindableIterator>.DisposeAbi(__retval);
            }
        }

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
        {
            return _AbiHelper((IWinRTObject)this).GetEnumerator();
        }
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

#if EMBED
    internal
#else
    public
#endif
    static class IListMethods
    {
        public static Guid IID { get; } = new Guid(new global::System.ReadOnlySpan<byte>(new byte[] { 0xDE, 0xE7, 0x3D, 0x39, 0xD0, 0x6F, 0x0D, 0x4C, 0xBB, 0x71, 0x47, 0x24, 0x4A, 0x11, 0x3E, 0x93 }));

        public static IntPtr AbiToProjectionVftablePtr => IList.AbiToProjectionVftablePtr;
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
    internal unsafe interface IList : global::System.Collections.IList, global::Windows.UI.Xaml.Interop.IBindableVector
    {
        public static string GetGuidSignature() => GuidGenerator.GetSignature(typeof(IList));

        public interface IBindableVectorAdapter
        {
            object GetAt(uint index);
            IBindableVectorViewAdapter GetView();
            bool IndexOf(object value, out uint index);
            void SetAt(uint index, object value);
            void InsertAt(uint index, object value);
            void RemoveAt(uint index);
            void Append(object value);
            void RemoveAtEnd();
            void Clear();
            uint Size { get; }
        }

        public interface IBindableVectorViewAdapter
        {
            object GetAt(uint index);
            bool IndexOf(object value, out uint index);
            uint Size { get; }
        }

        private sealed class WuxBindableVectorAdapter(WUX.IBindableVector vector) : IBindableVectorAdapter
        {
            public object GetAt(uint index) => vector.GetAt(index);
            public IBindableVectorViewAdapter GetView() => new WuxBindableVectorViewAdapter(vector.GetView());
            public bool IndexOf(object value, out uint index) => vector.IndexOf(value, out index);
            public void SetAt(uint index, object value) => vector.SetAt(index, value);
            public void InsertAt(uint index, object value) => vector.InsertAt(index, value);
            public void RemoveAt(uint index) => vector.RemoveAt(index);
            public void Append(object value) => vector.Append(value);
            public void RemoveAtEnd() => vector.RemoveAtEnd();
            public void Clear() => vector.Clear();
            public uint Size => vector.Size;

            private sealed class WuxBindableVectorViewAdapter(WUX.IBindableVectorView vectorView) : IBindableVectorViewAdapter
            {
                public object GetAt(uint index) => vectorView.GetAt(index);
                public bool IndexOf(object value, out uint index) => vectorView.IndexOf(value, out index);
                public uint Size => vectorView.Size;
            }
        }

        private sealed class MuxBindableVectorAdapter(MUX.IBindableVector vector) : IBindableVectorAdapter
        {
            public object GetAt(uint index) => vector.GetAt(index);
            public IBindableVectorViewAdapter GetView() => new WuxBindableVectorViewAdapter(vector.GetView());
            public bool IndexOf(object value, out uint index) => vector.IndexOf(value, out index);
            public void SetAt(uint index, object value) => vector.SetAt(index, value);
            public void InsertAt(uint index, object value) => vector.InsertAt(index, value);
            public void RemoveAt(uint index) => vector.RemoveAt(index);
            public void Append(object value) => vector.Append(value);
            public void RemoveAtEnd() => vector.RemoveAtEnd();
            public void Clear() => vector.Clear();
            public uint Size => vector.Size;

            private sealed class WuxBindableVectorViewAdapter(MUX.IBindableVectorView vectorView) : IBindableVectorViewAdapter
            {
                public object GetAt(uint index) => vectorView.GetAt(index);
                public bool IndexOf(object value, out uint index) => vectorView.IndexOf(value, out index);
                public uint Size => vectorView.Size;
            }
        }

        public sealed class FromAbiHelper : global::System.Collections.IList
        {
            private readonly IBindableVectorAdapter _vector;

            public FromAbiHelper(IBindableVectorAdapter vector)
            {
                _vector = vector;
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
                _vector.Clear();
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

            private static object GetAt(IBindableVectorAdapter _this, uint index)
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

            private static void SetAt(IBindableVectorAdapter _this, uint index, object value)
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

            private static void InsertAtHelper(IBindableVectorAdapter _this, uint index, object item)
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

            private static void RemoveAtHelper(IBindableVectorAdapter _this, uint index)
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

            public IEnumerator GetEnumerator()
            {
                return ((IEnumerable)(IWinRTObject)_vector).GetEnumerator();
            }
        }

        public sealed class ToAbiHelper : WUX.IBindableVector, MUX.IBindableVector, IBindableVectorAdapter
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

            IBindableVectorViewAdapter IBindableVectorAdapter.GetView()
            {
                return new ListToBindableVectorViewAdapter(_list);
            }

            WUX.IBindableVectorView WUX.IBindableVector.GetView()
            {
                return new ListToBindableVectorViewAdapter(_list);
            }

            MUX.IBindableVectorView MUX.IBindableVector.GetView()
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
            internal sealed class ListToBindableVectorViewAdapter : WUX.IBindableVectorView, MUX.IBindableVectorView, IBindableVectorViewAdapter
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

        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IList()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IList), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 10);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_GetAt_0;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_Size_1;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[8] = Projections.UiXamlModeSetting is Projections.UiXamlMode.WindowsUiXaml ? &Do_Wux_Abi_GetView_2 : &Do_Mux_Abi_GetView_2;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>*)AbiToProjectionVftablePtr)[9] = &Do_Abi_IndexOf_3;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>*)AbiToProjectionVftablePtr)[10] = &Do_Abi_SetAt_4;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>*)AbiToProjectionVftablePtr)[11] = &Do_Abi_InsertAt_5;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, int>*)AbiToProjectionVftablePtr)[12] = &Do_Abi_RemoveAt_6;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>*)AbiToProjectionVftablePtr)[13] = &Do_Abi_Append_7;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)AbiToProjectionVftablePtr)[14] = &Do_Abi_RemoveAtEnd_8;
            ((delegate* unmanaged[Stdcall]<IntPtr, int>*)AbiToProjectionVftablePtr)[15] = &Do_Abi_Clear_9;
        }

        private static readonly ConditionalWeakTable<global::System.Collections.IList, ToAbiHelper> _adapterTable = new();

        private static IBindableVectorAdapter FindAdapter(IntPtr thisPtr)
        {
            var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IList>(thisPtr);
            return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Wux_Abi_GetView_2(IntPtr thisPtr, IntPtr* result)
        {
            IBindableVectorViewAdapter __result = default;
            *result = default;
            try
            {
                __result = FindAdapter(thisPtr).GetView();
                *result = MarshalInterface<WUX.IBindableVectorView>.FromManaged((WUX.IBindableVectorView)__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
        private static unsafe int Do_Mux_Abi_GetView_2(IntPtr thisPtr, IntPtr* result)
        {
            IBindableVectorViewAdapter __result = default;
            *result = default;
            try
            {
                __result = FindAdapter(thisPtr).GetView();
                *result = MarshalInterface<MUX.IBindableVectorView>.FromManaged((MUX.IBindableVectorView)__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        internal static ObjectReference<IUnknownVftbl> ObjRefFromAbi(IntPtr thisPtr)
        {
            if (thisPtr == IntPtr.Zero)
            {
                return null;
            }
            return ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);
        }

        internal static FromAbiHelper _VectorToList(IWinRTObject _this)
        {
            IBindableVectorAdapter adapter = null;
            if (Projections.UiXamlModeSetting is Projections.UiXamlMode.WindowsUiXaml)
            {
                adapter = new WuxBindableVectorAdapter((WUX.IBindableVector)_this);
            }
            else
            {
                adapter = new MuxBindableVectorAdapter((MUX.IBindableVector)_this);
            }
            return (FromAbiHelper)_this.GetOrCreateTypeHelperData(typeof(global::System.Collections.IList).TypeHandle,
                () => new FromAbiHelper(adapter));
        }

        unsafe object global::Windows.UI.Xaml.Interop.IBindableVector.GetAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>**)ThisPtr)[6](ThisPtr, index, &__retval));
                return MarshalInspectable<object>.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        unsafe global::Windows.UI.Xaml.Interop.IBindableVectorView global::Windows.UI.Xaml.Interop.IBindableVector.GetView()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[8](ThisPtr, &__retval));
                return MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableVectorView>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableVectorView>.DisposeAbi(__retval);
            }
        }

        unsafe bool global::Windows.UI.Xaml.Interop.IBindableVector.IndexOf(object value, out uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>**)ThisPtr)[9](ThisPtr, MarshalInspectable<object>.GetAbi(__value), &__index, &__retval));
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.UI.Xaml.Interop.IBindableVector.SetAt(uint index, object value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>**)ThisPtr)[10](ThisPtr, index, MarshalInspectable<object>.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.UI.Xaml.Interop.IBindableVector.InsertAt(uint index, object value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, int>**)ThisPtr)[11](ThisPtr, index, MarshalInspectable<object>.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.UI.Xaml.Interop.IBindableVector.RemoveAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint, int>**)ThisPtr)[12](ThisPtr, index));
        }

        unsafe void global::Windows.UI.Xaml.Interop.IBindableVector.Append(object value)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>**)ThisPtr)[13](ThisPtr, MarshalInspectable<object>.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        unsafe void global::Windows.UI.Xaml.Interop.IBindableVector.RemoveAtEnd()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[14](ThisPtr));
        }

        unsafe void global::Windows.UI.Xaml.Interop.IBindableVector.Clear()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int>**)ThisPtr)[15](ThisPtr));
        }

        unsafe uint global::Windows.UI.Xaml.Interop.IBindableVector.Size
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::System.Collections.IList).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
                return __retval;
            }
        }

        object global::System.Collections.IList.this[int index]
        {
            get => _VectorToList((IWinRTObject)this)[index];
             
            set => _VectorToList((IWinRTObject)this)[index] = value;
        }

        bool global::System.Collections.IList.IsFixedSize => _VectorToList((IWinRTObject)this).IsFixedSize;

        bool global::System.Collections.IList.IsReadOnly => _VectorToList((IWinRTObject)this).IsReadOnly;

        int global::System.Collections.ICollection.Count => _VectorToList((IWinRTObject)this).Count;

        bool global::System.Collections.ICollection.IsSynchronized => _VectorToList((IWinRTObject)this).IsSynchronized;

        object global::System.Collections.ICollection.SyncRoot => _VectorToList((IWinRTObject)this).SyncRoot;

        int global::System.Collections.IList.Add(object value) => _VectorToList((IWinRTObject)this).Add(value);

        void global::System.Collections.IList.Clear() => _VectorToList((IWinRTObject)this).Clear();

        bool global::System.Collections.IList.Contains(object value) => _VectorToList((IWinRTObject)this).Contains(value);

        int global::System.Collections.IList.IndexOf(object value) => _VectorToList((IWinRTObject)this).IndexOf(value);

        void global::System.Collections.IList.Insert(int index, object value) => _VectorToList((IWinRTObject)this).Insert(index, value);

        void global::System.Collections.IList.Remove(object value) => _VectorToList((IWinRTObject)this).Remove(value);

        void global::System.Collections.IList.RemoveAt(int index) => _VectorToList((IWinRTObject)this).RemoveAt(index);

        void global::System.Collections.ICollection.CopyTo(Array array, int index) => _VectorToList((IWinRTObject)this).CopyTo(array, index);

        IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => _VectorToList((IWinRTObject)this).GetEnumerator();
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
