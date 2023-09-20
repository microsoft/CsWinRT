// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Windows.Input;
using Windows.Foundation.Collections;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    static class Projections
    {
        private static readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        private static readonly Dictionary<Type, Type> CustomTypeToHelperTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> CustomAbiTypeToTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> CustomAbiTypeNameToTypeMappings = new Dictionary<string, Type>(StringComparer.Ordinal);
        private static readonly Dictionary<Type, string> CustomTypeToAbiTypeNameMappings = new Dictionary<Type, string>();
        private static readonly HashSet<string> ProjectedRuntimeClassNames = new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<Type> ProjectedCustomTypeRuntimeClasses = new HashSet<Type>();

        static Projections()
        {
            // This should be in sync with cswinrt/helpers.h and the reverse mapping from WinRT.SourceGenerator/WinRTTypeWriter.cs.
            RegisterCustomAbiTypeMappingNoLock(typeof(bool), typeof(ABI.System.Boolean), "Boolean");
            RegisterCustomAbiTypeMappingNoLock(typeof(char), typeof(ABI.System.Char), "Char");
            RegisterCustomAbiTypeMappingNoLock(typeof(EventRegistrationToken), typeof(ABI.WinRT.EventRegistrationToken), "Windows.Foundation.EventRegistrationToken");
            
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<>), typeof(ABI.System.Nullable<>), "Windows.Foundation.IReference`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<int>), typeof(ABI.System.Nullable_int), "Windows.Foundation.IReference`1<Int32>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<byte>), typeof(ABI.System.Nullable_byte), "Windows.Foundation.IReference`1<UInt8>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<sbyte>), typeof(ABI.System.Nullable_sbyte), "Windows.Foundation.IReference`1<Int8>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<short>), typeof(ABI.System.Nullable_short), "Windows.Foundation.IReference`1<Int16>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<ushort>), typeof(ABI.System.Nullable_ushort), "Windows.Foundation.IReference`1<UInt16>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<uint>), typeof(ABI.System.Nullable_uint), "Windows.Foundation.IReference`1<UInt32>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<long>), typeof(ABI.System.Nullable_long), "Windows.Foundation.IReference`1<Int64>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<ulong>), typeof(ABI.System.Nullable_ulong), "Windows.Foundation.IReference`1<UInt64>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<float>), typeof(ABI.System.Nullable_float), "Windows.Foundation.IReference`1<Single>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<double>), typeof(ABI.System.Nullable_double), "Windows.Foundation.IReference`1<Double>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<char>), typeof(ABI.System.Nullable_char), "Windows.Foundation.IReference`1<Char16>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<bool>), typeof(ABI.System.Nullable_bool), "Windows.Foundation.IReference`1<Boolean>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<Guid>), typeof(ABI.System.Nullable_guid), "Windows.Foundation.IReference`1<Guid>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<DateTimeOffset>), typeof(ABI.System.Nullable_DateTimeOffset), "Windows.Foundation.IReference`1<Windows.Foundation.DateTime>");
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<TimeSpan>), typeof(ABI.System.Nullable_TimeSpan), "Windows.Foundation.IReference`1<TimeSpan>");

            RegisterCustomAbiTypeMappingNoLock(typeof(DateTimeOffset), typeof(ABI.System.DateTimeOffset), "Windows.Foundation.DateTime");
            RegisterCustomAbiTypeMappingNoLock(typeof(Exception), typeof(ABI.System.Exception), "Windows.Foundation.HResult");
            RegisterCustomAbiTypeMappingNoLock(typeof(TimeSpan), typeof(ABI.System.TimeSpan), "Windows.Foundation.TimeSpan");
            RegisterCustomAbiTypeMappingNoLock(typeof(Uri), typeof(ABI.System.Uri), "Windows.Foundation.Uri", isRuntimeClass: true);
            RegisterCustomAbiTypeMappingNoLock(typeof(DataErrorsChangedEventArgs), typeof(ABI.System.ComponentModel.DataErrorsChangedEventArgs), "Microsoft.UI.Xaml.Data.DataErrorsChangedEventArgs", isRuntimeClass: true);
            RegisterCustomAbiTypeMappingNoLock(typeof(PropertyChangedEventArgs), typeof(ABI.System.ComponentModel.PropertyChangedEventArgs), "Microsoft.UI.Xaml.Data.PropertyChangedEventArgs", isRuntimeClass: true);
            RegisterCustomAbiTypeMappingNoLock(typeof(PropertyChangedEventHandler), typeof(ABI.System.ComponentModel.PropertyChangedEventHandler), "Microsoft.UI.Xaml.Data.PropertyChangedEventHandler");
            RegisterCustomAbiTypeMappingNoLock(typeof(INotifyDataErrorInfo), typeof(ABI.System.ComponentModel.INotifyDataErrorInfo), "Microsoft.UI.Xaml.Data.INotifyDataErrorInfo");    
            RegisterCustomAbiTypeMappingNoLock(typeof(INotifyPropertyChanged), typeof(ABI.System.ComponentModel.INotifyPropertyChanged), "Microsoft.UI.Xaml.Data.INotifyPropertyChanged");
            RegisterCustomAbiTypeMappingNoLock(typeof(ICommand), typeof(ABI.System.Windows.Input.ICommand), "Microsoft.UI.Xaml.Interop.ICommand");
            RegisterCustomAbiTypeMappingNoLock(typeof(IServiceProvider), typeof(ABI.System.IServiceProvider), "Microsoft.UI.Xaml.IXamlServiceProvider");
            RegisterCustomAbiTypeMappingNoLock(typeof(EventHandler<>), typeof(ABI.System.EventHandler<>), "Windows.Foundation.EventHandler`1");

            RegisterCustomAbiTypeMappingNoLock(typeof(KeyValuePair<,>), typeof(ABI.System.Collections.Generic.KeyValuePair<,>), "Windows.Foundation.Collections.IKeyValuePair`2");
            RegisterCustomAbiTypeMappingNoLock(typeof(IEnumerable<>), typeof(ABI.System.Collections.Generic.IEnumerable<>), "Windows.Foundation.Collections.IIterable`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IEnumerator<>), typeof(ABI.System.Collections.Generic.IEnumerator<>), "Windows.Foundation.Collections.IIterator`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IList<>), typeof(ABI.System.Collections.Generic.IList<>), "Windows.Foundation.Collections.IVector`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IReadOnlyList<>), typeof(ABI.System.Collections.Generic.IReadOnlyList<>), "Windows.Foundation.Collections.IVectorView`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IDictionary<,>), typeof(ABI.System.Collections.Generic.IDictionary<,>), "Windows.Foundation.Collections.IMap`2");
            RegisterCustomAbiTypeMappingNoLock(typeof(IReadOnlyDictionary<,>), typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>), "Windows.Foundation.Collections.IMapView`2");
            RegisterCustomAbiTypeMappingNoLock(typeof(IDisposable), typeof(ABI.System.IDisposable), "Windows.Foundation.IClosable");

            RegisterCustomAbiTypeMappingNoLock(typeof(IEnumerable), typeof(ABI.System.Collections.IEnumerable), "Microsoft.UI.Xaml.Interop.IBindableIterable");
            RegisterCustomAbiTypeMappingNoLock(typeof(IList), typeof(ABI.System.Collections.IList), "Microsoft.UI.Xaml.Interop.IBindableVector");
            RegisterCustomAbiTypeMappingNoLock(typeof(INotifyCollectionChanged), typeof(ABI.System.Collections.Specialized.INotifyCollectionChanged), "Microsoft.UI.Xaml.Interop.INotifyCollectionChanged");
            RegisterCustomAbiTypeMappingNoLock(typeof(NotifyCollectionChangedAction), typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedAction), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedAction");
            RegisterCustomAbiTypeMappingNoLock(typeof(NotifyCollectionChangedEventArgs), typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventArgs", isRuntimeClass: true);
            RegisterCustomAbiTypeMappingNoLock(typeof(NotifyCollectionChangedEventHandler), typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler), "Microsoft.UI.Xaml.Interop.NotifyCollectionChangedEventHandler");

            RegisterCustomAbiTypeMappingNoLock(typeof(Matrix3x2), typeof(ABI.System.Numerics.Matrix3x2), "Windows.Foundation.Numerics.Matrix3x2");
            RegisterCustomAbiTypeMappingNoLock(typeof(Matrix4x4), typeof(ABI.System.Numerics.Matrix4x4), "Windows.Foundation.Numerics.Matrix4x4");
            RegisterCustomAbiTypeMappingNoLock(typeof(Plane), typeof(ABI.System.Numerics.Plane), "Windows.Foundation.Numerics.Plane");
            RegisterCustomAbiTypeMappingNoLock(typeof(Quaternion), typeof(ABI.System.Numerics.Quaternion), "Windows.Foundation.Numerics.Quaternion");
            RegisterCustomAbiTypeMappingNoLock(typeof(Vector2), typeof(ABI.System.Numerics.Vector2), "Windows.Foundation.Numerics.Vector2");
            RegisterCustomAbiTypeMappingNoLock(typeof(Vector3), typeof(ABI.System.Numerics.Vector3), "Windows.Foundation.Numerics.Vector3");
            RegisterCustomAbiTypeMappingNoLock(typeof(Vector4), typeof(ABI.System.Numerics.Vector4), "Windows.Foundation.Numerics.Vector4");

            // TODO: Ideally we should not need these
            CustomTypeToHelperTypeMappings.Add(typeof(IMap<,>), typeof(ABI.System.Collections.Generic.IDictionary<,>));
            CustomTypeToHelperTypeMappings.Add(typeof(IVector<>), typeof(ABI.System.Collections.Generic.IList<>));
            CustomTypeToHelperTypeMappings.Add(typeof(IMapView<,>), typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>));
            CustomTypeToHelperTypeMappings.Add(typeof(IVectorView<>), typeof(ABI.System.Collections.Generic.IReadOnlyList<>));
            CustomTypeToHelperTypeMappings.Add(typeof(Microsoft.UI.Xaml.Interop.IBindableVector), typeof(ABI.System.Collections.IList));

