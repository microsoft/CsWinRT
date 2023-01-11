// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    static class TypeExtensions
    {
        private readonly static ConcurrentDictionary<Type, Type> HelperTypeCache = new ConcurrentDictionary<Type, Type>();

#if NET
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                            DynamicallyAccessedMemberTypes.NonPublicMethods |
                                            DynamicallyAccessedMemberTypes.PublicNestedTypes | 
                                            DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public static Type FindHelperType(this Type type)
        {
            return HelperTypeCache.GetOrAdd(type,
#if NET
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                                DynamicallyAccessedMemberTypes.NonPublicMethods |
                                                DynamicallyAccessedMemberTypes.PublicNestedTypes | 
                                                DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            (type) =>
            {
                if (typeof(Exception).IsAssignableFrom(type))
                {
                    type = typeof(Exception);
                }
                Type customMapping = Projections.FindCustomHelperTypeMapping(type);
                if (customMapping is not null)
                {
                    return customMapping;
                }

                var helperTypeAtribute = type.GetCustomAttribute<WindowsRuntimeHelperTypeAttribute>();
                if (helperTypeAtribute is not null)
                {
                    return GetHelperTypeFromAttribute(helperTypeAtribute, type);
                }

                return FindHelperTypeFallback(type);
            });

#if NET
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "No members of the generic type are dynamically accessed other than for the attributes on it.")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            static Type GetHelperTypeFromAttribute(WindowsRuntimeHelperTypeAttribute helperTypeAtribute, Type type)
            {
                if (type.IsGenericType)
                {
                    // how can we avoid this MGT 
                    
                    return helperTypeAtribute.HelperType.MakeGenericType(type.GetGenericArguments());
                }
                else
                {
                    return helperTypeAtribute.HelperType;
                }
            }

#if NET
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "This is a fallback for compat purposes with existing projections.  " +
                "Applications which make use of trimming will make use of updated projections that won't hit this code path.")]
#endif
            static Type FindHelperTypeFallback(Type type)
            {
                string fullTypeName = type.FullName;
                string ccwTypePrefix = "ABI.Impl.";
                if (fullTypeName.StartsWith(ccwTypePrefix, StringComparison.Ordinal))
                {
                    fullTypeName = fullTypeName.Substring(ccwTypePrefix.Length);
                }

                var helper = $"ABI.{fullTypeName}";
                return type.Assembly.GetType(helper) ?? Type.GetType(helper);
            }
        }

#if NET
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | 
                                            DynamicallyAccessedMemberTypes.NonPublicMethods |
                                            DynamicallyAccessedMemberTypes.PublicNestedTypes |
                                            DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public static Type GetHelperType(this Type type)
        {
            var helperType = type.FindHelperType();
            if (helperType is object)
                return helperType;
            throw new InvalidOperationException($"Target type is not a projected type: {type.FullName}.");
        }

#if NET
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public static Type GetGuidType(this Type type)
        {
            return type.IsDelegate() ? type.GetHelperType() : type;
        }

#if NET
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
        public static Type FindVftblType(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes)]
#endif
            this Type helperType)
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

        internal static IntPtr GetAbiToProjectionVftblPtr(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            this Type helperType)
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

        internal static Type GetMarshaler2Type(this Type type)
        {
            var helperType = type.GetHelperType();
            var createMarshaler = helperType.GetMethod("CreateMarshaler2", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ??
                helperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return createMarshaler.ReturnType;
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

        private readonly static ConcurrentDictionary<Type, Type> AuthoringMetadataTypeCache = new ConcurrentDictionary<Type, Type>();

        internal static Type GetAuthoringMetadataType(this Type type)
        {
            return AuthoringMetadataTypeCache.GetOrAdd(type,
#if NET
                [RequiresUnreferencedCodeAttribute("If authoring a WinRT component in C# using C#/WinRT authoring support, it might require ABI helper types that might get trimmed. Avoid marking such components trimmable or ensure types don't get trimmed from it.")]
#endif
                (type) =>
                {
                    var ccwTypeName = $"ABI.Impl.{type.FullName}";
                    return type.Assembly.GetType(ccwTypeName, false);
                });
        }
    }
}
