// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace WinRT
{
    [Flags]
    internal enum TypeNameGenerationFlags
    {
        None = 0,

        /// <summary>
        /// Generate the name of the type as if it was boxed in an object.
        /// </summary>
        GenerateBoxedName = 0x1,

        /// <summary>
        /// Don't output a type name of a custom .NET type. Generate a compatible WinRT type name if needed.
        /// </summary>
        ForGetRuntimeClassName = 0x2,
    }

    internal static class TypeNameSupport
    {
        private static readonly List<Assembly> projectionAssemblies = new List<Assembly>();
        private static readonly List<IDictionary<string, string>> projectionTypeNameToBaseTypeNameMappings = new List<IDictionary<string, string>>();
        private static readonly ConcurrentDictionary<string, Type> typeNameCache = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal) { ["TrackerCollection<T>"] = null };
        private static readonly ConcurrentDictionary<string, Type> baseRcwTypeCache = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal) { ["TrackerCollection<T>"] = null };

        public static void RegisterProjectionAssembly(Assembly assembly)
        {
            projectionAssemblies.Add(assembly);
        }

        public static void RegisterProjectionTypeBaseTypeMapping(IDictionary<string, string> typeNameToBaseTypeNameMapping)
        {
            projectionTypeNameToBaseTypeNameMappings.Add(typeNameToBaseTypeNameMapping);
        }

        public static Type FindRcwTypeByNameCached(string runtimeClassName)
        {
            // Try to get the given type name. If it is not found, the type might have been trimmed.
            // Due to that, check if one of the base types exists and if so use that instead for the RCW type.
            var rcwType = FindTypeByNameCached(runtimeClassName);
            if (rcwType is null)
            {
                rcwType = baseRcwTypeCache.GetOrAdd(runtimeClassName,
                    static (runtimeClassName) =>
                    {
                        // Using for loop to avoid exception from list changing when using for each.
                        // List is only added to and if any are added while looping, we can ignore those.
                        int count = projectionTypeNameToBaseTypeNameMappings.Count;
                        for (int i = 0; i < count; i++)
                        {
                            if (projectionTypeNameToBaseTypeNameMappings[i].ContainsKey(runtimeClassName))
                            {
                                return FindRcwTypeByNameCached(projectionTypeNameToBaseTypeNameMappings[i][runtimeClassName]);
                            }
                        }

                        return null;
                    });
            }

            return rcwType;
        }

        /// <summary>
        /// Parses and loads the given type name, if not found in the cache.
        /// </summary>
        /// <param name="runtimeClassName">The runtime class name to attempt to parse.</param>
        /// <returns>The type, if found.  Null otherwise</returns>
        public static Type FindTypeByNameCached(string runtimeClassName)
        {
            return typeNameCache.GetOrAdd(runtimeClassName,
                static (runtimeClassName) =>
                {
                    Type implementationType = null;
                    try
                    {
                        implementationType = FindTypeByName(runtimeClassName.AsSpan()).type;
                    }
                    catch (Exception)
                    {
                    }
                    return implementationType;
                });
        }

        // Helper to get an exception if the input type is 'IReference<T>' when support for it is disabled
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception GetExceptionForUnsupportedIReferenceType(ReadOnlySpan<char> runtimeClassName)
        {
            return new NotSupportedException(
                $"The requested runtime class name is '{runtimeClassName.ToString()}', which maps to an 'IReference<T>' projected type. " +
                "This can only be used when support for 'IReference<T>' types is enabled in the CsWinRT configuration. To enable it, " +
                "make sure that the 'CsWinRTEnableIReferenceSupport' MSBuild property is not being set to 'false' anywhere.");
        }

        /// <summary>
        /// Parse the first full type name within the provided span.
        /// </summary>
        /// <param name="runtimeClassName">The runtime class name to attempt to parse.</param>
        /// <returns>A tuple containing the resolved type and the index of the end of the resolved type name.</returns>
        public static (Type type, int remaining) FindTypeByName(ReadOnlySpan<char> runtimeClassName)
        {
            // Assume that anonymous types are expando objects, whether declared 'dynamic' or not.
            // It may be necessary to detect otherwise and return System.Object.
            if (runtimeClassName.StartsWith("<>f__AnonymousType".AsSpan(), StringComparison.Ordinal))
            {
                if (FeatureSwitches.EnableDynamicObjectsSupport)
                {
                    return (typeof(System.Dynamic.ExpandoObject), 0);
                }

                throw new NotSupportedException(
                    $"The requested runtime class name is '{runtimeClassName.ToString()}', which maps to a dynamic projected type. " +
                    "This can only be used when support for dynamic objects is enabled in the CsWinRT configuration. To enable it, " +
                    "make sure that the 'CsWinRTEnableDynamicObjectsSupport' MSBuild property is not being set to 'false' anywhere.");
            }

            // PropertySet and ValueSet can return IReference<String> but Nullable<String> is illegal
            if (runtimeClassName.CompareTo("Windows.Foundation.IReference`1<String>".AsSpan(), StringComparison.Ordinal) == 0)
            {
                if (FeatureSwitches.EnableIReferenceSupport)
                {
                    return (typeof(ABI.System.Nullable_string), 0);
                }

                throw GetExceptionForUnsupportedIReferenceType(runtimeClassName);
            }
            
            if (runtimeClassName.CompareTo("Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>".AsSpan(), StringComparison.Ordinal) == 0)
            {
                if (FeatureSwitches.EnableIReferenceSupport)
                {
                    return (typeof(ABI.System.Nullable_Type), 0);
                }

                throw GetExceptionForUnsupportedIReferenceType(runtimeClassName);
            }
            
            if (runtimeClassName.CompareTo("Windows.Foundation.IReference`1<Windows.Foundation.HResult>".AsSpan(), StringComparison.Ordinal) == 0)
            {
                if (FeatureSwitches.EnableIReferenceSupport)
                {
                    return (typeof(ABI.System.Nullable_Exception), 0);
                }

                throw GetExceptionForUnsupportedIReferenceType(runtimeClassName);
            }

            var (genericTypeName, genericTypes, remaining) = ParseGenericTypeName(runtimeClassName);
            if (genericTypeName == null)
            {
                return (null, -1);
            }
            return (FindTypeByNameCore(genericTypeName, genericTypes), remaining);
        }

        /// <summary>
        /// Resolve a type from the given simple type name and the provided generic parameters.
        /// </summary>
        /// <param name="runtimeClassName">The simple type name.</param>
        /// <param name="genericTypes">The generic parameters.</param>
        /// <returns>The resolved (and instantiated if generic) type.</returns>
        /// <remarks>
        /// We look up the type dynamically because at this point in the stack we can't know
        /// the full type closure of the application.
        /// </remarks>