#if NET
            CustomTypeToHelperTypeMappings.Add(typeof(ICollection<>), typeof(ABI.System.Collections.Generic.ICollection<>));
            CustomTypeToHelperTypeMappings.Add(typeof(IReadOnlyCollection<>), typeof(ABI.System.Collections.Generic.IReadOnlyCollection<>));
            CustomTypeToHelperTypeMappings.Add(typeof(ICollection), typeof(ABI.System.Collections.ICollection));
#endif
            RegisterCustomAbiTypeMappingNoLock(typeof(EventHandler), typeof(ABI.System.EventHandler));

            CustomTypeToAbiTypeNameMappings.Add(typeof(System.Type), "Windows.UI.Xaml.Interop.TypeName");
        }

        public static void RegisterCustomAbiTypeMapping(
            Type publicType,
#if NET
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods |
                DynamicallyAccessedMemberTypes.PublicNestedTypes |
                DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type abiType, 
            string winrtTypeName, 
            bool isRuntimeClass = false)
        {
            rwlock.EnterWriteLock();
            try
            {
                RegisterCustomAbiTypeMappingNoLock(publicType, abiType, winrtTypeName, isRuntimeClass);
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        private static void RegisterCustomAbiTypeMappingNoLock(
            Type publicType,
#if NET
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods |
                DynamicallyAccessedMemberTypes.PublicNestedTypes |
                DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type abiType, 
            string winrtTypeName,
            bool isRuntimeClass = false)
        {
            CustomTypeToHelperTypeMappings.Add(publicType, abiType);
            CustomAbiTypeToTypeMappings.Add(abiType, publicType);
            CustomTypeToAbiTypeNameMappings.Add(publicType, winrtTypeName);
            CustomAbiTypeNameToTypeMappings.Add(winrtTypeName, publicType);
            if (isRuntimeClass)
            {
                ProjectedRuntimeClassNames.Add(winrtTypeName);
                ProjectedCustomTypeRuntimeClasses.Add(publicType);
            }
        }

        private static void RegisterCustomAbiTypeMappingNoLock(
            Type publicType,
#if NET
            [DynamicallyAccessedMembers(
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods |
                DynamicallyAccessedMemberTypes.PublicNestedTypes)]
#endif
            Type abiType)
        {
            CustomTypeToHelperTypeMappings.Add(publicType, abiType);
            CustomAbiTypeToTypeMappings.Add(abiType, publicType);
        }

#if NET
        [return: DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods |
            DynamicallyAccessedMemberTypes.PublicNestedTypes |
            DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public static Type FindCustomHelperTypeMapping(Type publicType, bool filterToRuntimeClass = false)
        {
            rwlock.EnterReadLock();
            try
            {
                if(filterToRuntimeClass && !ProjectedCustomTypeRuntimeClasses.Contains(publicType))
                {
                    return null;
                }

                if (publicType.IsGenericType)
                {
                    if (CustomTypeToHelperTypeMappings.TryGetValue(publicType, out Type specializedAbiType))
                    {
                        return specializedAbiType;
                    }

                    return CustomTypeToHelperTypeMappings.TryGetValue(publicType.GetGenericTypeDefinition(), out Type abiTypeDefinition)
                        ? abiTypeDefinition.MakeGenericType(publicType.GetGenericArguments())
                        : null;
                }
                return CustomTypeToHelperTypeMappings.TryGetValue(publicType, out Type abiType) ? abiType : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static Type FindCustomPublicTypeForAbiType(Type abiType)
        {
            rwlock.EnterReadLock();
            try
            {
                if (abiType.IsGenericType)
                {
                    if (CustomAbiTypeToTypeMappings.TryGetValue(abiType, out Type specializedPublicType))
                    {
                        return specializedPublicType;
                    }

                    return CustomAbiTypeToTypeMappings.TryGetValue(abiType.GetGenericTypeDefinition(), out Type publicTypeDefinition)
                        ? publicTypeDefinition.MakeGenericType(abiType.GetGenericArguments())
                        : null;
                }
                return CustomAbiTypeToTypeMappings.TryGetValue(abiType, out Type publicType) ? publicType : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static Type FindCustomTypeForAbiTypeName(string abiTypeName)
        {
            rwlock.EnterReadLock();
            try
            {
                return CustomAbiTypeNameToTypeMappings.TryGetValue(abiTypeName, out Type type) ? type : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static string FindCustomAbiTypeNameForType(Type type)
        {
            rwlock.EnterReadLock();
            try
            {
                return CustomTypeToAbiTypeNameMappings.TryGetValue(type, out string typeName) ? typeName : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        private readonly static ConcurrentDictionary<Type, bool> IsTypeWindowsRuntimeTypeCache = new();
        public static bool IsTypeWindowsRuntimeType(Type type)
        {
            return IsTypeWindowsRuntimeTypeCache.GetOrAdd(type, (type) =>
            {
                Type typeToTest = type;
                if (typeToTest.IsArray)
                {
                    typeToTest = typeToTest.GetElementType();
                }
                return IsTypeWindowsRuntimeTypeNoArray(typeToTest);
            });
        }

        private static bool IsTypeWindowsRuntimeTypeNoArray(Type type)
        {
            if (type.IsConstructedGenericType)
            {
                if(IsTypeWindowsRuntimeTypeNoArray(type.GetGenericTypeDefinition()))
                {
                    foreach (var arg in type.GetGenericArguments())
                    {
                        if (!IsTypeWindowsRuntimeTypeNoArray(arg))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            return CustomTypeToAbiTypeNameMappings.ContainsKey(type)
                || type.IsPrimitive
                || type == typeof(string)
                || type == typeof(Guid)
                || type == typeof(object)
                || type.IsDefined(typeof(WindowsRuntimeTypeAttribute))
                || type.GetAuthoringMetadataType() != null;
        }

        // Use TryGetCompatibleWindowsRuntimeTypesForVariantType instead.
        public static bool TryGetCompatibleWindowsRuntimeTypeForVariantType(Type type, out Type compatibleType)
        {
            compatibleType = null;
            if (!type.IsConstructedGenericType)
            {
                throw new ArgumentException(nameof(type));
            }

            var definition = type.GetGenericTypeDefinition();

            if (!IsTypeWindowsRuntimeTypeNoArray(definition))
            {
                return false;
            }

            var genericConstraints = definition.GetGenericArguments();
            var genericArguments = type.GetGenericArguments();
            var newArguments = new Type[genericArguments.Length];
            for (int i = 0; i < genericArguments.Length; i++)
            {
                if (!IsTypeWindowsRuntimeTypeNoArray(genericArguments[i]))
                {
                    bool argumentCovariant = (genericConstraints[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) == GenericParameterAttributes.Covariant;
                    if (argumentCovariant && !genericArguments[i].IsValueType)
                    {
                        newArguments[i] = typeof(object);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    newArguments[i] = genericArguments[i];
                }
            }
            compatibleType = definition.MakeGenericType(newArguments);
            return true;
        }

        private static HashSet<Type> GetCompatibleTypes(Type type, Stack<Type> typeStack)
        {
            HashSet<Type> compatibleTypes = new HashSet<Type>();

            foreach (var iface in type.GetInterfaces())
            {
                if (IsTypeWindowsRuntimeTypeNoArray(iface))
                {
                    compatibleTypes.Add(iface);
                }

                if (iface.IsConstructedGenericType
                    && TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, typeStack, out var compatibleIfaces))
                {
                    compatibleTypes.UnionWith(compatibleIfaces);
                }
            }

            Type baseType = type.BaseType;
            while (baseType != null)
            {
                if (IsTypeWindowsRuntimeTypeNoArray(baseType))
                {
                    compatibleTypes.Add(baseType);
                }
                baseType = baseType.BaseType;
            }

            return compatibleTypes;
        }

        internal static IEnumerable<Type> GetAllPossibleTypeCombinations(IEnumerable<IEnumerable<Type>> compatibleTypesPerGeneric, Type definition)
        {
            // Implementation adapted from https://stackoverflow.com/a/4424005
            var accum = new List<Type>();
            var compatibleTypesPerGenericArray = compatibleTypesPerGeneric.ToArray();
            if (compatibleTypesPerGenericArray.Length > 0)
            {
                GetAllPossibleTypeCombinationsCore(
                    accum,
                    new Stack<Type>(),
                    compatibleTypesPerGenericArray,
                    compatibleTypesPerGenericArray.Length - 1);
            }
            return accum;

#if NET
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "No members of the generic type are dynamically accessed other than for the attributes on it.")]
#endif
            void GetAllPossibleTypeCombinationsCore(List<Type> accum, Stack<Type> stack, IEnumerable<Type>[] compatibleTypes, int index)
            {
                foreach (var type in compatibleTypes[index])
                {
                    stack.Push(type);
                    if (index == 0)
                    {
                        // IEnumerable on a System.Collections.Generic.Stack
                        // enumerates in order of removal (last to first).
                        // As a result, we get the correct ordering here.
                        accum.Add(definition.MakeGenericType(stack.ToArray()));
                    }
                    else
                    {
                        GetAllPossibleTypeCombinationsCore(accum, stack, compatibleTypes, index - 1);
                    }
                    stack.Pop();
                }
            }
        }

        internal static bool TryGetCompatibleWindowsRuntimeTypesForVariantType(Type type, Stack<Type> typeStack, out IEnumerable<Type> compatibleTypes)
        {
            compatibleTypes = null;
            if (!type.IsConstructedGenericType)
            {
                throw new ArgumentException(nameof(type));
            }

            var definition = type.GetGenericTypeDefinition();

            if (!IsTypeWindowsRuntimeTypeNoArray(definition))
            {
                return false;
            }

            if (typeStack == null)
            {
                typeStack = new Stack<Type>();
            }
            else
            {
                if (typeStack.Contains(type))
                {
                    return false;
                }
            }
            typeStack.Push(type);

            var genericConstraints = definition.GetGenericArguments();
            var genericArguments = type.GetGenericArguments();
            List<List<Type>> compatibleTypesPerGeneric = new List<List<Type>>();
            for (int i = 0; i < genericArguments.Length; i++)
            {
                List<Type> compatibleTypesForGeneric = new List<Type>();
                bool argumentCovariantObject = (genericConstraints[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) == GenericParameterAttributes.Covariant
                    && !genericArguments[i].IsValueType;

                if (IsTypeWindowsRuntimeTypeNoArray(genericArguments[i]))
                {
                    compatibleTypesForGeneric.Add(genericArguments[i]);
                }
                else if (!argumentCovariantObject)
                {
                    typeStack.Pop();
                    return false;
                }

                if (argumentCovariantObject)
                {
                    compatibleTypesForGeneric.AddRange(GetCompatibleTypes(genericArguments[i], typeStack));
                }

                compatibleTypesPerGeneric.Add(compatibleTypesForGeneric);
            }

            typeStack.Pop();
            compatibleTypes = GetAllPossibleTypeCombinations(compatibleTypesPerGeneric, definition);
            return true;
        }

        private readonly static ConcurrentDictionary<Type, Type> DefaultInterfaceTypeCache = new();

#if NET
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070",
            Justification = "This is a fallback for compat purposes with existing projections.  " +
            "Applications which make use of trimming will make use of updated projections that won't hit this code path.")]
#endif
        internal static bool TryGetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass, out Type defaultInterface)
        {
            defaultInterface = DefaultInterfaceTypeCache.GetOrAdd(runtimeClass, (runtimeClass) =>
            {
                runtimeClass = runtimeClass.GetRuntimeClassCCWType() ?? runtimeClass;
                ProjectedRuntimeClassAttribute attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
                if (attr is null)
                {
                    return null;
                }

                if (attr.DefaultInterface != null)
                {
                    return attr.DefaultInterface;
                }
                else if (attr.DefaultInterfaceProperty != null)
                {
                    return runtimeClass.GetProperty(attr.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).PropertyType;
                }
                else
                {
                    return null;
                }
            });
            return defaultInterface != null;
        }

        internal static Type GetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass)
        {
            if (!TryGetDefaultInterfaceTypeForRuntimeClassType(runtimeClass, out Type defaultInterface))
            {
                throw new ArgumentException($"The provided type '{runtimeClass.FullName}' is not a WinRT projected runtime class.", nameof(runtimeClass));
            }
            return defaultInterface;
        }

        internal static bool TryGetMarshalerTypeForProjectedRuntimeClass<T>(IObjectReference objectReference, out Type type)
        {
            Type projectedType = typeof(T);
            if (projectedType == typeof(object))
            {
                if (objectReference.TryAs<IInspectable.Vftbl>(InterfaceIIDs.IInspectable_IID, out var inspectablePtr) == 0)
                {
                    rwlock.EnterReadLock();
                    try
                    {
                        IInspectable inspectable = inspectablePtr;
                        string runtimeClassName = inspectable.GetRuntimeClassName(true);
                        if (runtimeClassName is object)
                        {
                            if (ProjectedRuntimeClassNames.Contains(runtimeClassName))
                            {
                                type = CustomTypeToHelperTypeMappings[CustomAbiTypeNameToTypeMappings[runtimeClassName]];
                                return true;
                            }
                        }
                    }
                    finally
                    {
                        inspectablePtr.Dispose();
                        rwlock.ExitReadLock();
                    }
                }
            }
            else
            {
                type = FindCustomHelperTypeMapping(projectedType, true);
                return type != null;
            }
            type = null;
            return false;
        }

#if NET
        internal static Type GetAbiDelegateType(params Type[] typeArgs) => Expression.GetDelegateType(typeArgs);
#else
        private class DelegateTypeComparer : IEqualityComparer<Type[]>
        {
            public bool Equals(Type[] x, Type[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(Type[] obj)
            {
                int hashCode = 0;
                for (int idx = 0; idx < obj.Length; idx++)
                {
                    hashCode ^= obj[idx].GetHashCode();
                }
                return hashCode;
            }
        }

        private static readonly ConcurrentDictionary<Type[], Type> abiDelegateCache = new(new DelegateTypeComparer())
        {
            // IEnumerable
            [new Type[] { typeof(void*), typeof(IntPtr).MakeByRefType(), typeof(int) }] = typeof(Interop._get_Current_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type).MakeByRefType(), typeof(int) }] = typeof(Interop._get_Current_Type),
            // IList / IReadOnlyList
            [new Type[] { typeof(void*), typeof(uint), typeof(IntPtr).MakeByRefType(), typeof(int) }] = typeof(Interop._get_At_IntPtr),
            [new Type[] { typeof(void*), typeof(uint), typeof(ABI.System.Type).MakeByRefType(), typeof(int) }] = typeof(Interop._get_At_Type),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._index_Of_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(uint).MakeByRefType(), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._index_Of_Type),
            [new Type[] { typeof(void*), typeof(uint), typeof(IntPtr), typeof(int) }] = typeof(Interop._set_At_IntPtr),
            [new Type[] { typeof(void*), typeof(uint), typeof(ABI.System.Type), typeof(int) }] = typeof(Interop._set_At_Type),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(int) }] = typeof(Interop._append_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(int) }] = typeof(Interop._append_Type),
            // IDictionary / IReadOnlyDictionary
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(IntPtr).MakeByRefType(), typeof(int) }] = typeof(Interop._lookup_IntPtr_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(ABI.System.Type).MakeByRefType(), typeof(int) }] = typeof(Interop._lookup_Type_Type),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(ABI.System.Type).MakeByRefType(), typeof(int) }] = typeof(Interop._lookup_IntPtr_Type),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(IntPtr).MakeByRefType(), typeof(int) }] = typeof(Interop._lookup_Type_IntPtr),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._has_key_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._has_key_Type),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(IntPtr), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._insert_IntPtr_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(ABI.System.Type), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._insert_Type_Type),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(ABI.System.Type), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._insert_IntPtr_Type),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(IntPtr), typeof(byte).MakeByRefType(), typeof(int) }] = typeof(Interop._insert_Type_IntPtr),
            // EventHandler
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(IntPtr), typeof(int) }] = typeof(Interop._invoke_IntPtr_IntPtr),
            [new Type[] { typeof(void*), typeof(IntPtr), typeof(ABI.System.Type), typeof(int) }] = typeof(Interop._invoke_IntPtr_Type),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(IntPtr), typeof(int) }] = typeof(Interop._invoke_Type_IntPtr),
            [new Type[] { typeof(void*), typeof(ABI.System.Type), typeof(ABI.System.Type), typeof(int) }] = typeof(Interop._invoke_Type_Type),
        };

        public static void RegisterAbiDelegate(Type[] delegateSignature, Type delegateType)
        {
            abiDelegateCache.TryAdd(delegateSignature, delegateType);
        }

        // The .NET Standard projection can be used in both .NET Core and .NET Framework scenarios.
        // With the latter, using Expression.GetDelegateType to create custom delegates with void* parameters
        // doesn't seem to be supported.  So we handle that by pregenerating all the ABI delegates that we need
        // based on the WinMD and also by allowing apps to register their own if there are any
        // that we couldn't detect (i.e. types passed as object in WinMD).
        public static Type GetAbiDelegateType(params Type[] typeArgs)
        {
            if (abiDelegateCache.TryGetValue(typeArgs, out var delegateType))
            {
                return delegateType;
            }

            return Expression.GetDelegateType(typeArgs);
        }
#endif
    }
}