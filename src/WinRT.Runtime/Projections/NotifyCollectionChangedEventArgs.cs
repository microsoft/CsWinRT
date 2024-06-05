// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Microsoft.UI.Xaml.Interop;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;

namespace ABI.Microsoft.UI.Xaml.Interop
{
#if !NET
    [global::WinRT.ObjectReferenceWrapper(nameof(_obj))]
#endif
    [Guid("5108EBA4-4892-5A20-8374-A96815E0FD27")]
    internal sealed unsafe class WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory
    {
        private readonly IObjectReference _obj;
        public IntPtr ThisPtr => _obj.ThisPtr;

        public static readonly WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory Instance = new();

        private WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory()
        {
#if NET
            _obj = FeatureSwitches.UseWindowsUIXamlProjections
                ? ActivationFactory.Get("Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", IID.IID_WUX_INotifyCollectionChangedEventArgsFactory)
                : ActivationFactory.Get("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", IID.IID_MUX_INotifyCollectionChangedEventArgsFactory);
#else
            _obj = ActivationFactory.Get<IUnknownVftbl>("Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", IID.IID_MUX_INotifyCollectionChangedEventArgsFactory);
#endif
        }

        public unsafe ObjectReferenceValue CreateInstanceWithAllParameters(global::System.Collections.Specialized.NotifyCollectionChangedAction action, global::System.Collections.IList newItems, global::System.Collections.IList oldItems, int newIndex, int oldIndex)
        {
            ObjectReferenceValue __newItems = default;
            ObjectReferenceValue __oldItems = default;
            IntPtr __innerInterface = default;
            IntPtr __retval = default;
            try
            {
#if NET
                __newItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(newItems, global::ABI.System.Collections.IListMethods.IID);
                __oldItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(oldItems, global::ABI.System.Collections.IListMethods.IID);
#else
                __newItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(newItems);
                __oldItems = MarshalInterface<global::System.Collections.IList>.CreateMarshaler2(oldItems);
#endif
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction, IntPtr, IntPtr, int, int, IntPtr, IntPtr*, IntPtr*, int>**)ThisPtr)[6](
                    ThisPtr,
                    action,
                    MarshalInterface<global::System.Collections.IList>.GetAbi(__newItems),
                    MarshalInterface<global::System.Collections.IList>.GetAbi(__oldItems),
                    newIndex,
                    oldIndex,
                    IntPtr.Zero,
                    &__innerInterface,
                    &__retval));
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
    [WuxMuxProjectedType(wuxIID: "4cf68d33-e3f2-4964-b85e-945b4f7e2f21", muxIID: "DA049FF2-D2E0-5FE8-8C7B-F87F26060B6F")]
#if EMBED
    internal
#else
    public
#endif
    struct NotifyCollectionChangedEventArgs
    {
        private static readonly Guid Interface_IID = 
            FeatureSwitches.UseWindowsUIXamlProjections
                ? IID.IID_WUX_INotifyCollectionChangedEventArgs
                : IID.IID_MUX_INotifyCollectionChangedEventArgs;

        public static IObjectReference CreateMarshaler(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return null;
            }

            ObjectReferenceValue _value = default;
            try
            {
                _value = CreateMarshaler2(value);
                return ObjectReference<IUnknownVftbl>.FromAbi(_value.GetAbi(), IID.IID_IUnknown);
            }
            finally
            {
                _value.Dispose();
            }
        }

        public static ObjectReferenceValue CreateMarshaler2(global::System.Collections.Specialized.NotifyCollectionChangedEventArgs value)
        {
            if (value is null)
            {
                return new ObjectReferenceValue();
            }

            return WinRTNotifyCollectionChangedEventArgsRuntimeClassFactory.Instance.CreateInstanceWithAllParameters(value.Action, value.NewItems, value.OldItems, value.NewStartingIndex, value.OldStartingIndex);
        }

        public static IntPtr GetAbi(IObjectReference m) => m?.ThisPtr ?? IntPtr.Zero;

        public static global::System.Collections.Specialized.NotifyCollectionChangedEventArgs FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            return CreateNotifyCollectionChangedEventArgs(ptr);
        }

        private static unsafe global::System.Collections.Specialized.NotifyCollectionChangedEventArgs CreateNotifyCollectionChangedEventArgs(IntPtr ptr)
        {
            IntPtr thisPtr = default;
            global::System.Collections.Specialized.NotifyCollectionChangedAction action = default;
            IntPtr newItems = default;
            IntPtr oldItems = default;
            int newStartingIndex = default;
            int oldStartingIndex = default;

            try
            {
                // Call can come from CreateObject which means it might not be on the right interface.
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR(Marshal.QueryInterface(ptr, ref Unsafe.AsRef(in Interface_IID), out thisPtr));

                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, global::System.Collections.Specialized.NotifyCollectionChangedAction*, int>**)thisPtr)[6](thisPtr, &action));
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)thisPtr)[7](thisPtr, &newItems));
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)thisPtr)[8](thisPtr, &oldItems));
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, int>**)thisPtr)[9](thisPtr, &newStartingIndex));
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, int*, int>**)thisPtr)[10](thisPtr, &oldStartingIndex));
                return CreateNotifyCollectionChangedEventArgs(
                    action,
                    MarshalInterface<global::System.Collections.IList>.FromAbi(newItems),
                    MarshalInterface<global::System.Collections.IList>.FromAbi(oldItems),
                    newStartingIndex,
                    oldStartingIndex);
            }
            finally
            {
                MarshalInterface<global::System.Collections.IList>.DisposeAbi(newItems);
                MarshalInterface<global::System.Collections.IList>.DisposeAbi(oldItems);
                Marshal.Release(thisPtr);
            }
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
            if (FeatureSwitches.UseWindowsUIXamlProjections)
            {
                return "rc(Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})";
            }
            return "rc(Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs;{4cf68d33-e3f2-4964-b85e-945b4f7e2f21})";
        }
    }
}
