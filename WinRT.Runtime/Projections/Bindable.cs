using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Interop;
using WinRT;
using WinRT.Interop;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to

namespace Windows.UI.Xaml.Interop
{
    [global::WinRT.WindowsRuntimeType]
    public delegate void BindableVectorChangedEventHandler(IBindableObservableVector vector, object e);
    [global::WinRT.WindowsRuntimeType]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    public interface IBindableIterable
    {
        IBindableIterator First();
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    public interface IBindableIterator
    {
        bool MoveNext();
        object Current { get; }
        bool HasCurrent { get; }
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("FE1EB536-7E7F-4F90-AC9A-474984AAE512")]
    public interface IBindableObservableVector : IList, IEnumerable //IBindableVector, IBindableIterable
    {
        event BindableVectorChangedEventHandler VectorChanged;
    }
    [global::WinRT.WindowsRuntimeType]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
    public interface IBindableVector : IEnumerable //IBindableIterable
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
    public interface IBindableVectorView : IEnumerable //IBindableIterable
    {
        object GetAt(uint index);
        bool IndexOf(object value, out uint index);
        uint Size { get; }
    }
}

namespace ABI.Windows.UI.Xaml.Interop
{
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    [Guid("624CD4E1-D007-43B1-9C03-AF4D3E6258C4")]
    public static class BindableVectorChangedEventHandler
    {
        private unsafe delegate int Abi_Invoke(IntPtr thisPtr, IntPtr vector, IntPtr e);

        private static readonly global::WinRT.Interop.IDelegateVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static BindableVectorChangedEventHandler()
        {
            AbiInvokeDelegate = new Abi_Invoke(Do_Abi_Invoke);
            AbiToProjectionVftable = new global::WinRT.Interop.IDelegateVftbl
            {
                IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                Invoke = Marshal.GetFunctionPointerForDelegate(AbiInvokeDelegate)
            };
            var nativeVftbl = ComWrappersSupport.AllocateVtableMemory(typeof(BindableVectorChangedEventHandler), Marshal.SizeOf<global::WinRT.Interop.IDelegateVftbl>());
            Marshal.StructureToPtr(AbiToProjectionVftable, nativeVftbl, false);
            AbiToProjectionVftablePtr = nativeVftbl;
        }

        public static global::System.Delegate AbiInvokeDelegate { get; }

        public static unsafe IObjectReference CreateMarshaler(global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler managedDelegate) => ComWrappersSupport.CreateCCWForObject(managedDelegate).As<global::WinRT.Interop.IDelegateVftbl>(GuidGenerator.GetIID(typeof(BindableVectorChangedEventHandler)));

        public static IntPtr GetAbi(IObjectReference value) => MarshalInterfaceHelper<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler>.GetAbi(value);

        public static unsafe global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler FromAbi(IntPtr nativeDelegate)
        {
            var abiDelegate = ObjectReference<IDelegateVftbl>.FromAbi(nativeDelegate);
            return (global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler)ComWrappersSupport.TryRegisterObjectForInterface(new global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler(new NativeDelegateWrapper(abiDelegate).Invoke), nativeDelegate);
        }

        [global::WinRT.ObjectReferenceWrapper(nameof(_nativeDelegate))]
        private class NativeDelegateWrapper
        {
            private readonly ObjectReference<global::WinRT.Interop.IDelegateVftbl> _nativeDelegate;

            public NativeDelegateWrapper(ObjectReference<global::WinRT.Interop.IDelegateVftbl> nativeDelegate)
            {
                _nativeDelegate = nativeDelegate;
            }

            public void Invoke(global::Windows.UI.Xaml.Interop.IBindableObservableVector vector, object e)
            {
                IntPtr ThisPtr = _nativeDelegate.ThisPtr;
                var abiInvoke = Marshal.GetDelegateForFunctionPointer<Abi_Invoke>(_nativeDelegate.Vftbl.Invoke);
                IObjectReference __vector = default;
                IObjectReference __e = default;
                try
                {
                    __vector = MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableObservableVector>.CreateMarshaler(vector);
                    __e = MarshalInspectable.CreateMarshaler(e);
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(abiInvoke(ThisPtr, MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableObservableVector>.GetAbi(__vector), MarshalInspectable.GetAbi(__e)));
                }
                finally
                {
                    MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableObservableVector>.DisposeMarshaler(__vector);
                    MarshalInspectable.DisposeMarshaler(__e);
                }

            }
        }

