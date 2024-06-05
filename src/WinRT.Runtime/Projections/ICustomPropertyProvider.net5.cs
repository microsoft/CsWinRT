// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using WinRT;

// These types have the same GUIDs in both Microsoft.UI.Xaml and Windows.UI.Xaml,
// so we don't need to duplicate them for the internal usage here as they can be transparently used by both WUX and MUX.
namespace Microsoft.UI.Xaml.Data
{
    [global::WinRT.WindowsRuntimeType]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Data.ICustomProperty))]
    [Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782")]
    interface ICustomProperty
    {
        object GetValue(object target);
        void SetValue(object target, object value);
        object GetIndexedValue(object target, object index);
        void SetIndexedValue(object target, object value, object index);
        bool CanRead { get; }
        bool CanWrite { get; }
        string Name { get; }
        global::System.Type Type { get; }
    }
}

namespace ABI.Microsoft.UI.Xaml.Data
{
    [Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782")]
    internal unsafe interface ICustomProperty
    {
        public static readonly IntPtr AbiToProjectionVftablePtr;
        public static global::System.Guid IID => global::WinRT.Interop.IID.IID_ICustomProperty;

        static unsafe ICustomProperty()
        {
            AbiToProjectionVftablePtr = ComWrappersSupport.AllocateVtableMemory(typeof(ICustomProperty), sizeof(IInspectable.Vftbl) + sizeof(IntPtr) * 8);
            *(IInspectable.Vftbl*)AbiToProjectionVftablePtr = IInspectable.Vftbl.AbiToProjectionVftable;
            ((delegate* unmanaged<IntPtr, global::ABI.System.Type*, int>*)AbiToProjectionVftablePtr)[6] = &Do_Abi_get_Type_0;
            ((delegate* unmanaged<IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[7] = &Do_Abi_get_Name_1;
            ((delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[8] = &Do_Abi_GetValue_2;
            ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, int>*)AbiToProjectionVftablePtr)[9] = &Do_Abi_SetValue_3;
            ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr*, int>*)AbiToProjectionVftablePtr)[10] = &Do_Abi_GetIndexedValue_4;
            ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, int>*)AbiToProjectionVftablePtr)[11] = &Do_Abi_SetIndexedValue_5;
            ((delegate* unmanaged<IntPtr, byte*, int>*)AbiToProjectionVftablePtr)[12] = &Do_Abi_get_CanWrite_6;
            ((delegate* unmanaged<IntPtr, byte*, int>*)AbiToProjectionVftablePtr)[13] = &Do_Abi_get_CanRead_7;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_GetValue_2(IntPtr thisPtr, IntPtr target, IntPtr* result)
        {
            object __result = default;

            *result = default;

            try
            {
                __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).GetValue(MarshalInspectable<object>.FromAbi(target)); 
                *result = MarshalInspectable<object>.FromManaged(__result);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_SetValue_3(IntPtr thisPtr, IntPtr target, IntPtr value)
        {
            try
            {
                global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).SetValue(MarshalInspectable<object>.FromAbi(target), MarshalInspectable<object>.FromAbi(value));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_GetIndexedValue_4(IntPtr thisPtr, IntPtr target, IntPtr index, IntPtr* result)
        {
            object __result = default;

            try
            {
                __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).GetIndexedValue(MarshalInspectable<object>.FromAbi(target), MarshalInspectable<object>.FromAbi(index)); 
                *result = MarshalInspectable<object>.FromManaged(__result);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_SetIndexedValue_5(IntPtr thisPtr, IntPtr target, IntPtr value, IntPtr index)
        {
            try
            {
                global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).SetIndexedValue(MarshalInspectable<object>.FromAbi(target), MarshalInspectable<object>.FromAbi(value), MarshalInspectable<object>.FromAbi(index));
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_get_CanRead_7(IntPtr thisPtr, byte* value)
        {
            bool __value = default;

            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).CanRead; *value = (byte)(__value ? 1 : 0);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_get_CanWrite_6(IntPtr thisPtr, byte* value)
        {
            bool __value = default;

            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).CanWrite; *value = (byte)(__value ? 1 : 0);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_get_Name_1(IntPtr thisPtr, IntPtr* value)
        {
            string __value = default;

            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).Name; 
                *value = MarshalString.FromManaged(__value);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe int Do_Abi_get_Type_0(IntPtr thisPtr, global::ABI.System.Type* value)
        {
            global::System.Type __value = default;

            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).Type; 
                *value = global::ABI.System.Type.FromManaged(__value);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }
    internal static class ICustomProperty_Delegates
    {
        public unsafe delegate int get_Type_0(IntPtr thisPtr, global::ABI.System.Type* value);
        public unsafe delegate int get_Name_1(IntPtr thisPtr, IntPtr* value);
        public unsafe delegate int GetValue_2(IntPtr thisPtr, IntPtr target, IntPtr* result);
        public unsafe delegate int SetValue_3(IntPtr thisPtr, IntPtr target, IntPtr value);
        public unsafe delegate int GetIndexedValue_4(IntPtr thisPtr, IntPtr target, IntPtr index, IntPtr* result);
        public unsafe delegate int SetIndexedValue_5(IntPtr thisPtr, IntPtr target, IntPtr value, IntPtr index);
        public unsafe delegate int get_CanWrite_6(IntPtr thisPtr, byte* value);
        public unsafe delegate int get_CanRead_7(IntPtr thisPtr, byte* value);
    }

    internal sealed class ManagedCustomPropertyWinRTTypeDetails : global::WinRT.IWinRTExposedTypeDetails
    {
        public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
        {
            return new ComWrappers.ComInterfaceEntry[]
            {
                new ComWrappers.ComInterfaceEntry
                {
                    IID = ICustomProperty.IID,
                    Vtable = ICustomProperty.AbiToProjectionVftablePtr
                },
            };
        }
    }

    [global::WinRT.WinRTExposedType(typeof(ManagedCustomPropertyWinRTTypeDetails))]
    internal sealed class ManagedCustomProperty : global::Microsoft.UI.Xaml.Data.ICustomProperty
    {
        private readonly PropertyInfo _property;

        public ManagedCustomProperty(PropertyInfo property)
        {
            _property = property;
        }

        public bool CanRead => _property.CanRead;

        public bool CanWrite => _property.CanWrite;

        public string Name => _property.Name;

        public Type Type => _property.PropertyType;

        public object GetIndexedValue(object target, object index)
        {
            return _property.GetValue(target, new[] { index });
        }

        public object GetValue(object target)
        {
            return _property.GetValue(target);
        }

        public void SetIndexedValue(object target, object value, object index)
        {
            _property.SetValue(target, value, new[] { index });
        }

        public void SetValue(object target, object value)
        {
            _property.SetValue(target, value);
        }
    }

    [Guid("7C925755-3E48-42B4-8677-76372267033F")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ManagedCustomPropertyProviderVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        private void* GetCustomProperty_0;
        private void* GetIndexedProperty_1;
        private void* GetStringRepresentation_2;
        private void* get_Type_3;

        private static readonly ManagedCustomPropertyProviderVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;

        static unsafe ManagedCustomPropertyProviderVftbl()
        {
            AbiToProjectionVftable = new ManagedCustomPropertyProviderVftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,

                GetCustomProperty_0 = (delegate* unmanaged<IntPtr, IntPtr, IntPtr*, int>)&Do_Abi_GetCustomProperty_0,
                GetIndexedProperty_1 = (delegate* unmanaged<IntPtr, IntPtr, global::ABI.System.Type, IntPtr*, int>)&Do_Abi_GetIndexedProperty_1,
                GetStringRepresentation_2 = (delegate* unmanaged<IntPtr, IntPtr*, int>)&Do_Abi_GetStringRepresentation_2,
                get_Type_3 = (delegate* unmanaged<IntPtr, global::ABI.System.Type*, int>)&Do_Abi_get_Type_3

            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(ManagedCustomPropertyProviderVftbl), sizeof(global::WinRT.IInspectable.Vftbl) + sizeof(IntPtr) * 4);
            *(ManagedCustomPropertyProviderVftbl*)nativeVftbl = AbiToProjectionVftable;
            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }


        [UnmanagedCallersOnly]

        private static unsafe int Do_Abi_GetCustomProperty_0(IntPtr thisPtr, IntPtr name, IntPtr* result)
        {
            global::Microsoft.UI.Xaml.Data.ICustomProperty __result = default;
            try
            {
                string _name = MarshalString.FromAbi(name);
                object target = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                PropertyInfo propertyInfo = target.GetType().GetProperty(
                     _name,
                     BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

                if (propertyInfo is object)
                {
                    __result = new ManagedCustomProperty(propertyInfo);
                }
                
                *result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]

        private static unsafe int Do_Abi_GetIndexedProperty_1(IntPtr thisPtr, IntPtr name, global::ABI.System.Type type, IntPtr* result)
        {
            global::Microsoft.UI.Xaml.Data.ICustomProperty __result = default;
            try
            {
                string _name = MarshalString.FromAbi(name);
                object target = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);
                PropertyInfo propertyInfo = target.GetType().GetProperty(
                    _name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
                    null,                                                                   // default binder
                    null,                                                                   // ignore return type
                    new Type[] { global::ABI.System.Type.FromAbi(type) },                   // indexed parameter type
                    null                                                                    // ignore type modifier
                    );

                if (propertyInfo is object)
                {
                    __result = new ManagedCustomProperty(propertyInfo);
                }

                *result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]

        private static unsafe int Do_Abi_GetStringRepresentation_2(IntPtr thisPtr, IntPtr* result)
        {
            string __result = default;
            try
            {
                __result = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).ToString();
                *result = MarshalString.FromManaged(__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }

        [UnmanagedCallersOnly]

        private static unsafe int Do_Abi_get_Type_3(IntPtr thisPtr, global::ABI.System.Type* value)
        {
            global::System.Type __value = default;
            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).GetType();
                *value = global::ABI.System.Type.FromManaged(__value);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }
}
