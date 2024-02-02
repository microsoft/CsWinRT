// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Microsoft.UI.Xaml.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Microsoft.UI.Xaml.Interop
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("DA049FF2-D2E0-5FE8-8C7B-F87F26060B6F")]
    internal sealed unsafe class INotifyCollectionChangedEventArgs
    {
        [Guid("DA049FF2-D2E0-5FE8-8C7B-F87F26060B6F")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _get_Action_0;
            public delegate* unmanaged[Stdcall]<IntPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction*, int> get_Action_0 => (delegate* unmanaged[Stdcall]<IntPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction*, int>)_get_Action_0;
            private void* _get_NewItems_1;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_NewItems_1 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_NewItems_1;
            private void* _get_OldItems_2;
            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> get_OldItems_2 => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_get_OldItems_2;
            private void* _get_NewStartingIndex_3;
            public delegate* unmanaged[Stdcall]<IntPtr, int*, int> get_NewStartingIndex_3 => (delegate* unmanaged[Stdcall]<IntPtr, int*, int>)_get_NewStartingIndex_3;
            private void* _get_OldStartingIndex_4;
            public delegate* unmanaged[Stdcall]<IntPtr, int*, int> get_OldStartingIndex_4 => (delegate* unmanaged[Stdcall]<IntPtr, int*, int>)_get_OldStartingIndex_4;
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator INotifyCollectionChangedEventArgs(IObjectReference obj) => (obj != null) ? new INotifyCollectionChangedEventArgs(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IObjectReference ObjRef { get => _obj; }
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public INotifyCollectionChangedEventArgs(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        internal INotifyCollectionChangedEventArgs(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe global::System.Collections.Specialized.NotifyCollectionChangedAction Action
        {
            get
            {
                global::System.Collections.Specialized.NotifyCollectionChangedAction __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Action_0(thisPtr, &__retval));
                return __retval;
            }
        }

        public unsafe global::System.Collections.IList NewItems
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_NewItems_1(thisPtr, &__retval));
                    return MarshalInterface<global::System.Collections.IList>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInterface<global::System.Collections.IList>.DisposeAbi(__retval);
                }
            }
        }

        public unsafe int NewStartingIndex
        {
            get
            {
                int __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_NewStartingIndex_3(thisPtr, &__retval));
                return __retval;
            }
        }

        public unsafe global::System.Collections.IList OldItems
        {
            get
            {
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_OldItems_2(thisPtr, &__retval));
                    return MarshalInterface<global::System.Collections.IList>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInterface<global::System.Collections.IList>.DisposeAbi(__retval);
                }
            }
        }

        public unsafe int OldStartingIndex
        {
            get
            {
                int __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_OldStartingIndex_4(thisPtr, &__retval));
                return __retval;
            }
        }
    }

    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("5108EBA4-4892-5A20-8374-A96815E0FD27")]
    internal sealed unsafe class WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory
    {
        [Guid("5108EBA4-4892-5A20-8374-A96815E0FD27")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            private void* _CreateInstanceWithAllParameters_0;
            public delegate* unmanaged[Stdcall]<IntPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction, IntPtr, IntPtr, int, int, IntPtr, IntPtr*, IntPtr*, int> CreateInstanceWithAllParameters_0 => (delegate* unmanaged[Stdcall]<IntPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction, IntPtr, IntPtr, int, int, IntPtr, IntPtr*, IntPtr*, int>)_CreateInstanceWithAllParameters_0;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(IObjectReference obj) => (obj != null) ? new WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(obj) : null;
        public static implicit operator WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj) => (obj != null) ? new WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(obj) : null;
        private readonly ObjectReference<Vftbl> _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;
        public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
        public A As<A>() => _obj.AsType<A>();
        public WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(IObjectReference obj) : this(obj.As<Vftbl>()) { }
        public WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj)
        {
            _obj = obj;
        }

        public unsafe IObjectReference CreateInstanceWithAllParameters(global::System.Collections.Specialized.NotifyCollectionChangedAction action, global::System.Collections.IList newItems, global::System.Collections.IList oldItems, int newIndex, int oldIndex, object baseInterface, out IObjectReference innerInterface)
        {
            ObjectReferenceValue __newItems = default;
            ObjectReferenceValue __oldItems = default;
            ObjectReferenceValue __baseInterface = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                __newItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(newItems);
                __oldItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(oldItems);
                __baseInterface = MarshalInspectable<object>.CreateMarshaler2(baseInterface);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstanceWithAllParameters_0(thisPtr, action, MarshalInterface<global::System.Collections.IList>.GetAbi(__newItems), MarshalInterface<global::System.Collections.IList>.GetAbi(__oldItems), newIndex, oldIndex, MarshalInspectable<object>.GetAbi(__baseInterface), &__innerInterface, &__retval));
                innerInterface = ObjectReference<IUnknownVftbl>.FromAbi(__innerInterface);
                return ObjectReference<IUnknownVftbl>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::System.Collections.IList>.DisposeMarshaler(__newItems);
                MarshalInterface<global::System.Collections.IList>.DisposeMarshaler(__oldItems);
                MarshalInspectable<object>.DisposeMarshaler(__baseInterface);
                MarshalInspectable<object>.DisposeAbi(__innerInterface);
                MarshalInspectable<object>.DisposeAbi(__retval);
            }
        }

        public unsafe ObjectReferenceValue CreateInstanceWithAllParameters(global::System.Collections.Specialized.NotifyCollectionChangedAction action, global::System.Collections.IList newItems, global::System.Collections.IList oldItems, int newIndex, int oldIndex)
        {
            ObjectReferenceValue __newItems = default;
            ObjectReferenceValue __oldItems = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                __newItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(newItems);
                __oldItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(oldItems);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstanceWithAllParameters_0(thisPtr, action, MarshalInterface<global::System.Collections.IList>.GetAbi(__newItems), MarshalInterface<global::System.Collections.IList>.GetAbi(__oldItems), newIndex, oldIndex, IntPtr.Zero, &__innerInterface, &__retval));
                return new ObjectReferenceValue(__retval);
            }
            finally
            {
                __newItems.Dispose();
                __oldItems.Dispose();
                MarshalInspectable<object>.DisposeAbi(__innerInterface);
            }
        }
    }
}

