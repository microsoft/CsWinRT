using System;
using System.Collections.Generic;
using System.Text;

namespace WinRT
{
    static class TypeNameSupport
    {
        /// <summary>
        /// Parse the first full type name within the provided span.
        /// </summary>
        /// <param name="runtimeClassName">The runtime class name to attempt to parse.</param>
        /// <returns>A tuple containing the resolved type and the index of the end of the resolved type name.</returns>
        public static (Type type, int remaining) FindTypeByName(ReadOnlySpan<char> runtimeClassName)
        {
            var (genericTypeName, genericTypes, remaining) = ParseGenericTypeName(runtimeClassName);
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
        private static Type FindTypeByNameCore(string runtimeClassName, Type[] genericTypes)
        {
            Type resolvedType = Projections.FindTypeForAbiTypeName(runtimeClassName);

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
                "Single" => typeof(float),
                "Double" => typeof(double),
                "Guid" => typeof(Guid),
                "Object" => typeof(object),
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
            int possibleEndOfSimpleTypeName = partialTypeName.IndexOfAny(new[] { ',', '>' });
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
                    break;
                }
                else
                {
                    throw new InvalidOperationException("The provided type name is invalid.");
                }
            }
            return (genericTypeName.ToString(), genericTypes.ToArray(), partialTypeName.Length - remainingTypeName.Length);
        }

        public static string GetNameForType(Type type, bool boxedType)
        {
            if (type is null)
            {
                return string.Empty;
            }
            StringBuilder nameBuilder = new StringBuilder();
            AppendTypeName(type, nameBuilder, boxedType);
            return nameBuilder.ToString();
        }

        private static void AppendSimpleTypeName(Type type, StringBuilder builder, bool boxedType)
        {
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid))
            {
                if (boxedType)
                {
                    builder.Append("Windows.Foundation.IReference`1<");
                    AppendSimpleTypeName(type, builder, false);
                    builder.Append(">");
                    return;
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
                builder.Append(Projections.FindAbiTypeNameForType(type) ?? type.FullName);
            }
        }

        private static void AppendTypeName(Type type, StringBuilder builder, bool boxedType)
        {
#if NETSTANDARD2_0
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
                AppendTypeName(type, builder, boxedType);
                builder.Append(">");
            }

            if (!type.IsGenericType)
            {
                AppendSimpleTypeName(type, builder, boxedType);
                return;
            }

            AppendSimpleTypeName(type.GetGenericTypeDefinition(), builder, boxedType);

            builder.Append('<');

            bool first = true;

            foreach (var argument in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(", ");
                }
                AppendTypeName(argument, builder, boxedType);
                first = false;
            }

            builder.Append('>');
        }
    }
}
