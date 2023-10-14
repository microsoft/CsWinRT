// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using WinRT.SourceGenerator;

namespace Generator
{
    internal static class GenericVtableInitializerStrings
    {
        public static string GetInstantiation(string genericInterface, EquatableArray<GenericParameter> genericParameters)
        {
            if (genericInterface == "System.Collections.Generic.IEnumerable`1")
            {
                return GetIEnumerableInstantiation(genericParameters[0].ProjectedType, genericParameters[0].AbiType);
            }
            else if (genericInterface == "System.Collections.Generic.IList`1")
            {
                return GetIListInstantiation(genericParameters[0].ProjectedType, genericParameters[0].AbiType, genericParameters[0].TypeKind);
            }
            else if (genericInterface == "System.Collections.Generic.IReadOnlyList`1")
            {
                return GetIReadOnlyListInstantiation(genericParameters[0].ProjectedType, genericParameters[0].AbiType, genericParameters[0].TypeKind);
            }
            else if (genericInterface == "System.Collections.Generic.IDictionary`2")
            {
                return GetIDictionaryInstantiation(
                    genericParameters[0].ProjectedType,
                    genericParameters[0].AbiType,
                    genericParameters[0].TypeKind,
                    genericParameters[1].ProjectedType,
                    genericParameters[1].AbiType,
                    genericParameters[1].TypeKind);
            }
            else if (genericInterface == "System.Collections.Generic.IReadOnlyDictionary`2")
            {
                return GetIReadOnlyDictionaryInstantiation(
                    genericParameters[0].ProjectedType,
                    genericParameters[0].AbiType,
                    genericParameters[0].TypeKind,
                    genericParameters[1].ProjectedType,
                    genericParameters[1].AbiType,
                    genericParameters[1].TypeKind);
            }
            else if (genericInterface == "System.Collections.Generic.IEnumerator`1")
            {
                return GetIEnumeratorInstantiation(genericParameters[0].ProjectedType, genericParameters[0].AbiType, genericParameters[0].TypeKind);
            }
            else if (genericInterface == "System.Collections.Generic.KeyValuePair`2")
            {
                return GetKeyValuePairInstantiation(
                    genericParameters[0].ProjectedType,
                    genericParameters[0].AbiType,
                    genericParameters[0].TypeKind,
                    genericParameters[1].ProjectedType,
                    genericParameters[1].AbiType,
                    genericParameters[1].TypeKind);
            }
            else if (genericInterface == "System.EventHandler`1")
            {
                return GetEventHandlerInstantiation(genericParameters[0].ProjectedType, genericParameters[0].AbiType, genericParameters[0].TypeKind);
            }
            else if (genericInterface == "Windows.Foundation.AsyncActionWithProgressCompletedHandler`1")
            {
                return GetCompletedHandlerInstantiation("Windows.Foundation.AsyncActionWithProgressCompletedHandler", "Windows.Foundation.IAsyncActionWithProgress", genericParameters);
            }
            else if (genericInterface == "Windows.Foundation.AsyncOperationCompletedHandler`1")
            {
                return GetCompletedHandlerInstantiation("Windows.Foundation.AsyncOperationCompletedHandler", "Windows.Foundation.IAsyncOperation", genericParameters);
            }
            else if (genericInterface == "Windows.Foundation.AsyncOperationWithProgressCompletedHandler`2")
            {
                return GetCompletedHandlerInstantiation("Windows.Foundation.AsyncOperationWithProgressCompletedHandler", "Windows.Foundation.IAsyncOperationWithProgress", genericParameters);
            }
            else if (genericInterface == "Windows.Foundation.Collections.MapChangedEventHandler`2")
            {
                string senderInterface = $"global::Windows.Foundation.Collections.IObservableMap<{genericParameters[0].ProjectedType}, {genericParameters[1].ProjectedType}>";
                string changedEventArgsInterface = $"global::Windows.Foundation.Collections.IMapChangedEventArgs<{genericParameters[0].ProjectedType}>";
                return GetChangedHandlerInstantiation("Windows.Foundation.Collections.MapChangedEventHandler", senderInterface, changedEventArgsInterface, genericParameters);
            }
            else if (genericInterface == "Windows.Foundation.Collections.VectorChangedEventHandler`1")
            {
                string senderInterface = $"global::Windows.Foundation.Collections.IObservableVector<{genericParameters[0].ProjectedType}>";
                string changedEventArgsInterface = $"global::Windows.Foundation.Collections.IVectorChangedEventArgs";
                return GetChangedHandlerInstantiation("Windows.Foundation.Collections.VectorChangedEventHandler", senderInterface, changedEventArgsInterface, genericParameters);
            }
            else if (genericInterface == "Windows.Foundation.AsyncActionProgressHandler`1")
            {
                return GetProgressHandlerInstantiation("Windows.Foundation.AsyncActionProgressHandler", "Windows.Foundation.IAsyncActionWithProgress", genericParameters);
            }
            else if (genericInterface == "Windows.Foundation.AsyncOperationProgressHandler`2")
            {
                return GetProgressHandlerInstantiation("Windows.Foundation.AsyncOperationProgressHandler", "Windows.Foundation.IAsyncOperationWithProgress", genericParameters);
            }

            return "";
        }