#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Any types which are trimmed are not used by user code and there is fallback logic to handle that.")]
#endif
        private static Type FindTypeByNameCore(string runtimeClassName, Type[] genericTypes)
        {
            Type resolvedType = Projections.FindCustomTypeForAbiTypeName(runtimeClassName);

            if (resolvedType is null)
            {
                if (genericTypes is null)
                {
                    Type primitiveType = ResolvePrimitiveType(runtimeClassName);
                    if (primitiveType is not null)
                    {
                        return primitiveType;
                    }
                }

                // Using for loop to avoid exception from list changing when using for each.
                // List is only added to and if any are added while looping, we can ignore those.
                int count = projectionAssemblies.Count;
                for (int i = 0; i < count; i++)
                {
                    Type type = projectionAssemblies[i].GetType(runtimeClassName);
                    if (type is not null)
                    {
                        resolvedType = type;
                        break;
                    }
                }
            }

            if (resolvedType is null)
            { 
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType(runtimeClassName);
                    if (type is not null)
                    {
                        resolvedType = type;
                        break;
                    }
                }
            }

            if (resolvedType is not null)
            {
                if (genericTypes != null)
                {
                    return ResolveGenericType(resolvedType, genericTypes, runtimeClassName);
                }
                return resolvedType;
            }

            Debug.WriteLine($"FindTypeByNameCore: Unable to find a type named '{runtimeClassName}'");
            return null;

