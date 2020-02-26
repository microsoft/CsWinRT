﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    }
}
