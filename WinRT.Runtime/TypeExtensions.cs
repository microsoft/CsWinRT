using System;
using System.Collections.Generic;
using System.Text;

namespace WinRT
{

    public static class TypeExtensions
    {
        public static Type FindHelperType(this Type type)
        {
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

        public static Type GetAbiType(this Type type)
        {
            return type.GetHelperType().GetMethod("GetAbi").ReturnType;
        }

        public static Type GetMarshalerType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshaler").ReturnType;
        }
        public static bool IsDelegate(this Type type)
        {
            return typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);
        }
    }
}
