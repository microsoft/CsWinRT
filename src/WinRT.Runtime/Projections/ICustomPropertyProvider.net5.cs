// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

// These types have the same GUIDs in both Microsoft.UI.Xaml and Windows.UI.Xaml,
// so we don't need to duplicate them for the internal usage here as they can be transparently used by both WUX and MUX.
namespace Microsoft.UI.Xaml.Data
{
    [global::WinRT.WindowsRuntimeType]
    [global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Xaml.Data.ICustomProperty))]
    [Guid("30DA92C0-23E8-42A0-AE7C-734A0E5D2782")]
    internal interface ICustomProperty
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

    /// <summary>
    /// An interface complementing <see cref="GeneratedBindableCustomPropertyAttribute"/> providing the implementation to expose the specified properties.
    /// </summary>
#if EMBED
    internal
#else
    public
#endif
    interface IBindableCustomPropertyImplementation
    {
        /// <summary>
        /// Get the generated <see cref="Microsoft.UI.Xaml.Data.ICustomProperty"/> implementation representing the specified property name.
        /// </summary>
        /// <param name="name">The name of the property to get.</param>
        /// <returns>The <see cref="Microsoft.UI.Xaml.Data.ICustomProperty"/> implementation for the property specified by <paramref name="name"/>.</returns>
        public Microsoft.UI.Xaml.Data.BindableCustomProperty GetProperty(string name);

        /// <summary>
        /// Get the generated <see cref="Microsoft.UI.Xaml.Data.ICustomProperty"/> implementation representing the specified index property type.
        /// </summary>
        /// <param name="indexParameterType">The index property to get.</param>
        /// <returns>The <see cref="Microsoft.UI.Xaml.Data.ICustomProperty"/> implementation for the property specified by <paramref name="indexParameterType"/>.</returns>
        public Microsoft.UI.Xaml.Data.BindableCustomProperty GetProperty(Type indexParameterType);
    }

    /// <summary>
    /// An <see cref="Microsoft.UI.Xaml.Data.ICustomProperty"/> implementation that relies on a source generation approach for its implememtation
    /// rather than reflection.  This is used by the source generator generating the implementation for <see cref="Microsoft.UI.Xaml.Data.IBindableCustomPropertyImplementation"/>.
    /// </summary>
    [global::WinRT.WinRTExposedType(typeof(global::ABI.Microsoft.UI.Xaml.Data.ManagedCustomPropertyWinRTTypeDetails))]
#if EMBED
    internal
#else
    public
#endif
    sealed class BindableCustomProperty : ICustomProperty
    {
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly string _name;
        private readonly Type _type;
        private readonly Func<object, object> _getValue;
        private readonly Action<object, object> _setValue;
        private readonly Func<object, object, object> _getIndexedValue;
        private readonly Action<object, object, object> _setIndexedValue;

        public BindableCustomProperty(
            bool canRead,
            bool canWrite,
            string name,
            Type type,
            Func<object, object> getValue,
            Action<object, object> setValue,
            Func<object, object, object> getIndexedValue,
            Action<object, object, object> setIndexedValue)
        {
            _canRead = canRead;
            _canWrite = canWrite;
            _name = name;
            _type = type;
            _getValue = getValue;
            _setValue = setValue;
            _getIndexedValue = getIndexedValue;
            _setIndexedValue = setIndexedValue;
        }

        bool ICustomProperty.CanRead => _canRead;

        bool ICustomProperty.CanWrite => _canWrite;

        string ICustomProperty.Name => _name;

        Type ICustomProperty.Type => _type;

        object ICustomProperty.GetIndexedValue(object target, object index) => _getIndexedValue != null ? _getIndexedValue(target, index) : throw new NotImplementedException();

        object ICustomProperty.GetValue(object target) => _getValue != null ? _getValue(target) : throw new NotImplementedException();

        void ICustomProperty.SetIndexedValue(object target, object value, object index)
        {
            if (_setIndexedValue == null)
            {
                throw new NotImplementedException();
            }

            _setIndexedValue(target, value, index);
        }

        void ICustomProperty.SetValue(object target, object value)
        {
            if (_setValue == null)
            {
                throw new NotImplementedException();
            }

            _setValue(target, value);
        }
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

                if (target is global::Microsoft.UI.Xaml.Data.IBindableCustomPropertyImplementation bindableCustomPropertyImplementation)
                {
                    __result = bindableCustomPropertyImplementation.GetProperty(_name);
                    *result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);
                    return 0;
                }

                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    throw new NotSupportedException(
                        $"ICustomProperty support used by XAML binding for type '{target.GetType()}' (property '{name}') requires the type to marked with 'WinRT.GeneratedBindableCustomPropertyAttribute'. " +
                        $"If this is a built-in type or a type that can't be marked, a wrapper type should be used around it that is marked to enable this support.");
                }

                GetCustomPropertyForJit(target, _name, result);

                [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Fallback method for JIT environments that is not trim-safe by design.")]
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void GetCustomPropertyForJit(object target, string name, IntPtr* result)
                {
                    global::Microsoft.UI.Xaml.Data.ICustomProperty __result = default;

                    PropertyInfo propertyInfo = target.GetType().GetProperty(
                            name,
                            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);

                    if (propertyInfo is not null)
                    {
                        __result = new ManagedCustomProperty(propertyInfo);
                    }

                    *result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);
                }
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
                Type _type = global::ABI.System.Type.FromAbi(type);

                object target = global::WinRT.ComWrappersSupport.FindObject<object>(thisPtr);

                if (target is global::Microsoft.UI.Xaml.Data.IBindableCustomPropertyImplementation bindableCustomPropertyImplementation)
                {
                    __result = bindableCustomPropertyImplementation.GetProperty(_type);
                    *result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);
                    return 0;
                }

                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    throw new NotSupportedException(
                        $"ICustomProperty support used by XAML binding for type '{target.GetType()}' (indexer with parameter of type '{_type}') requires the type to marked with 'WinRT.GeneratedBindableCustomPropertyAttribute'. " +
                        $"If this is a built-in type or a type that can't be marked, a wrapper type should be used around it that is marked to enable this support.");
                }

                // Intentionally declare this here to avoid marshalling this value entirely on AOT,
                // as it's not needed. The indexer property is just matched by the parameter type.
                string _name = MarshalString.FromAbi(name);

                GetCustomPropertyForJit(target, _name, _type, result);

                [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Fallback method for JIT environments that is not trim-safe by design.")]
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void GetCustomPropertyForJit(object target, string name, Type type, IntPtr* result)
                {
                    global::Microsoft.UI.Xaml.Data.ICustomProperty __result = default;

                    PropertyInfo propertyInfo = target.GetType().GetProperty(
                        name,
                        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public,
                        null,                                                                   // default binder
                        null,                                                                   // ignore return type
                        new Type[] { type },                                                    // indexed parameter type
                        null                                                                    // ignore type modifier
                    );

                    if (propertyInfo is not null)
                    {
                        __result = new ManagedCustomProperty(propertyInfo);
                    }

                    *result = MarshalInterface<global::Microsoft.UI.Xaml.Data.ICustomProperty>.FromManaged(__result);
                }
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