#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2055", Justification = "The 'MakeGenericType' call is guarded by explicit checks in our code.")]
            [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Calls to MakeGenericType are done with reference types.")]
#endif
            static Type ResolveGenericType(Type resolvedType, Type[] genericTypes, string runtimeClassName)
            {
                if (resolvedType == typeof(global::System.Nullable<>))
                {
                    if (genericTypes[0].IsDelegate())
                    {
                        if (FeatureSwitches.EnableIReferenceSupport)
                        {
                            return typeof(ABI.System.Nullable_Delegate<>).MakeGenericType(genericTypes);
                        }

                        throw GetExceptionForUnsupportedIReferenceType(runtimeClassName.AsSpan());
                    }
                    else
                    {
                        var nullableType = ABI.System.NullableType.GetTypeAsNullableType(genericTypes[0]);
                        if (nullableType is not null)
                        {
                            return nullableType;
                        }
                    }
                }

#if NET
                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    foreach (var type in genericTypes)
                    {
                        if (type.IsValueType)
                        {
                            return null;
                        }
                    }
                }
#endif

                return resolvedType.MakeGenericType(genericTypes);
            }
        }

        public static Type ResolvePrimitiveType(string primitiveTypeName)
        {
            return primitiveTypeName switch
            {
                "UInt8" => typeof(byte),
                "Int8" => typeof(sbyte),
                "UInt16" => typeof(ushort),
                "Int16" => typeof(short),
                "UInt32" => typeof(uint),
                "Int32" => typeof(int),
                "UInt64" => typeof(ulong),
                "Int64" => typeof(long),
                "Boolean" => typeof(bool),
                "String" => typeof(string),
                "Char" => typeof(char),
                "Char16" => typeof(char),
                "Single" => typeof(float),
                "Double" => typeof(double),
                "Guid" => typeof(Guid),
                "Object" => typeof(object),
                "TimeSpan" => typeof(TimeSpan),
                _ => null
            };
        }

        /// <summary>
        /// Parses a type name from the start of a span including its generic parameters.
        /// </summary>
        /// <param name="partialTypeName">A span starting with a type name to parse.</param>
        /// <returns>Returns a tuple containing the simple type name of the type, and generic type parameters if they exist, and the index of the end of the type name in the span.</returns>
        private static (string genericTypeName, Type[] genericTypes, int remaining) ParseGenericTypeName(ReadOnlySpan<char> partialTypeName)
        {
            int possibleEndOfSimpleTypeName = partialTypeName.IndexOfAny(',', '>');
            int endOfSimpleTypeName = partialTypeName.Length;
            if (possibleEndOfSimpleTypeName != -1)
            {
                endOfSimpleTypeName = possibleEndOfSimpleTypeName;
            }
            var typeName = partialTypeName.Slice(0, endOfSimpleTypeName);

            // If the type name doesn't contain a '`', then it isn't a generic type
            // so we can return before starting to parse the generic type list.
            if (!typeName.Contains("`".AsSpan(), StringComparison.Ordinal))
            {
                return (typeName.ToString(), null, endOfSimpleTypeName);
            }

            int genericTypeListStart = partialTypeName.IndexOf('<');
            var genericTypeName = partialTypeName.Slice(0, genericTypeListStart);
            var remainingTypeName = partialTypeName.Slice(genericTypeListStart + 1);
            int remainingIndex = genericTypeListStart + 1;
            List<Type> genericTypes = new List<Type>();
            while (true)
            {
                // Resolve the generic type argument at this point in the parameter list.
                var (genericType, endOfGenericArgument) = FindTypeByName(remainingTypeName);
                if (genericType == null)
                {
                    return (null, null, -1);
                }

                remainingIndex += endOfGenericArgument;
                genericTypes.Add(genericType);
                remainingTypeName = remainingTypeName.Slice(endOfGenericArgument);
                if (remainingTypeName[0] == ',')
                {
                    // Skip the comma and the space in the type name.
                    remainingIndex += 2;
                    remainingTypeName = remainingTypeName.Slice(2);
                    continue;
                }
                else if (remainingTypeName[0] == '>')
                {
                    // Skip the space after nested '>'
                    var skip = (remainingTypeName.Length > 1 && remainingTypeName[1] == ' ') ? 2 : 1;
                    remainingIndex += skip;
                    remainingTypeName = remainingTypeName.Slice(skip);
                    break;
                }
                else
                {
                    throw new InvalidOperationException("The provided type name is invalid.");
                }
            }
            return (genericTypeName.ToString(), genericTypes.ToArray(), partialTypeName.Length - remainingTypeName.Length);
        }

        struct VisitedType
        {
            public Type Type { get; set; }
            public bool Covariant { get; set; }
        }

