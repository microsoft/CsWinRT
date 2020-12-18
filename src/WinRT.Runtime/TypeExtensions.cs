using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace WinRT
{

    public static class TypeExtensions
    {
        public static Type FindHelperType(this Type type)
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
            if (fullTypeName.StartsWith(ccwTypePrefix))
            {
                fullTypeName = fullTypeName.Substring(ccwTypePrefix.Length);
            }

            var helper = $"ABI.{fullTypeName}";
            return Type.GetType(helper) ?? type.Assembly.GetType(helper);
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

        public static Type GetRuntimeClassCCWType(this Type type)
        {
            return type.IsClass ? type.GetAuthoringMetadataType() : null;
        }

        internal static Type GetAuthoringMetadataType(this Type type)
        {
            var ccwTypeName = $"ABI.Impl.{type.FullName}";
            return Type.GetType(ccwTypeName, false) ?? type.Assembly.GetType(ccwTypeName, false);
        }

    }
}
