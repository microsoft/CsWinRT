// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace WinRT
{
    internal sealed class TypeCache
    {
        private readonly Type type;
        private volatile bool isHelperTypeSet;
        private volatile Type helperType;
        private volatile bool isAuthoringMetadataTypeSet;
        private volatile Type authoringMetadataType;
        private volatile bool isVftblTypeSet;
        private volatile Type vftblType;
        private volatile bool isDefaultInterfaceTypeSet;
        private volatile Type defaultInterfaceType;
        private volatile bool isWindowsRuntimeTypeSet;
        private volatile bool isWindowsRuntimeType;

        public TypeCache(Type type)
        {
            this.type = type;
        }

        private static Type GetHelperType(Type type)
        {
            if (typeof(Exception).IsAssignableFrom(type))
            {
                type = typeof(Exception);
            }
            Type customMapping = Projections.FindCustomHelperTypeMapping(type);
            if (customMapping is object)
            {
                return customMapping;
            }

            string fullTypeName = type.FullName;
            string ccwTypePrefix = "ABI.Impl.";
            if (fullTypeName.StartsWith(ccwTypePrefix, StringComparison.Ordinal))
            {
                fullTypeName = fullTypeName.Substring(ccwTypePrefix.Length);
            }

            var helper = $"ABI.{fullTypeName}";
            return type.Assembly.GetType(helper) ?? Type.GetType(helper);
        }

        private Type MakeHelperType()
        {
            helperType = GetHelperType(type);
            isHelperTypeSet = true;
            return helperType;
        }

        public Type HelperType => isHelperTypeSet ? helperType : MakeHelperType();

        private static Type GetAuthoringMetadataType(Type type)
        {
            var ccwTypeName = $"ABI.Impl.{type.FullName}";
            return type.Assembly.GetType(ccwTypeName, false) ?? Type.GetType(ccwTypeName, false);
        }

        private Type MakeAuthoringMetadataType()
        {
            authoringMetadataType = GetAuthoringMetadataType(type);
            isAuthoringMetadataTypeSet = true;
            return authoringMetadataType;
        }

        public Type AuthoringMetadataType => isAuthoringMetadataTypeSet ? authoringMetadataType : MakeAuthoringMetadataType();

        private static Type GetVftblType(Type helperType)
        {
            Type vftblType = helperType.GetNestedType("Vftbl");
            if (vftblType is null)
            {
                return null;
            }
            if (helperType.IsGenericType && vftblType is object)
            {
                vftblType = vftblType.MakeGenericType(helperType.GetGenericArguments());
            }
            return vftblType;
        }

        private Type MakeVftblType()
        {
            vftblType = GetVftblType(type);
            isVftblTypeSet = true;
            return vftblType;
        }

        public Type VftblType => isVftblTypeSet ? vftblType : MakeVftblType();

        private static Type GetDefaultInterfaceType(Type runtimeClass)
        {
            runtimeClass = runtimeClass.GetRuntimeClassCCWType() ?? runtimeClass;
            ProjectedRuntimeClassAttribute attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
            if (attr is null)
            {
                return null;
            }
            else if (attr.DefaultInterfaceProperty != null)
            {
                return runtimeClass.GetProperty(attr.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).PropertyType;
            }
            else
            {
                return attr.DefaultInterface;
            }
        }

        private Type MakeDefaultInterfaceType()
        {
            defaultInterfaceType = GetDefaultInterfaceType(type);
            isDefaultInterfaceTypeSet = true;
            return defaultInterfaceType;
        }

        public Type DefaultInterfaceType => isDefaultInterfaceTypeSet ? defaultInterfaceType : MakeDefaultInterfaceType();

        private static bool GetIsWindowsRuntimeType(Type type)
        {
            Type typeToTest = type;
            if (typeToTest.IsArray)
            {
                typeToTest = typeToTest.GetElementType();
            }
            return Projections.IsTypeWindowsRuntimeTypeNoArray(typeToTest);
        }

        private bool MakeIsWindowsRuntimeType()
        {
            isWindowsRuntimeType = GetIsWindowsRuntimeType(type);
            isWindowsRuntimeTypeSet = true;
            return isWindowsRuntimeType;
        }

        public bool IsWindowsRuntimeType => isWindowsRuntimeTypeSet ? isWindowsRuntimeType : MakeIsWindowsRuntimeType();
    }

#if EMBED
    internal
#else 
    public
#endif
    static class TypeExtensions
    {
        private readonly static ConcurrentDictionary<Type, TypeCache> TypeCache = new();
        internal static TypeCache GetTypeCacheEntry(this Type type)
        {
            return TypeCache.GetOrAdd(type, (type) => new TypeCache(type));
        }

        public static Type FindHelperType(this Type type)
        {
            return type.GetTypeCacheEntry().HelperType;
        }

        public static Type GetHelperType(this Type type)
        {
            var helperType = type.FindHelperType();
            if (helperType is object)
                return helperType;
            throw new InvalidOperationException($"Target type is not a projected type: {type.FullName}.");
        }

        public static Type GetGuidType(this Type type)
        {
            return type.IsDelegate() ? type.GetHelperType() : type;
        }

        public static Type FindVftblType(this Type helperType)
        {
            return helperType.GetTypeCacheEntry().VftblType;
        }

        internal static IntPtr GetAbiToProjectionVftblPtr(this Type helperType)
        {
            return (IntPtr)(helperType.FindVftblType() ?? helperType).GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        public static Type GetAbiType(this Type type)
        {
            return type.GetHelperType().GetMethod("GetAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ReturnType;
        }

        public static Type GetMarshalerType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ReturnType;
        }

        internal static Type GetMarshalerArrayType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshalerArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.ReturnType;
        }

        public static bool IsDelegate(this Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        internal static bool IsTypeOfType(this Type type)
        {
            return typeof(Type).IsAssignableFrom(type);
        }

        public static Type GetRuntimeClassCCWType(this Type type)
        {
            return type.IsClass && !type.IsArray ? type.GetAuthoringMetadataType() : null;
        }

        internal static Type GetAuthoringMetadataType(this Type type)
        {
            return type.GetTypeCacheEntry().AuthoringMetadataType;
        }

        internal static Type GetDefaultInterfaceType(this Type type)
        {
            return type.GetTypeCacheEntry().DefaultInterfaceType;
        }
    }
}
