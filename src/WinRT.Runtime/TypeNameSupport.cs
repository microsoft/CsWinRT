// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace WinRT
{
    [Flags]
#if EMBED
    internal
#endif
    enum TypeNameGenerationFlags
    {
        None = 0,
        /// <summary>
        /// Generate the name of the type as if it was boxed in an object.
        /// </summary>
        GenerateBoxedName = 0x1,
        /// <summary>
        /// Don't output a type name of a custom .NET type. Generate a compatible WinRT type name if needed.
        /// </summary>
        NoCustomTypeName = 0x2
    }

#if EMBED
    internal
#endif
    static class TypeNameSupport
    {
        private static readonly List<Assembly> projectionAssemblies = new List<Assembly>();
        private static readonly ConcurrentDictionary<string, Type> typeNameCache = new ConcurrentDictionary<string, Type>(StringComparer.Ordinal) { ["TrackerCollection<T>"] = null };

        public static void RegisterProjectionAssembly(Assembly assembly)
        {
            projectionAssemblies.Add(assembly);
        }

        /// <summary>
        /// Parses and loads the given type name, if not found in the cache.
        /// </summary>
        /// <param name="runtimeClassName">The runtime class name to attempt to parse.</param>
        /// <returns>The type, if found.  Null otherwise</returns>
        public static Type FindTypeByNameCached(string runtimeClassName)
        {
            return typeNameCache.GetOrAdd(runtimeClassName, (runtimeClassName) =>
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
                return (typeof(System.Dynamic.ExpandoObject), 0);
            }
            // PropertySet and ValueSet can return IReference<String> but Nullable<String> is illegal
            else if (runtimeClassName.CompareTo("Windows.Foundation.IReference`1<String>".AsSpan(), StringComparison.Ordinal) == 0)
            {
                return (typeof(ABI.System.Nullable_string), 0);
            }
            else if (runtimeClassName.CompareTo("Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>".AsSpan(), StringComparison.Ordinal) == 0)
            {
                return (typeof(ABI.System.Nullable_Type), 0);
            }
            else
            {
                var (genericTypeName, genericTypes, remaining) = ParseGenericTypeName(runtimeClassName);
                return (FindTypeByNameCore(genericTypeName, genericTypes), remaining);
            }
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
        private static Type FindTypeByNameCore(string runtimeClassName, Type[] genericTypes)
        {
            Type resolvedType = Projections.FindCustomTypeForAbiTypeName(runtimeClassName);

            if (resolvedType is null)
            {
                if (genericTypes is null)
                {
                    Type primitiveType = ResolvePrimitiveType(runtimeClassName);
                    if (primitiveType is object)
                    {
                        return primitiveType;
                    }
                }

                foreach (var assembly in projectionAssemblies)
                {
                    Type type = assembly.GetType(runtimeClassName);
                    if (type is object)
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
                    if (type is object)
                    {
                        resolvedType = type;
                        break;
                    }
                }
            }

            if (resolvedType is object)
            {
                if (genericTypes != null)
                {
                    if(resolvedType == typeof(global::System.Nullable<>) && genericTypes[0].IsDelegate())
                    {
                        return typeof(ABI.System.Nullable_Delegate<>).MakeGenericType(genericTypes);
                    }
                    resolvedType = resolvedType.MakeGenericType(genericTypes);
                }
                return resolvedType;
            }

            throw new TypeLoadException($"Unable to find a type named '{runtimeClassName}'");
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

        /// <summary>
        /// Tracker for visited types when determining a WinRT interface to use as the type name.
        /// Only used when GetNameForType is called with <see cref="TypeNameGenerationFlags.NoCustomTypeName"/>.
        /// </summary>
        private static readonly ThreadLocal<Stack<VisitedType>> VisitedTypes = new ThreadLocal<Stack<VisitedType>>(() => new Stack<VisitedType>());

        public static string GetNameForType(Type type, TypeNameGenerationFlags flags)
        {
            if (type is null)
            {
                return string.Empty;
            }
            StringBuilder nameBuilder = new StringBuilder();
            if (TryAppendTypeName(type, nameBuilder, flags))
            {
                return nameBuilder.ToString();
            }
            return null;
        }

        private static bool TryAppendSimpleTypeName(Type type, StringBuilder builder, TypeNameGenerationFlags flags)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type == typeof(TimeSpan))
            {
                if ((flags & TypeNameGenerationFlags.GenerateBoxedName) != 0)
                {
                    builder.Append("Windows.Foundation.IReference`1<");
                    if (!TryAppendSimpleTypeName(type, builder, flags & ~TypeNameGenerationFlags.GenerateBoxedName))
                    {
                        return false;
                    }
                    builder.Append('>');
                    return true;
                }
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
            else
            {
                var projectedAbiTypeName = Projections.FindCustomAbiTypeNameForType(type);
                if (projectedAbiTypeName is object)
                {
                    builder.Append(projectedAbiTypeName);
                }
                else if (Projections.IsTypeWindowsRuntimeType(type))
                {
                    builder.Append(type.FullName);
                }
                else if ((flags & TypeNameGenerationFlags.NoCustomTypeName) != 0)
                {
                    return TryAppendWinRTInterfaceNameForType(type, builder, flags);
                }
                else
                {
                    builder.Append(type.FullName);
                }
            }
            return true;
        }

        private static bool TryAppendWinRTInterfaceNameForType(Type type, StringBuilder builder, TypeNameGenerationFlags flags)
        {
            Debug.Assert((flags & TypeNameGenerationFlags.NoCustomTypeName) != 0);
            Debug.Assert(!type.IsGenericTypeDefinition);

            var visitedTypes = VisitedTypes.Value;

            if (visitedTypes.Any(visited => visited.Type == type))
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

                if (interfaceTypeToUse is object)
                {
                    success = TryAppendTypeName(interfaceTypeToUse, builder, flags); 
                }

                visitedTypes.Pop();

                return success;
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
                builder.Append("Windows.Foundation.IReferenceArray`1<");
                if (TryAppendTypeName(type.GetElementType(), builder, flags & ~TypeNameGenerationFlags.GenerateBoxedName))
                {
                    builder.Append('>');
                    return true;
                }
                return true;
            }

            if (!type.IsGenericType || type.IsGenericTypeDefinition)
            {
                return TryAppendSimpleTypeName(type, builder, flags);
            }

            if ((flags & TypeNameGenerationFlags.NoCustomTypeName) != 0 && !Projections.IsTypeWindowsRuntimeType(type))
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

                if ((flags & TypeNameGenerationFlags.NoCustomTypeName) != 0)
                {
                    VisitedTypes.Value.Push(new VisitedType
                    {
                        Type = type,
                        Covariant = (genericTypeParameters[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) == GenericParameterAttributes.Covariant
                    });
                }

                bool success = TryAppendTypeName(argument, builder, flags & ~TypeNameGenerationFlags.GenerateBoxedName);

                if ((flags & TypeNameGenerationFlags.NoCustomTypeName) != 0)
                {
                    VisitedTypes.Value.Pop();
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