        public static IntPtr FromManaged(global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler managedDelegate) => CreateMarshaler(managedDelegate).GetRef();

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler>.DisposeMarshaler(value);

        public static void DisposeAbi(IntPtr abi) => MarshalInterfaceHelper<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler>.DisposeAbi(abi);

        private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr vector, IntPtr e)
        {


            try
            {
                global::WinRT.ComWrappersSupport.MarshalDelegateInvoke(thisPtr, (global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler invoke) =>
                {
                    invoke(MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableObservableVector>.FromAbi(vector), MarshalInspectable.FromAbi(e));
                });

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    public class IBindableIterator : global::Windows.UI.Xaml.Interop.IBindableIterator
    {
        [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public _get_PropertyAsObject get_Current_0;
            public _get_PropertyAsBoolean get_HasCurrent_1;
            public IBindableIterator_Delegates.MoveNext_2 MoveNext_2;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable, 
                    get_Current_0 = Do_Abi_get_Current_0,
                    get_HasCurrent_1 = Do_Abi_get_HasCurrent_1,
                    MoveNext_2 = Do_Abi_MoveNext_2
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 3);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, out byte result)
            {
                bool __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableIterator>(thisPtr).MoveNext();
                    result = (byte)(__result ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Current_0(IntPtr thisPtr, out IntPtr value)
            {
                object __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableIterator>(thisPtr).Current;
                    value = MarshalInspectable.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableIterator>(thisPtr).HasCurrent;
                    value = (byte)(__value ? 1 : 0);

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
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.MoveNext_2(ThisPtr, out __retval));
            return __retval != 0;
        }

        public unsafe object Current
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Current_0(ThisPtr, out __retval));
                    return MarshalInspectable.FromAbi(__retval);
                }
                finally
                {
                    MarshalInspectable.DisposeAbi(__retval);
                }
            }
        }

