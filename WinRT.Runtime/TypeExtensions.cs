using System;
using System.Collections.Generic;
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

            return type.Assembly.GetType($"ABI.{type.FullName}");
        }

        public static Type GetHelperType(this Type type)
        {
            return type.FindHelperType() ?? throw new InvalidOperationException("Target type is not a projected type.");
        }

        public static Type FindVftblType(this Type helperType)
        {
            Type vftblType = helperType.GetNestedType("Vftbl");
            if (helperType.IsGenericType && vftblType is object)
            {
                vftblType = vftblType.MakeGenericType(helperType.GetGenericArguments());
            }
            return vftblType;
        }

        public static Type GetAbiType(this Type type)
        {
            return type.GetHelperType().GetMethod("GetAbi", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).ReturnType;
        }

        public static Type GetMarshalerType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshaler", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).ReturnType;
        }
        public static bool IsDelegate(this Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }
    }
}
