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

namespace Microsoft.UI.Xaml.Interop
{
    [global::WinRT.WindowsRuntimeType]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Interop.IBindableIterable))]
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
    [DynamicInterfaceCastableImplementation]
    [Guid("036D2C08-DF29-41AF-8AA2-D774BE62BA6F")]
    internal unsafe interface IBindableIterable : global::Microsoft.UI.Xaml.Interop.IBindableIterable, ABI.System.Collections.IEnumerable
    {
        
    }

    [DynamicInterfaceCastableImplementation]
    [Guid("6A1D6C07-076D-49F2-8314-F52C9C9A8331")]
    internal unsafe interface IBindableIterator : global::Microsoft.UI.Xaml.Interop.IBindableIterator
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IBindableIterator()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IBindableIterator), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 4);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_get_Current_0;
            ((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_HasCurrent_1;
            ((delegate* unmanaged[Stdcall]<IntPtr, byte*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_MoveNext_2;
            ((delegate* unmanaged[Stdcall]<IntPtr, int, IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[9] = &Do_Abi_GetMany_3;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        internal static ObjectReference<IUnknownVftbl> FromAbi(IntPtr thisPtr) => ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);

        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableIterator.MoveNext()
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            byte __retval = default;
            global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)ThisPtr)[8](ThisPtr, &__retval));
            return __retval != 0;
        }

        unsafe uint global::Microsoft.UI.Xaml.Interop.IBindableIterator.GetMany(ref object[] items)
        {
            // Should never be called.
            throw new NotImplementedException();
        }

        unsafe object global::Microsoft.UI.Xaml.Interop.IBindableIterator.Current
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                IntPtr __retval = default;
                try
                {
                    global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>**)ThisPtr)[6](ThisPtr, &__retval));
                    return MarshalInspectable<object>.FromAbi(__retval);
                }
                finally
                {
                    MarshalInspectable<object>.DisposeAbi(__retval);
                }
            }
        }

        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableIterator.HasCurrent
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                byte __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, byte*, int>**)ThisPtr)[7](ThisPtr, &__retval));
                return __retval != 0;
            }
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

    [DynamicInterfaceCastableImplementation]
    [Guid("346DD6E7-976E-4BC3-815D-ECE243BC0F33")]
    internal unsafe interface IBindableVectorView : global::Microsoft.UI.Xaml.Interop.IBindableVectorView
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;
        static IBindableVectorView()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(IBindableVectorView), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 3);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_GetAt_0;
            ((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_Size_1;
            ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_IndexOf_2;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
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

        internal static ObjectReference<IUnknownVftbl> FromAbi(IntPtr thisPtr) => ObjectReference<IUnknownVftbl>.FromAbi(thisPtr);

        private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<IWinRTObject, ABI.System.Collections.IEnumerable.AdaptiveFromAbiHelper> _helperTable = new();

        unsafe object global::Microsoft.UI.Xaml.Interop.IBindableVectorView.GetAt(uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
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

        unsafe bool global::Microsoft.UI.Xaml.Interop.IBindableVectorView.IndexOf(object value, out uint index)
        {
            var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
            var ThisPtr = _obj.ThisPtr;
            ObjectReferenceValue __value = default;
            uint __index = default;
            byte __retval = default;
            try
            {
                __value = MarshalInspectable<object>.CreateMarshaler2(value);
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint*, byte*, int>**)ThisPtr)[8](
                    ThisPtr,
                    MarshalInspectable<object>.GetAbi(__value),
                    &__index,
                    &__retval));
                index = __index;
                return __retval != 0;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(__value);
            }
        }

        unsafe uint global::Microsoft.UI.Xaml.Interop.IBindableVectorView.Size
        {
            get
            {
                var _obj = ((IWinRTObject)this).GetObjectReferenceForType(typeof(global::Microsoft.UI.Xaml.Interop.IBindableIterator).TypeHandle);
                var ThisPtr = _obj.ThisPtr;
                uint __retval = default;
                global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, uint*, int>**)ThisPtr)[7](ThisPtr, &__retval));
                return __retval;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _helperTable.GetValue((IWinRTObject)this,
                        (enumerable) => new ABI.System.Collections.IEnumerable.AdaptiveFromAbiHelper(enumerable)
                   ).GetEnumerator();
        }
    }
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IBindableVectorView_Delegates
    {
        public unsafe delegate int GetAt_0(IntPtr thisPtr, uint index, IntPtr* result);
        public unsafe delegate int get_Size_1(IntPtr thisPtr, uint* result);
        public unsafe delegate int IndexOf_2(IntPtr thisPtr, IntPtr value, uint* index, byte* returnValue);
    }
}