namespace ABI.System.Collections.Specialized
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Sequential)]
#if EMBED
    internal
#else
    public
#endif
    struct NotifyCollectionChangedEventArgs
    {
        private static WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory Instance = ActivationFactory.Get("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs");

        public static IObjectReference CreateMarshaler(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            return Instance.CreateInstanceWithAllParameters(value.Action, value.NewItems, value.OldItems, value.NewStartingIndex, value.OldStartingIndex, null, out _);
        }

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return new ObjectReferenceValue();
            }

            return Instance.CreateInstanceWithAllParameters(value.Action, value.NewItems, value.OldItems, value.NewStartingIndex, value.OldStartingIndex);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Specialized.NotifyCollectionChangedEventArgs FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            INotifyCollectionChangedEventArgs args = INotifyCollectionChangedEventArgs.FromAbi(ptr);
            return CreateNotifyCollectionChangedEventArgs(args.Action, args.NewItems, args.OldItems, args.NewStartingIndex, args.OldStartingIndex);
        }

        private static global::System.Collections.Specialized.NotifyCollectionChangedEventArgs CreateNotifyCollectionChangedEventArgs(
            global::System.Collections.Specialized.NotifyCollectionChangedAction action,
            global::System.Collections.IList newItems,
            global::System.Collections.IList oldItems,
            int newStartingIndex,
            int oldStartingIndex) =>
            action switch
            {
                global::System.Collections.Specialized.NotifyCollectionChangedAction.Add => new global::System.Collections.Specialized.NotifyCollectionChangedEventArgs(action, newItems, newStartingIndex),
                global::System.Collections.Specialized.NotifyCollectionChangedAction.Remove => new global::System.Collections.Specialized.NotifyCollectionChangedEventArgs(action, oldItems, oldStartingIndex),
                global::System.Collections.Specialized.NotifyCollectionChangedAction.Replace => new global::System.Collections.Specialized.NotifyCollectionChangedEventArgs(action, newItems, oldItems, newStartingIndex),
                global::System.Collections.Specialized.NotifyCollectionChangedAction.Move => new global::System.Collections.Specialized.NotifyCollectionChangedEventArgs(action, newItems, newStartingIndex, oldStartingIndex),
                global::System.Collections.Specialized.NotifyCollectionChangedAction.Reset => new global::System.Collections.Specialized.NotifyCollectionChangedEventArgs(global::System.Collections.Specialized.NotifyCollectionChangedAction.Reset),
                _ => throw new ArgumentException(),
            };

        public static unsafe void CopyManaged(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs o, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o).Detach();
        }

        public static IntPtr FromManaged(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler2(value).Detach();
        }

        public static void DisposeMarshaler(IObjectReference m) { m?.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { MarshalInspectable<object>.DisposeAbi(abi); }

        public static unsafe MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.MarshalerArray CreateMarshalerArray(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs[] array) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.CreateMarshalerArray2(array, CreateMarshaler2);
        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.GetAbiArray(box);
        public static unsafe global::System.Collections.Specialized.NotifyCollectionChangedEventArgs[] FromAbiArray(object box) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.FromAbiArray(box, FromAbi);
        public static void CopyAbiArray(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs[] array, object box) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.CopyAbiArray(array, box, FromAbi);
        public static (int length, IntPtr data) FromManagedArray(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs[] array) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.FromManagedArray(array, FromManaged);
        public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.MarshalerArray array) => MarshalInterfaceHelper<global::System.Collections.Specialized.NotifyCollectionChangedEventArgs>.DisposeMarshalerArray(array);
        public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);

        public static string GetGuidSignature()
        {
            return "rc(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})";
        }
    }
}
