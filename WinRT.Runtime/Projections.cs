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
        private static ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private static Dictionary<Type, Type> CustomHelperTypeMappings = new Dictionary<Type, Type>();
        private static Dictionary<string, Type> CustomAbiTypeNameToTypeMappings = new Dictionary<string, Type>();

        static Projections()
        {
            CustomHelperTypeMappings.Add(typeof(bool), typeof(ABI.System.Boolean));
            CustomHelperTypeMappings.Add(typeof(char), typeof(ABI.System.Char));
            CustomAbiTypeNameToTypeMappings.Add("Windows.Foundation.EventRegistrationToken", typeof(EventRegistrationToken));
        }

        public static void RegisterCustomAbiTypeMapping(Type publicType, Type abiType, string winrtTypeName)
        {
            rwlock.EnterWriteLock();
            try
            {
                CustomHelperTypeMappings.Add(publicType, abiType);
                CustomAbiTypeNameToTypeMappings.Add(winrtTypeName, publicType);
            }
            finally
            {
                rwlock.ExitWriteLock();
            }
        }

        public static Type FindCustomHelperTypeMapping(Type publicType)
        {
            rwlock.EnterReadLock();
            try
            {
                return CustomHelperTypeMappings.TryGetValue(publicType, out Type abiType) ? abiType : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static Type FindTypeForAbiTypeName(string abiTypeName)
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
