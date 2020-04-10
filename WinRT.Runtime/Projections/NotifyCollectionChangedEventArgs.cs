using ABI.Windows.UI.Xaml.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace ABI.Windows.UI.Xaml.Interop
{
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("4CF68D33-E3F2-4964-B85E-945B4F7E2F21")]
    internal class INotifyCollectionChangedEventArgs
    {
        [Guid("4CF68D33-E3F2-4964-B85E-945B4F7E2F21")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public INotifyCollectionChangedEventArgs_Delegates.get_Action_0 get_Action_0;
            public INotifyCollectionChangedEventArgs_Delegates.get_NewItems_1 get_NewItems_1;
            public INotifyCollectionChangedEventArgs_Delegates.get_OldItems_2 get_OldItems_2;
            public _get_PropertyAsInt32 get_NewStartingIndex_3;
            public _get_PropertyAsInt32 get_OldStartingIndex_4;
        }
        internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator INotifyCollectionChangedEventArgs(IObjectReference obj) => (obj != null) ? new INotifyCollectionChangedEventArgs(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
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
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_Action_0(ThisPtr, out __retval));
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
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_NewItems_1(ThisPtr, out __retval));
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
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_NewStartingIndex_3(ThisPtr, out __retval));
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
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_OldItems_2(ThisPtr, out __retval));
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
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.get_OldStartingIndex_4(ThisPtr, out __retval));
                return __retval;
            }
        }
    }
    internal static class INotifyCollectionChangedEventArgs_Delegates
    {
        public unsafe delegate int get_Action_0(IntPtr thisPtr, out global::System.Collections.Specialized.NotifyCollectionChangedAction value);
        public unsafe delegate int get_NewItems_1(IntPtr thisPtr, out IntPtr value);
        public unsafe delegate int get_OldItems_2(IntPtr thisPtr, out IntPtr value);
    }


    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
    [Guid("B30C3E3A-DF8D-44A5-9A38-7AC0D08CE63D")]
    internal class WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory
    {
        [Guid("B30C3E3A-DF8D-44A5-9A38-7AC0D08CE63D")]
        [StructLayout(LayoutKind.Sequential)]
        public struct Vftbl
        {
            public unsafe delegate int _CreateInstanceWithAllParameters_0(IntPtr thisPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction action, IntPtr newItems, IntPtr oldItems, int newIndex, int oldIndex, IntPtr baseInterface, out IntPtr innerInterface, out IntPtr value);
            internal IInspectable.Vftbl IInspectableVftbl;
            public _CreateInstanceWithAllParameters_0 CreateInstanceWithAllParameters_0;
        }
        public static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

        public static implicit operator WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(IObjectReference obj) => (obj != null) ? new WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(obj) : null;
        public static implicit operator WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(ObjectReference<Vftbl> obj) => (obj != null) ? new WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory(obj) : null;
        protected readonly ObjectReference<Vftbl> _obj;
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
            IObjectReference __newItems = default;
            IObjectReference __oldItems = default;
            IObjectReference __baseInterface = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
                __newItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler(newItems);
                __oldItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler(oldItems);
                __baseInterface = MarshalInspectable.CreateMarshaler(baseInterface);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(_obj.Vftbl.CreateInstanceWithAllParameters_0(ThisPtr, action, MarshalInterface<global::System.Collections.IList>.GetAbi(__newItems), MarshalInterface<global::System.Collections.IList>.GetAbi(__oldItems), newIndex, oldIndex, MarshalInspectable.GetAbi(__baseInterface), out __innerInterface, out __retval));
                innerInterface = ObjectReference<IUnknownVftbl>.FromAbi(__innerInterface);
                return ObjectReference<IUnknownVftbl>.FromAbi(__retval);
            }
            finally
            {
                MarshalInterface<global::System.Collections.IList>.DisposeMarshaler(__newItems);
                MarshalInterface<global::System.Collections.IList>.DisposeMarshaler(__oldItems);
                MarshalInspectable.DisposeMarshaler(__baseInterface);
                MarshalInspectable.DisposeAbi(__innerInterface);
                MarshalInspectable.DisposeAbi(__retval);
            }
        }
    }
}

namespace ABI.System.Collections.Specialized
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [StructLayout(LayoutKind.Sequential)]
    public struct NotifyCollectionChangedEventArgs
    {
        private static WeakLazy<ActivationFactory> _propertyChangedArgsFactory = new WeakLazy<ActivationFactory>();

        private class ActivationFactory : BaseActivationFactory
        {
            public ActivationFactory() : base("Windows.UI.Xaml.Interop", "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs")
            {
            }
        }

        public static IObjectReference CreateMarshaler(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory factory = _propertyChangedArgsFactory.Value._As<WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory.Vftbl>();
            return factory.CreateInstanceWithAllParameters(value.Action, value.NewItems, value.OldItems, value.NewStartingIndex, value.OldStartingIndex, null, out _);
        }

        public static IntPtr GetAbi(IObjectReference m) => m.ThisPtr;

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
    global::System.Collections.Specialized.NotifyCollectionChangedAction action, global::System.Collections.IList newItems, global::System.Collections.IList oldItems, int newStartingIndex, int oldStartingIndex) =>
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
            using var objRef = CreateMarshaler(o);
            *(IntPtr*)dest.ToPointer() = objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static IntPtr FromManaged(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static void DisposeMarshaler(IObjectReference m) { m.Dispose(); }
        public static void DisposeAbi(IntPtr abi) { using var objRef = ObjectReference<IUnknownVftbl>.Attach(ref abi); }

        public static string GetGuidSignature()
        {
            return "rc(Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})";
        }
    }
}
