using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Windows.Input;

namespace WinRT
{
    public static class Projections
    {
        private static readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private static readonly Dictionary<Type, Type> CustomTypeToHelperTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> CustomAbiTypeToTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> CustomAbiTypeNameToTypeMappings = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> CustomTypeToAbiTypeNameMappings = new Dictionary<Type, string>();

        static Projections()
        {
            RegisterCustomAbiTypeMappingNoLock(typeof(bool), typeof(ABI.System.Boolean), "Boolean");
            RegisterCustomAbiTypeMappingNoLock(typeof(char), typeof(ABI.System.Char), "Char");
            RegisterCustomAbiTypeMappingNoLock(typeof(EventRegistrationToken), typeof(ABI.WinRT.EventRegistrationToken), "Windows.Foundation.EventRegistrationToken");
            
            RegisterCustomAbiTypeMappingNoLock(typeof(Nullable<>), typeof(ABI.System.Nullable<>), "Windows.Foundation.IReference`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(DateTimeOffset), typeof(ABI.System.DateTimeOffset), "Windows.Foundation.DateTime");
            RegisterCustomAbiTypeMappingNoLock(typeof(Exception), typeof(ABI.System.Exception), "Windows.Foundation.HResult");
            RegisterCustomAbiTypeMappingNoLock(typeof(TimeSpan), typeof(ABI.System.TimeSpan), "Windows.Foundation.TimeSpan");
            RegisterCustomAbiTypeMappingNoLock(typeof(Uri), typeof(ABI.System.Uri), "Windows.Foundation.Uri");
            RegisterCustomAbiTypeMappingNoLock(typeof(PropertyChangedEventArgs), typeof(ABI.System.ComponentModel.PropertyChangedEventArgs), "Windows.UI.Xaml.Data.PropertyChangedEventArgs");
            RegisterCustomAbiTypeMappingNoLock(typeof(PropertyChangedEventHandler), typeof(ABI.System.ComponentModel.PropertyChangedEventHandler), "Windows.UI.Xaml.Data.PropertyChangedEventHandler");
            RegisterCustomAbiTypeMappingNoLock(typeof(INotifyPropertyChanged), typeof(ABI.System.ComponentModel.INotifyPropertyChanged), "Windows.UI.Xaml.Data.INotifyPropertyChanged");
            RegisterCustomAbiTypeMappingNoLock(typeof(ICommand), typeof(ABI.System.Windows.Input.ICommand), "Windows.UI.Xaml.Interop.ICommand", "Microsoft.UI.Xaml.Interop.ICommand");
            RegisterCustomAbiTypeMappingNoLock(typeof(EventHandler<>), typeof(ABI.System.EventHandler<>), "Windows.Foundation.EventHandler`1");

            RegisterCustomAbiTypeMappingNoLock(typeof(KeyValuePair<,>), typeof(ABI.System.Collections.Generic.KeyValuePair<,>), "Windows.Foundation.Collections.IKeyValuePair`2");
            RegisterCustomAbiTypeMappingNoLock(typeof(IEnumerable<>), typeof(ABI.System.Collections.Generic.IEnumerable<>), "Windows.Foundation.Collections.IIterable`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IEnumerator<>), typeof(ABI.System.Collections.Generic.IEnumerator<>), "Windows.Foundation.Collections.IIterator`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IList<>), typeof(ABI.System.Collections.Generic.IList<>), "Windows.Foundation.Collections.IVector`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IReadOnlyList<>), typeof(ABI.System.Collections.Generic.IReadOnlyList<>), "Windows.Foundation.Collections.IVectorView`1");
            RegisterCustomAbiTypeMappingNoLock(typeof(IDictionary<,>), typeof(ABI.System.Collections.Generic.IDictionary<,>), "Windows.Foundation.Collections.IMap`2");
            RegisterCustomAbiTypeMappingNoLock(typeof(IReadOnlyDictionary<,>), typeof(ABI.System.Collections.Generic.IReadOnlyDictionary<,>), "Windows.Foundation.Collections.IMapView`2");
            RegisterCustomAbiTypeMappingNoLock(typeof(IDisposable), typeof(ABI.System.IDisposable), "Windows.Foundation.IClosable");

            RegisterCustomAbiTypeMappingNoLock(typeof(IEnumerable), typeof(ABI.System.Collections.IEnumerable), "Windows.UI.Xaml.Interop.IBindableIterable");
            RegisterCustomAbiTypeMappingNoLock(typeof(IList), typeof(ABI.System.Collections.IList), "Windows.UI.Xaml.Interop.IBindableVector");
            RegisterCustomAbiTypeMappingNoLock(typeof(INotifyCollectionChanged), typeof(ABI.System.Collections.Specialized.INotifyCollectionChanged), "Windows.UI.Xaml.Interop.INotifyCollectionChanged");
            RegisterCustomAbiTypeMappingNoLock(typeof(NotifyCollectionChangedAction), typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedAction), "Windows.UI.Xaml.Interop.NotifyCollectionChangedAction");
            RegisterCustomAbiTypeMappingNoLock(typeof(NotifyCollectionChangedEventArgs), typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs), "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventArgs");
            RegisterCustomAbiTypeMappingNoLock(typeof(NotifyCollectionChangedEventHandler), typeof(ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler), "Windows.UI.Xaml.Interop.NotifyCollectionChangedEventHandler");