        public unsafe bool HasCurrent
        {
            get
            {
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_HasCurrent_1(ThisPtr, out __retval));
                return __retval != 0;
            }
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public static class IBindableIterator_Delegates
    {
        public unsafe delegate int MoveNext_2(IntPtr thisPtr, out byte result);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("FE1EB536-7E7F-4F90-AC9A-474984AAE512")]
    public class IBindableObservableVector : global::Windows.UI.Xaml.Interop.IBindableObservableVector
    {
        [Guid("FE1EB536-7E7F-4F90-AC9A-474984AAE512")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public _add_EventHandler add_VectorChanged_0;
            public _remove_EventHandler remove_VectorChanged_1;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    add_VectorChanged_0 = Do_Abi_add_VectorChanged_0,
                    remove_VectorChanged_1 = Do_Abi_remove_VectorChanged_1
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 2);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Windows.UI.Xaml.Interop.IBindableObservableVector, global::WinRT.EventRegistrationTokenTable<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler>> _VectorChanged_TokenTables = new global::System.Runtime.CompilerServices.ConditionalWeakTable<global::Windows.UI.Xaml.Interop.IBindableObservableVector, global::WinRT.EventRegistrationTokenTable<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler>>();
            private static unsafe int Do_Abi_add_VectorChanged_0(IntPtr thisPtr, IntPtr handler, out global::WinRT.EventRegistrationToken token)
            {
                token = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableObservableVector>(thisPtr);
                    var __handler = BindableVectorChangedEventHandler.FromAbi(handler);
                    token = _VectorChanged_TokenTables.GetOrCreateValue(__this).AddEventHandler(__handler);
                    __this.VectorChanged += __handler;
                    return 0;
                }
                catch (Exception __ex)
                {
                    return __ex.HResult;
                }
            }
            private static unsafe int Do_Abi_remove_VectorChanged_1(IntPtr thisPtr, global::WinRT.EventRegistrationToken token)
            {
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableObservableVector>(thisPtr);
                    if (_VectorChanged_TokenTables.TryGetValue(__this, out var __table) && __table.RemoveEventHandler(token, out var __handler))
                    {
                        __this.VectorChanged -= __handler;
                    }
                    return 0;
                }
                catch (Exception __ex)
                {
                    return __ex.HResult;
                }
            }
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator IBindableObservableVector(IObjectReference obj) => (obj != null) ? new IBindableObservableVector(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;

        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public IBindableObservableVector(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal IBindableObservableVector(ObjectReference<Vftbl> obj)
        {
            _obj = obj;

            _VectorChanged =
                new EventSource<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler>(_obj,
                _obj.Vftbl.add_VectorChanged_0,
                _obj.Vftbl.remove_VectorChanged_1);
            _vectorToList = new ABI.System.Collections.IList.FromAbiHelper(ObjRef);
        }
        ABI.System.Collections.IList.FromAbiHelper _vectorToList;

        public event global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler VectorChanged
        {
            add => _VectorChanged.Subscribe(value);
            remove => _VectorChanged.Unsubscribe(value);
        }

        private EventSource<global::Windows.UI.Xaml.Interop.BindableVectorChangedEventHandler> _VectorChanged;

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

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
    public class IBindableVectorView : global::Windows.UI.Xaml.Interop.IBindableVectorView
    {
        [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public IBindableVectorView_Delegates.GetAt_0 GetAt_0;
            public _get_PropertyAsUInt32 get_Size_1;
            public IBindableVectorView_Delegates.IndexOf_2 IndexOf_2;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = Do_Abi_GetAt_0,
                    get_Size_1 = Do_Abi_get_Size_1,
                    IndexOf_2 = Do_Abi_IndexOf_2
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 3);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, out IntPtr result)
            {
                object __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableVectorView>(thisPtr).GetAt(index);
                    result = MarshalInspectable.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_2(IntPtr thisPtr, IntPtr value, out uint index, out byte returnValue)
            {
                bool __returnValue = default;

                index = default;
                returnValue = default;
                uint __index = default;

                try
                {
                    __returnValue = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableVectorView>(thisPtr).IndexOf(MarshalInspectable.FromAbi(value), out __index);
                    index = __index;
                    returnValue = (byte)(__returnValue ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint value)
            {
                uint __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Windows.UI.Xaml.Interop.IBindableVectorView>(thisPtr).Size;
                    value = __value;

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
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetAt_0(ThisPtr, index, out __retval));
                return MarshalInspectable.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable.DisposeAbi(__retval);
            }
        }

        public unsafe bool IndexOf(object value, out uint index)
        {
            IObjectReference __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable.CreateMarshaler(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IndexOf_2(ThisPtr, MarshalInspectable.GetAbi(__value), out __index, out __retval));
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__value);
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
        public IEnumerator GetEnumerator() => _iterableToEnumerable.GetEnumerator();
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public static class IBindableVectorView_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, out IntPtr result);
        public unsafe delegate int IndexOf_2(IntPtr thisPtr, IntPtr value, out uint index, out byte returnValue);
    }
}

