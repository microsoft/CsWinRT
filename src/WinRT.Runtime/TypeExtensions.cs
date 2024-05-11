// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    static class TypeExtensions
    {
        internal readonly static ConcurrentDictionary<Type, Type> HelperTypeCache = new ConcurrentDictionary<Type, Type>();

#if NET
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                            DynamicallyAccessedMemberTypes.PublicFields)]
        [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Matching trimming annotations are used at all callsites registering helper types present in the cache.")]
#endif
        public static Type FindHelperType(this Type type)
        {
#if NET
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                                DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            static Type FindHelperTypeNoCache(Type type)
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

                var authoringMetadaType = type.GetAuthoringMetadataType();
                if (authoringMetadaType is not null)
                {
                    helperTypeAtribute = authoringMetadaType.GetCustomAttribute<WindowsRuntimeHelperTypeAttribute>();
                    if (helperTypeAtribute is not null)
                    {
                        return GetHelperTypeFromAttribute(helperTypeAtribute, type);
                    }
                }
#if NET
                // Using AOT requires using updated projections, which would never let the code below
                // be reached (as it's just a fallback path for legacy projections). So we can trim it.
                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    return null;
                }
#endif
                return FindHelperTypeFallback(type);
            }

            return HelperTypeCache.GetOrAdd(type, FindHelperTypeNoCache);

#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "No members of the generic type are dynamically accessed other than for the attributes on it.")]
            [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "The type arguments are guaranteed to be valid for the generic ABI types.")]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            static Type GetHelperTypeFromAttribute(WindowsRuntimeHelperTypeAttribute helperTypeAtribute, Type type)
            {
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
#if NET
                    if (!RuntimeFeature.IsDynamicCodeCompiled)
                    {
                        throw new NotSupportedException($"Cannot retrieve the helper type from generic type '{type}'.");
                    }
#endif

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                    return helperTypeAtribute.HelperType.MakeGenericType(type.GetGenericArguments());
#pragma warning restore IL3050
                }
                else
                {
                    return helperTypeAtribute.HelperType;
                }
            }

#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2026", Justification =
                "This is a fallback for compat purposes with existing projections. " +
                "Applications which make use of trimming will make use of updated projections that won't hit this code path.")]
            [UnconditionalSuppressMessage("Trimming", "IL2057", Justification =
                "This is a fallback for compat purposes with existing projections. " +
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
        public static Type GetGuidType(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            this Type type)
        {
            return type.IsDelegate() ? type.GetHelperType() : type;
        }

#if NET
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The fallback path is not AOT-safe by design (to avoid annotations).")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The fallback path is not trim-safe by design (to avoid annotations).")]
#endif
        public static Type FindVftblType(this Type helperType)
        {
#if NET
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return null;
            }
#endif

#if NET8_0_OR_GREATER
            [RequiresDynamicCode(AttributeMessages.MarshallingOrGenericInstantiationsRequiresDynamicCode)]
