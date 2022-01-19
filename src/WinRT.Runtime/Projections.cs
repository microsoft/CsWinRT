// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
            CustomTypeToHelperTypeMappings.Add(typeof(global::Microsoft.UI.Xaml.Interop.IBindableVector), typeof(ABI.System.Collections.IList));

            CustomTypeToAbiTypeNameMappings.Add(typeof(System.Type), "Windows.UI.Xaml.Interop.TypeName");
        }

        public static void RegisterCustomAbiTypeMapping(Type publicType, Type abiType, string winrtTypeName, bool isRuntimeClass = false)
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

        private static void RegisterCustomAbiTypeMappingNoLock(Type publicType, Type abiType, string winrtTypeName, bool isRuntimeClass = false)
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

        private readonly static ConcurrentDictionary<Type, bool> IsTypeWindowsRuntimeTypeCache = new ConcurrentDictionary<Type, bool>();
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
            type = type.GetAuthoringMetadataType() ?? type;
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
                || type.GetCustomAttribute<WindowsRuntimeTypeAttribute>() is object;
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

        private static HashSet<Type> GetCompatibleTypes(Type type)
        {
            HashSet<Type> compatibleTypes = new HashSet<Type>();

            foreach (var iface in type.GetInterfaces())
            {
                if (IsTypeWindowsRuntimeTypeNoArray(iface))
                {
                    compatibleTypes.Add(iface);
                }

                if (iface.IsConstructedGenericType
                    && TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, out var compatibleIfaces))
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

        internal static bool TryGetCompatibleWindowsRuntimeTypesForVariantType(Type type, out IEnumerable<Type> compatibleTypes)
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
                    return false;
                }

                if (argumentCovariantObject)
                {
                    compatibleTypesForGeneric.AddRange(GetCompatibleTypes(genericArguments[i]));
                }

                compatibleTypesPerGeneric.Add(compatibleTypesForGeneric);
            }

            compatibleTypes = GetAllPossibleTypeCombinations(compatibleTypesPerGeneric, definition);
            return true;
        }

        internal static bool TryGetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass, out Type defaultInterface)
        {
            runtimeClass = runtimeClass.GetRuntimeClassCCWType() ?? runtimeClass;
            defaultInterface = null;
            ProjectedRuntimeClassAttribute attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
            if (attr is null)
            {
                return false;
            }

            if (attr.DefaultInterfaceProperty != null)
            {
                defaultInterface = runtimeClass.GetProperty(attr.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).PropertyType;
            }
            else
            {
                defaultInterface = attr.DefaultInterface;
            }
            return true;
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
                if (objectReference.TryAs<IInspectable.Vftbl>(IInspectable.IID, out var inspectablePtr) == 0)
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
    }
}