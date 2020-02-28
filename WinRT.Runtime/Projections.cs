using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace WinRT
{
    public static class Projections
    {
        private static readonly ReaderWriterLockSlim TypeMappingsLock = new ReaderWriterLockSlim();
        private static readonly Dictionary<Type, Type> CustomHelperTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> CustomAbiTypeNameToTypeMappings = new Dictionary<string, Type>();
        private static readonly ConcurrentBag<(Func<object, bool> condition, Func<object, (Guid IID, IntPtr Vtable)> vtableEntryFactory)> AdditionalInterfacesBag = new ConcurrentBag<(Func<object, bool>, Func<object, (Guid, IntPtr)>)>();

        static Projections()
        {
            CustomHelperTypeMappings.Add(typeof(bool), typeof(ABI.System.Boolean));
            CustomHelperTypeMappings.Add(typeof(char), typeof(ABI.System.Char));

            CustomAbiTypeNameToTypeMappings.Add("Windows.Foundation.EventRegistrationToken", typeof(EventRegistrationToken));

            CustomHelperTypeMappings.Add(typeof(Nullable<>), typeof(ABI.System.Nullable<>));
            CustomAbiTypeNameToTypeMappings.Add("Windows.Foundation.IReference`1", typeof(Nullable<>));

            CustomHelperTypeMappings.Add(typeof(DateTimeOffset), typeof(ABI.System.DateTimeOffset));
            CustomAbiTypeNameToTypeMappings.Add("Windows.Foundation.DateTime", typeof(DateTimeOffset));

            CustomHelperTypeMappings.Add(typeof(Exception), typeof(ABI.System.Exception));
            CustomAbiTypeNameToTypeMappings.Add("Windows.Foundation.HResult", typeof(Exception));

            CustomHelperTypeMappings.Add(typeof(TimeSpan), typeof(ABI.System.TimeSpan));
            CustomAbiTypeNameToTypeMappings.Add("Windows.Foundation.TimeSpan", typeof(TimeSpan));
        }

        public static void RegisterCustomAbiTypeMapping(Type publicType, Type abiType, string winrtTypeName)
        {
            TypeMappingsLock.EnterWriteLock();
            try
            {
                CustomHelperTypeMappings.Add(publicType, abiType);
                CustomAbiTypeNameToTypeMappings.Add(winrtTypeName, publicType);
            }
            finally
            {
                TypeMappingsLock.ExitWriteLock();
            }
        }

        public static Type FindCustomHelperTypeMapping(Type publicType)
        {
            TypeMappingsLock.EnterReadLock();
            try
            {
                if (publicType.IsGenericType)
                {
                    return FindCustomHelperTypeMappingGeneric(publicType);
                }
                return CustomHelperTypeMappings.TryGetValue(publicType, out Type abiType) ? abiType : null;
            }
            finally
            {
                TypeMappingsLock.ExitReadLock();
            }
        }

        private static Type FindCustomHelperTypeMappingGeneric(Type publicType)
        {
            Type genericTypeDefinition = publicType.GetGenericTypeDefinition();

            if (!CustomHelperTypeMappings.TryGetValue(genericTypeDefinition, out var abiDefinition))
            {
                return null;
            }

            return abiDefinition.MakeGenericType(publicType.GenericTypeArguments);
        }

        public static Type FindTypeForAbiTypeName(string abiTypeName)
        {
            TypeMappingsLock.EnterReadLock();
            try
            {
                return CustomAbiTypeNameToTypeMappings.TryGetValue(abiTypeName, out Type type) ? type : null;
            }
            finally
            {
                TypeMappingsLock.ExitReadLock();
            }
        }

        public static void RegisterAdditionalInterfaceFactory(Func<object, bool> condition, Func<object, (Guid IID, IntPtr VTable)> vtableEntryFactory)
        {
            AdditionalInterfacesBag.Add((condition, vtableEntryFactory));
        }

        internal static List<(Guid IID, IntPtr VTable)> GetAdditionalVtablesForObject(object o)
        {
            List<(Guid, IntPtr)> additionalVTables = new List<(Guid, IntPtr)>();

            foreach (var (condition, vtableEntryFactory) in AdditionalInterfacesBag)
            {
                if (condition(o))
                {
                    additionalVTables.Add(vtableEntryFactory(o));
                }
            }

            return additionalVTables;
        }

        internal static Type GetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass)
        {
            ProjectedRuntimeClassAttribute attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
            if (attr is null)
            {
                throw new ArgumentException($"The provided type '{runtimeClass.FullName}' is not a WinRT projected runtime class.", nameof(runtimeClass));
            }

            return runtimeClass.GetField(attr.DefaultInterfaceField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).FieldType;
        }
    }
}
