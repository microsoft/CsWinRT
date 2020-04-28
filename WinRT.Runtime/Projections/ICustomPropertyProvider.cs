using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using WinRT;
using WinRT.Interop;

namespace Microsoft.UI.Xaml.Data
{
    [global::WinRT.WindowsRuntimeType]
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
    internal class ICustomProperty
    {
        [Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782")]
        public struct Vftbl
        {
            internal IInspectable.Vftbl IInspectableVftbl;
            public ICustomProperty_Delegates.get_Type_0 get_Type_0;
            public _get_PropertyAsString get_Name_1;
            public ICustomProperty_Delegates.GetValue_2 GetValue_2;
            public ICustomProperty_Delegates.SetValue_3 SetValue_3;
            public ICustomProperty_Delegates.GetIndexedValue_4 GetIndexedValue_4;
            public ICustomProperty_Delegates.SetIndexedValue_5 SetIndexedValue_5;
            public _get_PropertyAsBoolean get_CanWrite_6;
            public _get_PropertyAsBoolean get_CanRead_7;

            private static readonly Vftbl AbiToProjectionVftable;
            public static readonly IntPtr AbiToProjectionVftablePtr;
            static unsafe Vftbl()
            {
                AbiToProjectionVftable = new Vftbl
                {
                    IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                    get_Type_0 = Do_Abi_get_Type_0,
                    get_Name_1 = Do_Abi_get_Name_1,
                    GetValue_2 = Do_Abi_GetValue_2,
                    SetValue_3 = Do_Abi_SetValue_3,
                    GetIndexedValue_4 = Do_Abi_GetIndexedValue_4,
                    SetIndexedValue_5 = Do_Abi_SetIndexedValue_5,
                    get_CanWrite_6 = Do_Abi_get_CanWrite_6,
                    get_CanRead_7 = Do_Abi_get_CanRead_7
                };
                var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(Vftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 8);
                Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
                AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
            }

            private static unsafe int Do_Abi_GetValue_2(IntPtr thisPtr, IntPtr target, out IntPtr result)
            {
                object __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).GetValue(MarshalInspectable.FromAbi(target)); 
                    result = MarshalInspectable.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_SetValue_3(IntPtr thisPtr, IntPtr target, IntPtr value)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).SetValue(MarshalInspectable.FromAbi(target), MarshalInspectable.FromAbi(value));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_GetIndexedValue_4(IntPtr thisPtr, IntPtr target, IntPtr index, out IntPtr result)
            {
                object __result = default;

                result = default;

                try
                {
                    __result = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).GetIndexedValue(MarshalInspectable.FromAbi(target), MarshalInspectable.FromAbi(index)); 
                    result = MarshalInspectable.FromManaged(__result);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_SetIndexedValue_5(IntPtr thisPtr, IntPtr target, IntPtr value, IntPtr index)
            {


                try
                {
                    global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).SetIndexedValue(MarshalInspectable.FromAbi(target), MarshalInspectable.FromAbi(value), MarshalInspectable.FromAbi(index));
                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_CanRead_7(IntPtr thisPtr, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).CanRead; value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_CanWrite_6(IntPtr thisPtr, out byte value)
            {
                bool __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).CanWrite; value = (byte)(__value ? 1 : 0);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Name_1(IntPtr thisPtr, out IntPtr value)
            {
                string __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).Name; 
                    value = MarshalString.FromManaged(__value);

                }
                catch (Exception __exception__)
                {
                    global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                    return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                }
                return 0;
            }
            private static unsafe int Do_Abi_get_Type_0(IntPtr thisPtr, out global::ABI.System.Type value)
            {
                global::System.Type __value = default;

                value = default;

                try
                {
                    __value = global::WinRT.ComWrappersSupport.FindObject<global::Microsoft.UI.Xaml.Data.ICustomProperty>(thisPtr).Type; 
                    value = global::ABI.System.Type.FromManaged(__value);
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
    internal static class ICustomProperty_Delegates
    {
        public unsafe delegate int get_Type_0(IntPtr thisPtr, out global::ABI.System.Type value);
        public unsafe delegate int GetValue_2(IntPtr thisPtr, IntPtr target, out IntPtr result);
        public unsafe delegate int SetValue_3(IntPtr thisPtr, IntPtr target, IntPtr value);
        public unsafe delegate int GetIndexedValue_4(IntPtr thisPtr, IntPtr target, IntPtr index, out IntPtr result);
        public unsafe delegate int SetIndexedValue_5(IntPtr thisPtr, IntPtr target, IntPtr value, IntPtr index);
    }

    internal class ManagedCustomProperty : global::Microsoft.UI.Xaml.Data.ICustomProperty
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
    internal struct ManagedCustomPropertyProviderVftbl
    {
        internal IInspectable.Vftbl IInspectableVftbl;
        internal ICustomPropertyProvider_Delegates.GetCustomProperty_0 GetCustomProperty_0;
        internal ICustomPropertyProvider_Delegates.GetIndexedProperty_1 GetIndexedProperty_1;
        internal ICustomPropertyProvider_Delegates.GetStringRepresentation_2 GetStringRepresentation_2;
        internal ICustomPropertyProvider_Delegates.get_Type_3 get_Type_3;

        private static readonly ManagedCustomPropertyProviderVftbl AbiToProjectionVftable;
        public static readonly IntPtr AbiToProjectionVftablePtr;
        static unsafe ManagedCustomPropertyProviderVftbl()
        {
            AbiToProjectionVftable = new ManagedCustomPropertyProviderVftbl
            {
                IInspectableVftbl = global::WinRT.IInspectable.Vftbl.AbiToProjectionVftable,
                GetCustomProperty_0 = Do_Abi_GetCustomProperty_0,
                GetIndexedProperty_1 = Do_Abi_GetIndexedProperty_1,
                GetStringRepresentation_2 = Do_Abi_GetStringRepresentation_2,
                get_Type_3 = Do_Abi_get_Type_3
            };
            var nativeVftbl = (IntPtr*)ComWrappersSupport.AllocateVtableMemory(typeof(ManagedCustomPropertyProviderVftbl), Marshal.SizeOf<global::WinRT.IInspectable.Vftbl>() + sizeof(IntPtr) * 4);
            Marshal.StructureToPtr(AbiToProjectionVftable, (IntPtr)nativeVftbl, false);
            AbiToProjectionVftablePtr = (IntPtr)nativeVftbl;
        }

        private static unsafe int Do_Abi_GetCustomProperty_0(IntPtr thisPtr, IntPtr name, out IntPtr result)
        {
            global::Microsoft.UI.Xaml.Data.ICustomProperty __result = default;

            result = default;

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
                
                result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetIndexedProperty_1(IntPtr thisPtr, IntPtr name, global::ABI.System.Type type, out IntPtr result)
        {
            global::Microsoft.UI.Xaml.Data.ICustomProperty __result = default;

            result = default;

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

                result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_GetStringRepresentation_2(IntPtr thisPtr, out IntPtr result)
        {
            string __result = default;

            result = default;

            try
            {
                __result = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).ToString();
                result = MarshalString.FromManaged(__result);
            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
        private static unsafe int Do_Abi_get_Type_3(IntPtr thisPtr, out global::ABI.System.Type value)
        {
            global::System.Type __value = default;

            value = default;

            try
            {
                __value = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr).GetType();
                value = global::ABI.System.Type.FromManaged(__value);

            }
            catch (Exception __exception__)
            {
                global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
            }
            return 0;
        }
    }

    internal static class ICustomPropertyProvider_Delegates
    {
        public unsafe delegate int GetCustomProperty_0(IntPtr thisPtr, IntPtr name, out IntPtr result);
        public unsafe delegate int GetIndexedProperty_1(IntPtr thisPtr, IntPtr name, global::ABI.System.Type type, out IntPtr result);
        public unsafe delegate int GetStringRepresentation_2(IntPtr thisPtr, out IntPtr result);
        public unsafe delegate int get_Type_3(IntPtr thisPtr, out global::ABI.System.Type value);
    }

}