#nullable enable
        /// <summary>
        /// Tracker for visited types when determining a WinRT interface to use as the type name.
        /// </summary>
        /// <remarks>
        /// Only used when <see cref="GetNameForType"/> is called with <see cref="TypeNameGenerationFlags.ForGetRuntimeClassName"/>.
        /// </remarks>
        [ThreadStatic]
        private static Stack<VisitedType>? visitedTypesInstance;

        [ThreadStatic]
        private static StringBuilder? nameForTypeBuilderInstance;

        public static string GetNameForType(Type? type, TypeNameGenerationFlags flags)
        {
            if (type is null)
            {
                return string.Empty;
            }

            // Get instance for this thread
            StringBuilder? nameBuilder = nameForTypeBuilderInstance ??= new StringBuilder();
            nameBuilder.Clear();
            if (TryAppendTypeName(type, nameBuilder, flags))
            {
                return nameBuilder.ToString();
            }

            return string.Empty;
        }
#nullable restore

        private static bool TryAppendSimpleTypeName(Type type, StringBuilder builder, TypeNameGenerationFlags flags)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(TimeSpan))
            {
                if (type == typeof(byte))
                {
                    builder.Append("UInt8");
                }
                else if (type == typeof(sbyte))
                {
                    builder.Append("Int8");
                }
                else
                {
                    builder.Append(type.Name);
                }
            }
            else if (type == typeof(object))
            {
                builder.Append("Object");
            }
            else if ((flags & TypeNameGenerationFlags.ForGetRuntimeClassName) != 0 && type.IsTypeOfType())
            {
                builder.Append("Windows.UI.Xaml.Interop.TypeName");
            }
            else
            {
                var projectedAbiTypeName = Projections.FindCustomAbiTypeNameForType(type);
                if (projectedAbiTypeName is not null)
                {
                    builder.Append(projectedAbiTypeName);
                }
                else if (Projections.IsTypeWindowsRuntimeType(type))
                {
                    builder.Append(type.FullName);
                }
                else
                {
                    if ((flags & TypeNameGenerationFlags.ForGetRuntimeClassName) != 0)
                    {
                        return TryAppendWinRTInterfaceNameForType(type, builder, flags);
                    }
                    else
                    {
                        builder.Append(type.FullName);
                    }
                }
            }
            return true;
        }

        private static bool TryAppendWinRTInterfaceNameForType(Type type, StringBuilder builder, TypeNameGenerationFlags flags)
        {
            Debug.Assert((flags & TypeNameGenerationFlags.ForGetRuntimeClassName) != 0);
            Debug.Assert(!type.IsGenericTypeDefinition);

#if NET
            var runtimeClassNameAttribute = type.GetCustomAttribute<WinRTRuntimeClassNameAttribute>();
            if (runtimeClassNameAttribute is not null)
            {
                builder.Append(runtimeClassNameAttribute.RuntimeClassName);
                return true;
            }

            var runtimeClassNameFromLookupTable = ComWrappersSupport.GetRuntimeClassNameForNonWinRTTypeFromLookupTable(type);
            if (!string.IsNullOrEmpty(runtimeClassNameFromLookupTable))
            {
                builder.Append(runtimeClassNameFromLookupTable);
                return true;
            }

            // AOT source generator should have generated the attribute with the class name.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                return false;
            }