namespace ABI.System.Collections
{
    using global::System;
    using global::System.Runtime.CompilerServices;
    using global::Windows.UI.Xaml.Interop;

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    public class IEnumerable : global::System.Collections.IEnumerable, IBindableIterable
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.IEnumerable obj) =>
            ComWrappersSupport.CreateCCWForObject(obj).As<Vftbl>(GuidGenerator.GetIID(typeof(IEnumerable)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.IEnumerable FromAbi(IntPtr thisPtr) =>
            new IEnumerable(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.IEnumerable value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<IBindableIterable>.DisposeAbi(abi);

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

            public global::System.Collections.IEnumerator GetEnumerator()
            {
                var first = _iterable.First();
                // translate ibindableiterator to iiterator<object>
                // then convert generic iiterator to nongeneric enumerator
                //if (first is global::ABI.System.Collections.IEnumerator iterable)
                //{
                //    return iterable;
                //}
                throw new InvalidOperationException("Unexpected type for enumerator");
            }
        }

        public class ToAbiHelper : IBindableIterable
        {
            private readonly IEnumerable m_enumerable;

            internal ToAbiHelper(IEnumerable enumerable) => m_enumerable = enumerable;

            public IBindableIterator First() => MakeBindableIterator(m_enumerable.GetEnumerator());

            public static IBindableIterator MakeBindableIterator(IEnumerator enumerator) =>
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
            public IEnumerable_Delegates.First_0 First_0;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    First_0 = Do_Abi_First_0
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 1);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_First_0(IntPtr thisPtr, out IntPtr result)
            {
                result = default;
                try
                {
                    var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IEnumerable>(thisPtr);
                    result = MarshalInterface<global::System.Collections.IEnumerator>.FromManaged(__this.GetEnumerator());
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

        public unsafe global::Windows.UI.Xaml.Interop.IBindableIterator First()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.First_0(ThisPtr, out __retval));
                return MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableIterator>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableIterator>.DisposeAbi(__retval);
            }
        }

        public IEnumerator GetEnumerator() => _FromIterable.GetEnumerator();
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public static class IEnumerable_Delegates
    {
        public unsafe delegate int First_0(IntPtr thisPtr, out IntPtr result);
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
    public class IList : global::System.Collections.IList
    {
        public static IObjectReference CreateMarshaler(global::System.Collections.IList obj) =>
            ComWrappersSupport.CreateCCWForObject(obj).As<Vftbl>(GuidGenerator.GetIID(typeof(IList)));

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.IList FromAbi(IntPtr thisPtr) =>
            new IList(ObjRefFromAbi(thisPtr));

        public static IntPtr FromManaged(global::System.Collections.IList value) =>
            (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeAbi(IntPtr abi) =>
            MarshalInterfaceHelper<IBindableVector>.DisposeAbi(abi);

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
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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
                    throw new ArgumentException(ErrorStrings.Arg_RankMultiDimNotSupported);

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
                    throw new ArgumentException(ErrorStrings.Argument_InsufficientSpaceToCopyCollection);

                if (arrayIndex - destLB > destLen)
                    throw new ArgumentException(ErrorStrings.Argument_IndexOutOfArrayBounds);

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
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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
                    throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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
                        throw new InvalidOperationException(ErrorStrings.InvalidOperation_CollectionBackingListTooLarge);
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
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
                }
            }

            public uint Size { get => (uint)_list.Count; }
            
            public IBindableVectorView GetView()
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
                    throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
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
                    Exception e = new InvalidOperationException(ErrorStrings.InvalidOperation_CannotRemoveLastFromEmptyCollection);
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
                    Exception e = new ArgumentOutOfRangeException(nameof(index), ErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
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
                        Exception e = new ArgumentOutOfRangeException(nameof(index), ErrorStrings.ArgumentOutOfRange_IndexLargerThanMaxValue);
                        e.SetHResult(ExceptionHelpers.E_BOUNDS);
                        throw e;
                    }
                }

                // IBindableIterable implementation:

                public IBindableIterator First() =>
                    IEnumerable.ToAbiHelper.MakeBindableIterator(list.GetEnumerator());

                // IBindableVectorView implementation:

                public object GetAt(uint index)
                {
                    EnsureIndexInt32(index, list.Count);

                    try
                    {
                        return list[(int)index];
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        throw ex.GetExceptionForHR(ExceptionHelpers.E_BOUNDS, ErrorStrings.ArgumentOutOfRange_Index);
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

                public IEnumerator GetEnumerator()
                {
                    throw new NotImplementedException();
                }
            }
        }

        [Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public IList_Delegates.GetAt_0 GetAt_0;
            public _get_PropertyAsUInt32 get_Size_1;
            public IList_Delegates.GetView_2 GetView_2;
            public IList_Delegates.IndexOf_3 IndexOf_3;
            public IList_Delegates.SetAt_4 SetAt_4;
            public IList_Delegates.InsertAt_5 InsertAt_5;
            public IList_Delegates.RemoveAt_6 RemoveAt_6;
            public IList_Delegates.Append_7 Append_7;
            public IList_Delegates.RemoveAtEnd_8 RemoveAtEnd_8;
            public IList_Delegates.Clear_9 Clear_9;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    GetAt_0 = Do_Abi_GetAt_0,
                    get_Size_1 = Do_Abi_get_Size_1,
                    GetView_2 = Do_Abi_GetView_2,
                    IndexOf_3 = Do_Abi_IndexOf_3,
                    SetAt_4 = Do_Abi_SetAt_4,
                    InsertAt_5 = Do_Abi_InsertAt_5,
                    RemoveAt_6 = Do_Abi_RemoveAt_6,
                    Append_7 = Do_Abi_Append_7,
                    RemoveAtEnd_8 = Do_Abi_RemoveAtEnd_8,
                    Clear_9 = Do_Abi_Clear_9
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 10);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static ConditionalWeakTable<global::System.Collections.IList, ToAbiHelper> _adapterTable =
                new ConditionalWeakTable<global::System.Collections.IList, ToAbiHelper>();

            private static ToAbiHelper FindAdapter(IntPtr thisPtr)
            {
                var __this = global::WinRT.ComWrappersSupport.FindObject<global::System.Collections.IList>(thisPtr);
                return _adapterTable.GetValue(__this, (list) => new ToAbiHelper(list));
            }

            private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, out IntPtr result)
            {
                object __result = default;

                result = default;

                try
                {
                    __result = FindAdapter(thisPtr).GetAt(index);
                    result = MarshalInspectable.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, out IntPtr result)
            {
                global::Windows.UI.Xaml.Interop.IBindableVectorView __result = default;

                result = default;

                try
                {
                    __result = FindAdapter(thisPtr).GetView();
                    result = MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableVectorView>.FromManaged(__result);
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_IndexOf_3(IntPtr thisPtr, IntPtr value, out uint index, out byte returnValue)
            {
                bool __returnValue = default;

                index = default;
                returnValue = default;
                uint __index = default;

                try
                {
                    __returnValue = FindAdapter(thisPtr).IndexOf(MarshalInspectable.FromAbi(value), out __index);
                    index = __index;
                    returnValue = (byte)(__returnValue ? 1 : 0);
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
                    FindAdapter(thisPtr).SetAt(index, MarshalInspectable.FromAbi(value));
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
                    FindAdapter(thisPtr).InsertAt(index, MarshalInspectable.FromAbi(value));
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
                    FindAdapter(thisPtr).Append(MarshalInspectable.FromAbi(value));
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
            private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, out uint value)
            {
                uint __value = default;

                value = default;

                try
                {
                    __value = FindAdapter(thisPtr).Size;
                    value = __value;
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
            _vectorToList = new ABI.System.Collections.IList.FromAbiHelper(ObjRef);
        }
        ABI.System.Collections.IList.FromAbiHelper _vectorToList;

        public unsafe object GetAt(uint index)
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetAt_0(ThisPtr, index, out __retval));
                return MarshalInspectable.FromAbi(__retval);
            }
            finally
            {
                MarshalInspectable.DisposeAbi(__retval);
            }
        }

        public unsafe global::Windows.UI.Xaml.Interop.IBindableVectorView GetView()
        {
            IntPtr __retval = default;
            try
            {
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.GetView_2(ThisPtr, out __retval));
                return MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableVectorView>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::Windows.UI.Xaml.Interop.IBindableVectorView>.DisposeAbi(__retval);
            }
        }

        public unsafe bool IndexOf(object value, out uint index)
        {
            IObjectReference __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable.CreateMarshaler(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.IndexOf_3(ThisPtr, MarshalInspectable.GetAbi(__value), out __index, out __retval));
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__value);
            }
        }

        public unsafe void SetAt(uint index, object value)
        {
            IObjectReference __value = default;
            try
            {
                __value = MarshalInspectable.CreateMarshaler(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.SetAt_4(ThisPtr, index, MarshalInspectable.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__value);
            }
        }

        public unsafe void InsertAt(uint index, object value)
        {
            IObjectReference __value = default;
            try
            {
                __value = MarshalInspectable.CreateMarshaler(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.InsertAt_5(ThisPtr, index, MarshalInspectable.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__value);
            }
        }

        public unsafe void RemoveAt(uint index)
        {
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.RemoveAt_6(ThisPtr, index));
        }

        public unsafe void Append(object value)
        {
            IObjectReference __value = default;
            try
            {
                __value = MarshalInspectable.CreateMarshaler(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.Append_7(ThisPtr, MarshalInspectable.GetAbi(__value)));
            }
            finally
            {
                MarshalInspectable.DisposeMarshaler(__value);
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
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Size_1(ThisPtr, out __retval));
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
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public static class IList_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, out IntPtr result);
        public unsafe delegate int GetView_2(IntPtr thisPtr, out IntPtr result);
        public unsafe delegate int IndexOf_3(IntPtr thisPtr, IntPtr value, out uint index, out byte returnValue);
        public unsafe delegate int SetAt_4(IntPtr thisPtr, uint index, IntPtr value);
        public unsafe delegate int InsertAt_5(IntPtr thisPtr, uint index, IntPtr value);
        public unsafe delegate int RemoveAt_6(IntPtr thisPtr, uint index);
        public unsafe delegate int Append_7(IntPtr thisPtr, IntPtr value);
        public unsafe delegate int RemoveAtEnd_8(IntPtr thisPtr);
        public unsafe delegate int Clear_9(IntPtr thisPtr);
    }
}