#endif
#if NET
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.Windows.Foundation.IAsyncActionWithProgress`1+Vftbl", "Microsoft.Windows.SDK.NET")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.Windows.Foundation.IAsyncOperationWithProgress`2+Vftbl", "Microsoft.Windows.SDK.NET")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.Windows.Foundation.IAsyncOperation`1+Vftbl", "Microsoft.Windows.SDK.NET")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.Windows.Foundation.Collections.IMapChangedEventArgs`1+Vftbl", "Microsoft.Windows.SDK.NET")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.Windows.Foundation.Collections.IObservableMap`2+Vftbl", "Microsoft.Windows.SDK.NET")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.Windows.Foundation.Collections.IObservableVector`1+Vftbl", "Microsoft.Windows.SDK.NET")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.EventHandler`1+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.KeyValuePair`2+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.IEnumerable`1+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.IEnumerator`1+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.IList`1+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.IReadOnlyList`1+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.IDictionary`2+Vftbl", "WinRT.Runtime")]
            [DynamicDependency(DynamicallyAccessedMemberTypes.All, "ABI.System.Collections.Generic.IReadOnlyDictionary`2+Vftbl", "WinRT.Runtime")]
            [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
#endif
            static Type FindVftblTypeFallback(Type helperType)
            {
                Type vftblType = helperType.GetNestedType("Vftbl");
                if (vftblType is null)
                {
                    return null;
                }
                if (helperType.IsGenericType)
                {
                    vftblType = vftblType.MakeGenericType(helperType.GetGenericArguments());
                }
                return vftblType;
            }

            return FindVftblTypeFallback(helperType);
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The path using vtable types is a fallback and is not trim-safe by design.")]
#endif
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
            return type.GetHelperType().GetMethod("GetAbi", BindingFlags.Public | BindingFlags.Static).ReturnType;
        }

        public static Type GetMarshalerType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.Static).ReturnType;
        }

        internal static Type GetMarshaler2Type(this Type type)
        {
            var helperType = type.GetHelperType();
            var createMarshaler =
                helperType.GetMethod("CreateMarshaler2", BindingFlags.Public | BindingFlags.Static) ??
                helperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.Static);
            return createMarshaler.ReturnType;
        }

        internal static Type GetMarshalerArrayType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshalerArray", BindingFlags.Public | BindingFlags.Static)?.ReturnType;
        }

        public static bool IsDelegate(this Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        internal static bool IsNullableT(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }

        internal static bool IsAbiNullableDelegate(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ABI.System.Nullable_Delegate<>);
        }

        internal static bool IsIReferenceArray(this Type type)
        {
            // If support for 'IReference<T>' is disabled, we'll never instantiate any types implementing this interface. We
            // can guard this check behind the feature switch to avoid making 'IReferenceArray<T>' reflectable, which will
            // otherwise root some unnecessary code and metadata from the ABI implementation type.
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                return false;
            }

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Windows.Foundation.IReferenceArray<>);
        }

        internal static bool ShouldProvideIReference(this Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                return false;
            }

            return type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(Guid) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type.IsTypeOfType() ||
                type.IsTypeOfException() ||
                ((type.IsValueType || type.IsDelegate()) && Projections.IsTypeWindowsRuntimeType(type));
        }

        internal static bool IsTypeOfType(this Type type)
        {
            return typeof(Type).IsAssignableFrom(type);
        }

        internal static bool IsTypeOfException(this Type type)
        {
            return typeof(Exception).IsAssignableFrom(type);
        }

        public static Type GetRuntimeClassCCWType(this Type type)
        {
            return type.IsClass && !type.IsArray ? type.GetAuthoringMetadataType() : null;
        }

        internal static Type GetCCWType(this Type type)
        {
            return !type.IsArray ? type.GetAuthoringMetadataType() : null;
        }

        private readonly static ConcurrentDictionary<Type, Type> AuthoringMetadataTypeCache = new();
        private readonly static List<Func<Type, Type>> AuthoringMetadaTypeLookup = new();

        internal static void RegisterAuthoringMetadataTypeLookup(Func<Type, Type> authoringMetadataTypeLookup)
        {
            AuthoringMetadaTypeLookup.Add(authoringMetadataTypeLookup);
        }

        internal static Type GetAuthoringMetadataType(this Type type)
        {
            return AuthoringMetadataTypeCache.GetOrAdd(type,
                static (type) =>
                {
                    // Using for loop to avoid exception from list changing when using for each.
                    // List is only added to and if any are added while looping, we can ignore those.
                    int count = AuthoringMetadaTypeLookup.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Type metadataType = AuthoringMetadaTypeLookup[i](type);
                        if (metadataType is not null)
                        {
                            return metadataType;
                        }
                    }

#if NET
                    if (!RuntimeFeature.IsDynamicCodeCompiled)
                    {
                        return null;
                    }
#endif
                    // Fallback code path for back compat with previously generated projections
                    // running without AOT.
                    return GetAuthoringMetadataTypeFallback(type);
                });
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
            Justification = "This is a fallback for compat purposes with existing projections.  " +
            "Applications making use of updated projections won't hit this code path.")]
#endif
        private static Type GetAuthoringMetadataTypeFallback(Type type)
        {
            var ccwTypeName = $"ABI.Impl.{type.FullName}";
            return type.Assembly.GetType(ccwTypeName, false);
        }
    }
}