#endif


            var visitedTypes = visitedTypesInstance ??= new Stack<VisitedType>();

            // Manual helper to save binary size (no LINQ, no lambdas) and get better performance
            static bool HasAnyVisitedTypes(Stack<VisitedType> visitedTypes, Type type)
            {
                foreach (VisitedType visitedType in visitedTypes)
                {
                    if (visitedType.Type == type)
                    {
                        return true;
                    }
                }

                return false;
            }

            if (HasAnyVisitedTypes(visitedTypes, type))
            {
                // In this case, we've already visited the type when recursing through generic parameters.
                // Try to fall back to object if the parameter is covariant and the argument is compatable with object.
                // Otherwise there's no valid type name.
                if (visitedTypes.Peek().Covariant && !type.IsValueType)
                {
                    builder.Append("Object");
                    return true;
                }
                return false;
            }
            else
            {
#if NET
                [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Updated binaries will have WinRTRuntimeClassNameAttribute which will be used instead.")]
#endif
                static bool TryAppendWinRTInterfaceNameForTypeJit(Type type, StringBuilder builder, TypeNameGenerationFlags flags)
                {
                    var visitedTypes = visitedTypesInstance;

                    visitedTypes.Push(new VisitedType { Type = type });

                    Type interfaceTypeToUse = null;
                    foreach (var iface in type.GetInterfaces())
                    {
                        if (Projections.IsTypeWindowsRuntimeType(iface))
                        {
                            if (interfaceTypeToUse is null || interfaceTypeToUse.IsAssignableFrom(iface))
                            {
                                interfaceTypeToUse = iface;
                            }
                        }
                    }

                    bool success = false;

                    if (interfaceTypeToUse is not null)
                    {
                        success = TryAppendTypeName(interfaceTypeToUse, builder, flags);
                    }

                    visitedTypes.Pop();

                    return success;
                }

                return TryAppendWinRTInterfaceNameForTypeJit(type, builder, flags);
            }
        }

        private static bool TryAppendTypeName(Type type, StringBuilder builder, TypeNameGenerationFlags flags)
        {
#if !NET
            // We can't easily determine from just the type
            // if the array is an "single dimension index from zero"-array in .NET Standard 2.0,
            // so just approximate it.
            // (Other array types will be blocked in other code-paths anyway where we have an object.)
            if (type.IsArray && type.GetArrayRank() == 1)
#else
            if (type.IsSZArray)
#endif
            {
                var elementType = type.GetElementType();
                if (elementType.ShouldProvideIReference())
                {
                    builder.Append("Windows.Foundation.IReferenceArray`1<");
                    if (TryAppendTypeName(elementType, builder, flags & ~TypeNameGenerationFlags.GenerateBoxedName))
                    {
                        builder.Append('>');
                        return true;
                    }
                    return false;
                }
                else
                {
                    return false;
                }
            }

            if ((flags & TypeNameGenerationFlags.GenerateBoxedName) != 0 && type.ShouldProvideIReference())
            {
                builder.Append("Windows.Foundation.IReference`1<");
                if (!TryAppendSimpleTypeName(type, builder, flags & ~TypeNameGenerationFlags.GenerateBoxedName))
                {
                    return false;
                }
                builder.Append('>');
                return true;
            }

            if (!type.IsGenericType || type.IsGenericTypeDefinition)
            {
                return TryAppendSimpleTypeName(type, builder, flags);
            }

            if ((flags & TypeNameGenerationFlags.ForGetRuntimeClassName) != 0 && !Projections.IsTypeWindowsRuntimeType(type))
            {
                return TryAppendWinRTInterfaceNameForType(type, builder, flags);
            }

            Type definition = type.GetGenericTypeDefinition();
            if (!TryAppendSimpleTypeName(definition, builder, flags))
            {
                return false;
            }

            builder.Append('<');

            bool first = true;

            Type[] genericTypeArguments = type.GetGenericArguments();
            Type[] genericTypeParameters = definition.GetGenericArguments();

            var visitedTypes = visitedTypesInstance ??= new Stack<VisitedType>();

            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                Type argument = genericTypeArguments[i];

                if (argument.ContainsGenericParameters)
                {
                    throw new ArgumentException(nameof(type));
                }

                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;

                if ((flags & TypeNameGenerationFlags.ForGetRuntimeClassName) != 0)
                {
                    visitedTypes.Push(new VisitedType
                    {
                        Type = type,
                        Covariant = (genericTypeParameters[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) == GenericParameterAttributes.Covariant
                    });
                }

                bool success = TryAppendTypeName(argument, builder, flags & ~TypeNameGenerationFlags.GenerateBoxedName);

                if ((flags & TypeNameGenerationFlags.ForGetRuntimeClassName) != 0)
                {
                    visitedTypes.Pop();
                }

                if (!success)
                {
                    return false;
                }
            }

            builder.Append('>');
            return true;
        }
    }
}