            RegisterCustomAbiTypeMappingNoLock(typeof(Matrix3x2), typeof(ABI.System.Numerics.Matrix3x2), "Windows.Foundation.Numerics.Matrix3x2");
            RegisterCustomAbiTypeMappingNoLock(typeof(Matrix4x4), typeof(ABI.System.Numerics.Matrix4x4), "Windows.Foundation.Numerics.Matrix4x4");
            RegisterCustomAbiTypeMappingNoLock(typeof(Plane), typeof(ABI.System.Numerics.Plane), "Windows.Foundation.Numerics.Plane");
            RegisterCustomAbiTypeMappingNoLock(typeof(Quaternion), typeof(ABI.System.Numerics.Quaternion), "Windows.Foundation.Numerics.Quaternion");
            RegisterCustomAbiTypeMappingNoLock(typeof(Vector2), typeof(ABI.System.Numerics.Vector2), "Windows.Foundation.Numerics.Vector2");
            RegisterCustomAbiTypeMappingNoLock(typeof(Vector3), typeof(ABI.System.Numerics.Vector3), "Windows.Foundation.Numerics.Vector3");
            RegisterCustomAbiTypeMappingNoLock(typeof(Vector4), typeof(ABI.System.Numerics.Vector4), "Windows.Foundation.Numerics.Vector4");
        }

        public static void RegisterCustomAbiTypeMapping(Type publicType, Type abiType, string winrtTypeName)
        {
            rwlock.EnterWriteLock();
            try
            {
                RegisterCustomAbiTypeMappingNoLock(publicType, abiType, winrtTypeName);
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        private static void RegisterCustomAbiTypeMappingNoLock(Type publicType, Type abiType, string winrtTypeName, params string[] additionalWinrtTypeNames)
        {
            CustomTypeToHelperTypeMappings.Add(publicType, abiType);
            CustomAbiTypeToTypeMappings.Add(abiType, publicType);
            CustomTypeToAbiTypeNameMappings.Add(publicType, winrtTypeName);
            CustomAbiTypeNameToTypeMappings.Add(winrtTypeName, publicType);
            if (additionalWinrtTypeNames is object)
            {
                foreach (var name in additionalWinrtTypeNames)
                {
                    CustomAbiTypeNameToTypeMappings.Add(name, publicType);
                }
            }
        }

        public static Type FindCustomHelperTypeMapping(Type publicType)
        {
            rwlock.EnterReadLock();
            try
            {
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

        public static bool IsTypeWindowsRuntimeType(Type type)
        {
            Type typeToTest = type;
            if (typeToTest.IsArray)
            {
                typeToTest = typeToTest.GetElementType();
            }
            if (typeToTest.IsGenericType)
            {
                typeToTest = typeToTest.GetGenericTypeDefinition();
            }
            return CustomTypeToAbiTypeNameMappings.ContainsKey(typeToTest) || typeToTest.GetCustomAttribute<WindowsRuntimeTypeAttribute>() is object;
        }

        internal static bool TryGetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass, out Type defaultInterface)
        {
            defaultInterface = null;
            ProjectedRuntimeClassAttribute attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
            if (attr is null)
            {
                return false;
            }

            defaultInterface = runtimeClass.GetProperty(attr.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).PropertyType;
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
    }
}