        public static string GetInstantiationInitFunction(string genericInterface, EquatableArray<GenericParameter> genericParameters)
        {
            // Get the class name from a string like System.Collections.Generic.IEnumerator`1.
            // Splitting on the dots and the generic specifier (`) will get us the class name
            // in the 2nd last element.
            var interfaceName = genericInterface.Split('.', '`')[^2];
            var genericParametersStr = string.Join("_", genericParameters.Select(genericParameter => GeneratorHelper.EscapeTypeNameForIdentifier(genericParameter.ProjectedType)));
            return $$"""        _ = global::WinRT.GenericHelpers.{{interfaceName}}_{{genericParametersStr}}.Initialized;""";
        }

        private static string GetIEnumerableInstantiation(string genericType, string abiType)
        {
            string iEnumerableInstantiation = $$"""
             internal static class IEnumerable_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.IEnumerableMethods<{{genericType}}, {{abiType}}>.InitCcw(
                        &Do_Abi_First_0
                     );
                 }
                 
                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_First_0(IntPtr thisPtr, IntPtr* __return_value__)
                 {
                     *__return_value__ = default;
                     try
                     {
                         *__return_value__ = MarshalInterface<global::System.Collections.Generic.IEnumerator<{{genericType}}>>.
                            FromManaged(global::ABI.System.Collections.Generic.IEnumerableMethods<{{genericType}}>.Abi_First_0(thisPtr));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return iEnumerableInstantiation;
        }

        private static string GetIEnumeratorInstantiation(string genericType, string abiType, TypeKind typeKind)
        {
            string iEnumeratorInstantiation = $$"""
             internal static class IEnumerator_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.IEnumeratorMethods<{{genericType}}, {{abiType}}>.InitCcw(
                        &Do_Abi_get_Current_0,
                        &Do_Abi_get_HasCurrent_1,
                        &Do_Abi_MoveNext_2,
                        &Do_Abi_GetMany_3
                     );
                 }
                 
                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_MoveNext_2(IntPtr thisPtr, byte* __return_value__)
                 {
                     bool ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IEnumeratorMethods<{{genericType}}>.Abi_MoveNext_2(thisPtr);
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, int __itemsSize, IntPtr items, uint* __return_value__)
                 {
                     uint ____return_value__ = default;

                     *__return_value__ = default;
                     {{genericType}}[] __items = {{GeneratorHelper.GetMarshalerClass(genericType, abiType, typeKind, true)}}.FromAbiArray((__itemsSize, items));
             
                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IEnumeratorMethods<{{genericType}}>.Abi_GetMany_3(thisPtr, ref __items);
                         {{GeneratorHelper.GetCopyManagedArrayMarshaler(genericType, abiType, typeKind)}}.CopyManagedArray(__items, items);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Current_0(IntPtr thisPtr, {{abiType}}* __return_value__)
                 {
                     {{genericType}} ____return_value__ = default;
                     
                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IEnumeratorMethods<{{genericType}}>.Abi_get_Current_0(thisPtr);
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericType, abiType, typeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_HasCurrent_1(IntPtr thisPtr, byte* __return_value__)
                 {
                     bool ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IEnumeratorMethods<{{genericType}}>.Abi_get_HasCurrent_1(thisPtr);
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return iEnumeratorInstantiation;
        }

        private static string GetIListInstantiation(string genericType, string abiType, TypeKind typeKind)
        {
            string iListInstantiation = $$"""
             internal static class IList_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.IListMethods<{{genericType}}, {{abiType}}>.InitCcw(
                        &Do_Abi_GetAt_0,
                        &Do_Abi_get_Size_1,
                        &Do_Abi_GetView_2,
                        &Do_Abi_IndexOf_3,
                        &Do_Abi_SetAt_4,
                        &Do_Abi_InsertAt_5,
                        &Do_Abi_RemoveAt_6,
                        &Do_Abi_Append_7,
                        &Do_Abi_RemoveAtEnd_8,
                        &Do_Abi_Clear_9,
                        &Do_Abi_GetMany_10,
                        &Do_Abi_ReplaceAll_11
                     );
                 }
                 
                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, {{abiType}}* __return_value__)
                 {
                     {{genericType}} ____return_value__ = default;
                     *__return_value__ = default;
                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_GetAt_0(thisPtr, index);
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericType, abiType, typeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetView_2(IntPtr thisPtr, IntPtr* __return_value__)
                 {
                     global::System.Collections.Generic.IReadOnlyList<{{genericType}}> ____return_value__ = default;
                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_GetView_2(thisPtr);
                         *__return_value__ = MarshalInterface<global::System.Collections.Generic.IReadOnlyList<{{genericType}}>>.FromManaged(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_IndexOf_3(IntPtr thisPtr, {{abiType}} value, uint* index, byte* __return_value__)
                 {
                     bool ____return_value__ = default;
                     
                     *index = default;
                     *__return_value__ = default;
                     uint __index = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_IndexOf_3(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericType, abiType, typeKind)}}(value), out __index);
                         *index = __index;
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_SetAt_4(IntPtr thisPtr, uint index, {{abiType}} value)
                 {
                     try
                     {
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_SetAt_4(thisPtr, index, {{GeneratorHelper.GetFromAbiMarshaler(genericType, abiType, typeKind)}}(value));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_InsertAt_5(IntPtr thisPtr, uint index, {{abiType}} value)
                 {
                     try
                     {
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_InsertAt_5(thisPtr, index, {{GeneratorHelper.GetFromAbiMarshaler(genericType, abiType, typeKind)}}(value));
                     }
                     catch (global::System.Exception __exception__)
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
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_RemoveAt_6(thisPtr, index);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Append_7(IntPtr thisPtr, {{abiType}} value)
                 {
                     try
                     {
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_Append_7(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericType, abiType, typeKind)}}(value));
                     }
                     catch (global::System.Exception __exception__)
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
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_RemoveAtEnd_8(thisPtr);
                     }
                     catch (global::System.Exception __exception__)
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
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_Clear_9(thisPtr);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetMany_10(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, uint* __return_value__)
                 {
                     uint ____return_value__ = default;
                      
                     *__return_value__ = default;
                     {{genericType}}[] __items = {{GeneratorHelper.GetMarshalerClass(genericType, abiType, typeKind, true)}}.FromAbiArray((__itemsSize, items));

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_GetMany_10(thisPtr, startIndex, ref __items);
                         {{GeneratorHelper.GetCopyManagedArrayMarshaler(genericType, abiType, typeKind)}}.CopyManagedArray(__items, items);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_ReplaceAll_11(IntPtr thisPtr, int __itemsSize, IntPtr items)
                 {
                     try
                     {
                         global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_ReplaceAll_11(thisPtr, {{GeneratorHelper.GetMarshalerClass(genericType, abiType, typeKind, true)}}.FromAbiArray((__itemsSize, items)));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
                 {
                     uint ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IListMethods<{{genericType}}>.Abi_get_Size_1(thisPtr);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return iListInstantiation;
        }

        private static string GetIReadOnlyListInstantiation(string genericType, string abiType, TypeKind typeKind)
        {
            string iReadOnlylistInstantiation = $$"""
             internal static class IReadOnlyList_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.IReadOnlyListMethods<{{genericType}}, {{abiType}}>.InitCcw(
                        &Do_Abi_GetAt_0,
                        &Do_Abi_get_Size_1,
                        &Do_Abi_IndexOf_2,
                        &Do_Abi_GetMany_3
                     );
                 }
                 
                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetAt_0(IntPtr thisPtr, uint index, {{abiType}}* __return_value__)
                 {
                     {{genericType}} ____return_value__ = default;
                     *__return_value__ = default;
                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyListMethods<{{genericType}}>.Abi_GetAt_0(thisPtr, index);
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericType, abiType, typeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_IndexOf_2(IntPtr thisPtr, {{abiType}} value, uint* index, byte* __return_value__)
                 {
                     bool ____return_value__ = default;
                     
                     *index = default;
                     *__return_value__ = default;
                     uint __index = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyListMethods<{{genericType}}>.Abi_IndexOf_2(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericType, abiType, typeKind)}}(value), out __index);
                         *index = __index;
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetMany_3(IntPtr thisPtr, uint startIndex, int __itemsSize, IntPtr items, uint* __return_value__)
                 {
                     uint ____return_value__ = default;
                      
                     *__return_value__ = default;
                     {{genericType}}[] __items = {{GeneratorHelper.GetMarshalerClass(genericType, abiType, typeKind, true)}}.FromAbiArray((__itemsSize, items));

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyListMethods<{{genericType}}>.Abi_GetMany_3(thisPtr, startIndex, ref __items);
                         {{GeneratorHelper.GetCopyManagedArrayMarshaler(genericType, abiType, typeKind)}}.CopyManagedArray(__items, items);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
                 {
                     uint ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyListMethods<{{genericType}}>.Abi_get_Size_1(thisPtr);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return iReadOnlylistInstantiation;
        }

        private static string GetIDictionaryInstantiation(string genericKeyType, string abiKeyType, TypeKind keyTypeKind, string genericValueType, string abiValueType, TypeKind valueTypeKind)
        {
            string iDictionaryInstantiation = $$"""
             internal static class IDictionary_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericKeyType)}}_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericValueType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{abiKeyType}}, {{genericValueType}}, {{abiValueType}}>.InitCcw(
                        &Do_Abi_Lookup_0,
                        &Do_Abi_get_Size_1,
                        &Do_Abi_HasKey_2,
                        &Do_Abi_GetView_3,
                        &Do_Abi_Insert_4,
                        &Do_Abi_Remove_5,
                        &Do_Abi_Clear_6
                     );
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Lookup_0(IntPtr thisPtr, {{abiKeyType}} key, {{abiValueType}}* __return_value__)
                 {
                     {{genericValueType}} ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_Lookup_0(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(key));
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericValueType, abiValueType, valueTypeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_HasKey_2(IntPtr thisPtr, {{abiKeyType}} key, byte* __return_value__)
                 {
                     bool ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_HasKey_2(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(key));
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_GetView_3(IntPtr thisPtr, IntPtr* __return_value__)
                 {
                     global::System.Collections.Generic.IReadOnlyDictionary<{{genericKeyType}}, {{genericValueType}}> ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_GetView_3(thisPtr);
                         *__return_value__ = MarshalInterface<global::System.Collections.Generic.IReadOnlyDictionary<{{genericKeyType}}, {{genericValueType}}>>.FromManaged(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Insert_4(IntPtr thisPtr, {{abiKeyType}} key, {{abiValueType}} value, byte* __return_value__)
                 {
                     bool ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_Insert_4(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(key), {{GeneratorHelper.GetFromAbiMarshaler(genericValueType, abiValueType, valueTypeKind)}}(value));
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Remove_5(IntPtr thisPtr, {{abiKeyType}} key)
                 {
                     try
                     {
                         global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_Remove_5(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(key));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Clear_6(IntPtr thisPtr)
                 {
                     try
                     {
                         global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_Clear_6(thisPtr);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
                 {
                     uint ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_get_Size_1(thisPtr);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return iDictionaryInstantiation;
        }

        private static string GetIReadOnlyDictionaryInstantiation(string genericKeyType, string abiKeyType, TypeKind keyTypeKind, string genericValueType, string abiValueType, TypeKind valueTypeKind)
        {
            string iReadOnlyDictionaryInstantiation = $$"""
             internal static class IReadOnlyDictionary_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericKeyType)}}_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericValueType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<{{genericKeyType}}, {{abiKeyType}}, {{genericValueType}}, {{abiValueType}}>.InitCcw(
                        &Do_Abi_Lookup_0,
                        &Do_Abi_get_Size_1,
                        &Do_Abi_HasKey_2,
                        &Do_Abi_Split_3
                     );
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Lookup_0(IntPtr thisPtr, {{abiKeyType}} key, {{abiValueType}}* __return_value__)
                 {
                     {{genericValueType}} ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_Lookup_0(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(key));
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericValueType, abiValueType, valueTypeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_HasKey_2(IntPtr thisPtr, {{abiKeyType}} key, byte* __return_value__)
                 {
                     bool ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_HasKey_2(thisPtr, {{GeneratorHelper.GetFromAbiMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(key));
                         *__return_value__ = (byte)(____return_value__ ? 1 : 0);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Split_3(IntPtr thisPtr, IntPtr* first, IntPtr* second)
                 {
                     *first = default;
                     *second = default;
                     IntPtr __first = default;
                     IntPtr __second = default;

                     try
                     {
                         global::ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_Split_3(thisPtr, out __first, out __second);
                         *first = __first;
                         *second = __second;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Size_1(IntPtr thisPtr, uint* __return_value__)
                 {
                     uint ____return_value__ = default;

                     *__return_value__ = default;

                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.IReadOnlyDictionaryMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_get_Size_1(thisPtr);
                         *__return_value__ = ____return_value__;
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return iReadOnlyDictionaryInstantiation;
        }

        private static string GetKeyValuePairInstantiation(string genericKeyType, string abiKeyType, TypeKind keyTypeKind, string genericValueType, string abiValueType, TypeKind valueTypeKind)
        {
            string keyValuePairInstantiation = $$"""
             internal static class KeyValuePair_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericKeyType)}}_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericValueType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return global::ABI.System.Collections.Generic.KeyValuePairMethods<{{genericKeyType}}, {{abiKeyType}}, {{genericValueType}}, {{abiValueType}}>.InitCcw(
                        &Do_Abi_get_Key_0,
                        &Do_Abi_get_Value_1
                     );
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Key_0(IntPtr thisPtr, {{abiKeyType}}* __return_value__)
                 {
                     {{genericKeyType}} ____return_value__ = default;
                     *__return_value__ = default;
                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.KeyValuePairMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_get_Key_0(thisPtr);
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericKeyType, abiKeyType, keyTypeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_get_Value_1(IntPtr thisPtr, {{abiValueType}}* __return_value__)
                 {
                     {{genericValueType}} ____return_value__ = default;
                     *__return_value__ = default;
                     try
                     {
                         ____return_value__ = global::ABI.System.Collections.Generic.KeyValuePairMethods<{{genericKeyType}}, {{genericValueType}}>.Abi_get_Value_1(thisPtr);
                         *__return_value__ = {{GeneratorHelper.GetFromManagedMarshaler(genericValueType, abiValueType, valueTypeKind)}}(____return_value__);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return keyValuePairInstantiation;
        }

        // Delegates are special and initialize both the RCW and CCW.
        private static string GetEventHandlerInstantiation(string genericType, string abiType, TypeKind typeKind)
        {
            string eventHandlerInstantiation = $$"""
             internal static class EventHandler_{{GeneratorHelper.EscapeTypeNameForIdentifier(genericType)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     _ = global::ABI.System.EventHandlerMethods<{{genericType}}, {{abiType}}>.InitCcw(
                        &Do_Abi_Invoke
                     );
                     _ = global::ABI.System.EventHandlerMethods<{{genericType}}, {{abiType}}>.InitRcwHelper(
                        &Invoke
                     );
                     return true;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, {{abiType}} args)
                 {
                     try
                     {
                         global::ABI.System.EventHandlerMethods<{{genericType}}, {{abiType}}>.Abi_Invoke(thisPtr, MarshalInspectable<object>.FromAbi(sender), {{GeneratorHelper.GetFromAbiMarshaler(genericType, abiType, typeKind)}}(args));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 private static unsafe void Invoke(IObjectReference objRef, object sender, {{genericType}} args)
                 {
                     IntPtr ThisPtr = objRef.ThisPtr;
                     ObjectReferenceValue __sender = default;
                     {{GeneratorHelper.GetMarshalerDeclaration(genericType, abiType, typeKind, "args")}}
                     try
                     {
                         __sender = MarshalInspectable<object>.CreateMarshaler2(sender);
                         IntPtr abiSender = MarshalInspectable<object>.GetAbi(__sender);
                         {{GeneratorHelper.GetCreateMarshaler(genericType, abiType, typeKind, "args")}}
                         global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, {{abiType}}, int>**)ThisPtr)[3](ThisPtr, abiSender, {{GeneratorHelper.GetAbiFromMarshaler(genericType, abiType, typeKind, "args")}}));
                     }
                     finally
                     {
                         MarshalInspectable<object>.DisposeMarshaler(__sender);
                         {{GeneratorHelper.GetDisposeMarshaler(genericType, abiType, typeKind, "args")}}
                     }
                 }
             }
             """;
            return eventHandlerInstantiation;
        }

        private static string GetCompletedHandlerInstantiation(string completedHandler, string asyncInfoInterface, EquatableArray<GenericParameter> genericParameters)
        {
            string staticMethodsClass = $"global::ABI.{completedHandler}Methods<{GetGenericParametersAsString(genericParameters, ", ", true)}>";
            string interfaceWithGeneric = $"global::{asyncInfoInterface}<{GetGenericParametersAsString(genericParameters, ", ", false)}>";
            string completedHandlerInstantiation = $$"""
             internal static class {{completedHandler.Split('.')[^1]}}_{{GetGenericParametersAsString(genericParameters, "_", false)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return {{staticMethodsClass}}.InitCcw(
                        &Do_Abi_Invoke
                     );
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, global::Windows.Foundation.AsyncStatus asyncStatus)
                 {
                     try
                     {
                         {{staticMethodsClass}}.Abi_Invoke(thisPtr, MarshalInterface<{{interfaceWithGeneric}}>.FromAbi(asyncInfo), asyncStatus);
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return completedHandlerInstantiation;
        }

        private static string GetChangedHandlerInstantiation(string changedHandler, string senderInterface, string changedEventArgsInterface, EquatableArray<GenericParameter> genericParameters)
        {
            string staticMethodsClass = $"global::ABI.{changedHandler}Methods<{GetGenericParametersAsString(genericParameters, ", ", true)}>";
            string changedHandlerInstantiation = $$"""
             internal static class {{changedHandler.Split('.')[^1]}}_{{GetGenericParametersAsString(genericParameters, "_", false)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     return {{staticMethodsClass}}.InitCcw(
                        &Do_Abi_Invoke
                     );
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr sender, IntPtr @event)
                 {
                     try
                     {
                         {{staticMethodsClass}}.Abi_Invoke(thisPtr, MarshalInterface<{{senderInterface}}>.FromAbi(sender), MarshalInterface<{{changedEventArgsInterface}}>.FromAbi(@event));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }
             }
             """;
            return changedHandlerInstantiation;
        }

        private static string GetProgressHandlerInstantiation(string progressHandler, string asyncInfoInterface, EquatableArray<GenericParameter> genericParameters)
        {
            string staticMethodsClass = $"global::ABI.{progressHandler}Methods<{GetGenericParametersAsString(genericParameters, ", ", true)}>";
            string asyncInfoInterfaceWithGeneric = $"global::{asyncInfoInterface}<{GetGenericParametersAsString(genericParameters, ", ", false)}>";
            var progressParameter = genericParameters.Last();
            string progressHandlerInstantiation = $$"""
             internal static class {{progressHandler.Split('.')[^1]}}_{{GetGenericParametersAsString(genericParameters, "_", false)}}
             {
                 private static bool _initialized = Init();
                 internal static bool Initialized => _initialized;

                 private static unsafe bool Init()
                 {
                     _ = {{staticMethodsClass}}.InitCcw(
                        &Do_Abi_Invoke
                     );
                    _ = {{staticMethodsClass}}.InitRcwHelper(
                        &Invoke
                     );
                     return true;
                 }

                 [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
                 private static unsafe int Do_Abi_Invoke(IntPtr thisPtr, IntPtr asyncInfo, {{progressParameter.AbiType}} progressInfo)
                 {
                     try
                     {
                         {{staticMethodsClass}}.Abi_Invoke(thisPtr, MarshalInterface<{{asyncInfoInterfaceWithGeneric}}>.FromAbi(asyncInfo), {{GeneratorHelper.GetFromAbiMarshaler(progressParameter.ProjectedType, progressParameter.AbiType, progressParameter.TypeKind)}}(progressInfo));
                     }
                     catch (global::System.Exception __exception__)
                     {
                         global::WinRT.ExceptionHelpers.SetErrorInfo(__exception__);
                         return global::WinRT.ExceptionHelpers.GetHRForException(__exception__);
                     }
                     return 0;
                 }

                 private static unsafe void Invoke(IObjectReference objRef, {{asyncInfoInterfaceWithGeneric}} asyncInfo, {{progressParameter.ProjectedType}} progressInfo)
                 {
                     IntPtr ThisPtr = objRef.ThisPtr;
                     ObjectReferenceValue __asyncInfo = default;
                     {{GeneratorHelper.GetMarshalerDeclaration(progressParameter.ProjectedType, progressParameter.AbiType, progressParameter.TypeKind, "progressInfo")}}
                     try
                     {
                         __asyncInfo = MarshalInterface<{{asyncInfoInterfaceWithGeneric}}>.CreateMarshaler2(asyncInfo, GuidGenerator.GetIID(typeof({{asyncInfoInterfaceWithGeneric}}).GetHelperType()));
                         IntPtr abiAsyncInfo = MarshalInspectable<object>.GetAbi(__asyncInfo);
                         {{GeneratorHelper.GetCreateMarshaler(progressParameter.ProjectedType, progressParameter.AbiType, progressParameter.TypeKind, "progressInfo")}}
                         global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, {{progressParameter.AbiType}}, int>**)ThisPtr)[3](ThisPtr, abiAsyncInfo, {{GeneratorHelper.GetAbiFromMarshaler(progressParameter.ProjectedType, progressParameter.AbiType, progressParameter.TypeKind, "progressInfo")}}));
                     }
                     finally
                     {
                         MarshalInterface<{{asyncInfoInterfaceWithGeneric}}>.DisposeMarshaler(__asyncInfo);
                         {{GeneratorHelper.GetDisposeMarshaler(progressParameter.ProjectedType, progressParameter.AbiType, progressParameter.TypeKind, "progressInfo")}}
                     }
                 }
             }
             """;
            return progressHandlerInstantiation;
        }

        private static string GetGenericParametersAsString(EquatableArray<GenericParameter> genericParameters, string separator, bool includeAbiTypes)
        {
            string genericParametersStr = string.Join(separator, 
                genericParameters.Select(genericParameter =>
                {
                    if (includeAbiTypes)
                    {
                        return string.Join(
                            separator, 
                            GeneratorHelper.EscapeTypeNameForIdentifier(genericParameter.ProjectedType), 
                            GeneratorHelper.EscapeTypeNameForIdentifier(genericParameter.AbiType));
                    }
                    else
                    {
                        return GeneratorHelper.EscapeTypeNameForIdentifier(genericParameter.ProjectedType);
                    }
                }));
            return genericParametersStr;
        }
    }
}
