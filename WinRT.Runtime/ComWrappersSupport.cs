using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;
using WinRT.Interop;
using ABI.Windows.Foundation;

#if !NETSTANDARD2_0
using ComInterfaceEntry = System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry;
#endif

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public static partial class ComWrappersSupport
    {
        private readonly static ConcurrentDictionary<string, Func<IInspectable, object>> TypedObjectFactoryCache = new ConcurrentDictionary<string, Func<IInspectable, object>>();

        private readonly static Guid IID_IAgileObject = Guid.Parse("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");

        static ComWrappersSupport()
        {
            PlatformSpecificInitialize();
        }

        static partial void PlatformSpecificInitialize();

        public static TReturn MarshalDelegateInvoke<TDelegate, TReturn>(IntPtr thisPtr, Func<TDelegate, TReturn> invoke)
            where TDelegate : class, Delegate
        {
            using (new Mono.ThreadContext())
            {
                var target_invoke = FindObject<TDelegate>(thisPtr);
                if (target_invoke != null)
                {
                    return invoke(target_invoke);
                }
                return default;
            }
        }

        public static void MarshalDelegateInvoke<T>(IntPtr thisPtr, Action<T> invoke)
            where T : class, Delegate
        {
            using (new Mono.ThreadContext())
            {
                var target_invoke = FindObject<T>(thisPtr);
                if (target_invoke != null)
                {
                    invoke(target_invoke);
                }
            }
        }

        public static bool TryUnwrapObject(object o, out IObjectReference objRef)
        {
            // The unwrapping here needs to be in exact type match in case the user
            // has implemented a WinRT interface or inherited from a WinRT class
            // in a .NET (non-projected) type.

            if (o is Delegate del)
            {
                return TryUnwrapObject(del.Target, out objRef);
            }

            Type type = o.GetType();
            ObjectReferenceWrapperAttribute objRefWrapper = type.GetCustomAttribute<ObjectReferenceWrapperAttribute>();
            if (objRefWrapper is object)
            {
                objRef = (IObjectReference)type.GetField(objRefWrapper.ObjectReferenceField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(o);
                return true;
            }

            ProjectedRuntimeClassAttribute projectedClass = type.GetCustomAttribute<ProjectedRuntimeClassAttribute>();

            if (projectedClass is object)
            {
                return TryUnwrapObject(
                    type.GetField(projectedClass.DefaultInterfaceField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(o),
                    out objRef);
            }

            objRef = null;
            return false;
        }

        internal static IObjectReference GetObjectReferenceForIntPtr(IntPtr externalComObject, bool addRef)
        {
            IObjectReference objRefToReturn = null;
            ObjectReference<IUnknownVftbl> unknownRef;
            if (addRef)
            {
                unknownRef = ObjectReference<IUnknownVftbl>.FromAbi(externalComObject);
            }
            else
            {
                unknownRef = ObjectReference<IUnknownVftbl>.Attach(ref externalComObject);
            }

            using (unknownRef)
            {
                try
                {
                    var agileObjectRef = unknownRef.As(IID_IAgileObject);
                    agileObjectRef.Dispose();
                    objRefToReturn = unknownRef.As<IUnknownVftbl>();
                }
                catch (Exception)
                {
                    objRefToReturn = new ObjectReferenceWithContext<IUnknownVftbl>(unknownRef.GetRef(), Context.GetContextCallback());
                }
            }
            return objRefToReturn;
        }

        internal static List<ComInterfaceEntry> GetInterfaceTableEntries(object obj)
        {
            var entries = new List<ComInterfaceEntry>();
            var interfaces = obj.GetType().GetInterfaces();
            foreach (var iface in interfaces)
            {
                var ifaceAbiType = iface.FindHelperType();
                if (ifaceAbiType == null)
                {
                    // This interface isn't a WinRT interface.
                    // TODO: Handle WinRT -> .NET projected interfaces.
                    continue;
                }

                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(ifaceAbiType),
                    Vtable = (IntPtr)ifaceAbiType.FindVftblType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }

            if (obj is Delegate)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(obj.GetType()),
                    Vtable = (IntPtr)obj.GetType().GetHelperType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }

            foreach (var (IID, Vtable) in Projections.GetAdditionalVtablesForObject(obj))
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = IID,
                    Vtable = Vtable
                });
            }

            if (ShouldProvideIReference(obj))
            {
                entries.Add(IPropertyValueEntry);
                entries.Add(ProvideIReference(obj));
            }
            else if (ShouldProvideIReferenceArray(obj))
            {
                entries.Add(IPropertyValueEntry);
                entries.Add(ProvideIReferenceArray(obj));
            }

            entries.Add(new ComInterfaceEntry
            {
                IID = typeof(IWeakReferenceSourceVftbl).GUID,
                Vtable = IWeakReferenceSourceVftbl.AbiToProjectionVftablePtr
            });

            // Add IAgileObject to all CCWs
            entries.Add(new ComInterfaceEntry
            {
                IID = IID_IAgileObject,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });
            return entries;
        }

        internal static (InspectableInfo inspectableInfo, List<ComInterfaceEntry> interfaceTableEntries) PregenerateNativeTypeInformation(object obj)
        {
            string typeName = GetRuntimeClassNameForType(obj.GetType());

            var interfaceTableEntries = GetInterfaceTableEntries(obj);
            var iids = new Guid[interfaceTableEntries.Count];
            for (int i = 0; i < interfaceTableEntries.Count; i++)
            {
                iids[i] = interfaceTableEntries[i].IID;
            }

            return (
                new InspectableInfo(typeName, iids),
                interfaceTableEntries);
        }

        private static string GetRuntimeClassNameForType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(System.Nullable<>))
                {
                    return $"Windows.Foundation.IReference<{GetRuntimeClassNameForType(type.GenericTypeArguments[0])}>";
                }
            }
            else if (type.IsArray)
            {
                return $"Windows.Foundation.IReferenceArray<{GetRuntimeClassNameForType(type.GetElementType())}>";
            }

            string typeName = type.FullName;
            if (typeName.StartsWith("ABI.")) // If our type is an ABI type, get the real type name
            {
                typeName = typeName.Substring("ABI.".Length);
            }

            return typeName;
        }

        private static bool IsNullableT(Type implementationType)
        {
            return implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }

        private static bool IsIReferenceArray(Type implementationType)
        {
            return implementationType.FullName.StartsWith("Windows.Foundation.IReferenceArray`1");
        }

        private static Func<IInspectable, object> CreateNullableTFactory(Type implementationType)
        {
            Type helperType = implementationType.GetHelperType();
            Type vftblType = helperType.FindVftblType();

            ParameterExpression[] parms = new[] { Expression.Parameter(typeof(IInspectable), "inspectable") };
            var createInterfaceInstanceExpression = Expression.New(helperType.GetConstructor(new[] { typeof(ObjectReference<>).MakeGenericType(vftblType) }),
                    Expression.Call(parms[0],
                        typeof(IInspectable).GetMethod(nameof(IInspectable.As)).MakeGenericMethod(vftblType)));
            
            return Expression.Lambda<Func<IInspectable, object>>(
                Expression.Convert(Expression.Property(createInterfaceInstanceExpression, "Value"), typeof(object)), parms).Compile();
        }

        private static Func<IInspectable, object> CreateArrayFactory(Type implementationType)
        {
            Type helperType = implementationType.GetHelperType();
            Type vftblType = helperType.FindVftblType();

            ParameterExpression[] parms = new[] { Expression.Parameter(typeof(IInspectable), "inspectable") };
            var createInterfaceInstanceExpression = Expression.New(helperType.GetConstructor(new[] { typeof(ObjectReference<>).MakeGenericType(vftblType) }),
                    Expression.Call(parms[0],
                        typeof(IInspectable).GetMethod(nameof(IInspectable.As)).MakeGenericMethod(vftblType)));

            return Expression.Lambda<Func<IInspectable, object>>(
                Expression.Property(createInterfaceInstanceExpression, "Value"), parms).Compile();
        }

        internal static Func<IInspectable, object> CreateTypedRcwFactory(string runtimeClassName)
        {
            var (implementationType, _) = FindTypeByName(runtimeClassName.AsSpan());

            if (implementationType.IsValueType)
            {
                if (IsNullableT(implementationType))
                {
                    return CreateNullableTFactory(implementationType);
                }
                else
                {
                    return CreateNullableTFactory(typeof(System.Nullable<>).MakeGenericType(implementationType));
                }
            }
            else if (IsIReferenceArray(implementationType))
            {
                return CreateArrayFactory(implementationType);
            }

            Type classType;
            Type interfaceType;
            Type vftblType;
            if (implementationType.IsInterface)
            {
                classType = null;
                interfaceType = implementationType.GetHelperType() ??
                    throw new TypeLoadException($"Unable to find an ABI implementation for the type '{runtimeClassName}'");
                vftblType = interfaceType.FindVftblType() ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
                if (vftblType.IsGenericTypeDefinition)
                {
                    vftblType = vftblType.MakeGenericType(interfaceType.GetGenericArguments());
                }
            }
            else
            {
                classType = implementationType;
                interfaceType = Projections.GetDefaultInterfaceTypeForRuntimeClassType(classType);
                if (interfaceType is null)
                {
                    throw new TypeLoadException($"Unable to create a runtime wrapper for a WinRT object of type '{runtimeClassName}'. This type is not a projected type.");
                }
                vftblType = interfaceType.FindVftblType() ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
            }

            ParameterExpression[] parms = new[] { Expression.Parameter(typeof(IInspectable), "inspectable") };
            var createInterfaceInstanceExpression = Expression.New(interfaceType.GetConstructor(new[] { typeof(ObjectReference<>).MakeGenericType(vftblType) }),
                    Expression.Call(parms[0],
                        typeof(IInspectable).GetMethod(nameof(IInspectable.As)).MakeGenericMethod(vftblType)));

            if (classType is null)
            {
                return Expression.Lambda<Func<IInspectable, object>>(createInterfaceInstanceExpression, parms).Compile();
            }

            return Expression.Lambda<Func<IInspectable, object>>(
                Expression.New(classType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { interfaceType }, null),
                    createInterfaceInstanceExpression),
                parms).Compile();
        }

        /// <summary>
        /// Parse the first full type name within the provided span.
        /// </summary>
        /// <param name="runtimeClassName">The runtime class name to attempt to parse.</param>
        /// <returns>A tuple containing the resolved type and the index of the end of the resolved type name.</returns>
        private static (Type type, int remaining) FindTypeByName(ReadOnlySpan<char> runtimeClassName)
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

        private static Type ResolvePrimitiveType(string primitiveTypeName)
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


        private static bool ShouldProvideIReference(object obj)
        {
            // TODO: Handle types that are value-types in .NET and interfaces in WinRT and vice-versa
            return obj.GetType().IsValueType || obj is string;
        }


        private static ComInterfaceEntry IPropertyValueEntry =>
            new ComInterfaceEntry
            {
                IID = global::WinRT.GuidGenerator.GetIID(typeof(global::Windows.Foundation.IPropertyValue)),
                Vtable = ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
            };

        private static ComInterfaceEntry ProvideIReference(object obj)
        {
            Type type = obj.GetType();

            if (type == typeof(int))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<int>)),
                    Vtable = BoxedValueIReferenceImpl<int>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(string))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<string>)),
                    Vtable = BoxedValueIReferenceImpl<string>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(byte))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<byte>)),
                    Vtable = BoxedValueIReferenceImpl<byte>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(short))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<short>)),
                    Vtable = BoxedValueIReferenceImpl<short>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ushort))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<ushort>)),
                    Vtable = BoxedValueIReferenceImpl<ushort>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(uint))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<uint>)),
                    Vtable = BoxedValueIReferenceImpl<uint>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(long))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<long>)),
                    Vtable = BoxedValueIReferenceImpl<long>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ulong))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<ulong>)),
                    Vtable = BoxedValueIReferenceImpl<ulong>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(float))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<float>)),
                    Vtable = BoxedValueIReferenceImpl<float>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(double))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<double>)),
                    Vtable = BoxedValueIReferenceImpl<double>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(char))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<char>)),
                    Vtable = BoxedValueIReferenceImpl<char>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(bool))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<bool>)),
                    Vtable = BoxedValueIReferenceImpl<bool>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(Guid))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<Guid>)),
                    Vtable = BoxedValueIReferenceImpl<Guid>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(DateTimeOffset))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<DateTimeOffset>)),
                    Vtable = BoxedValueIReferenceImpl<DateTimeOffset>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(TimeSpan))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<TimeSpan>)),
                    Vtable = BoxedValueIReferenceImpl<TimeSpan>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(object))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<object>)),
                    Vtable = BoxedValueIReferenceImpl<object>.AbiToProjectionVftablePtr
                };
            }
            if (obj is Type)
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<Type>)),
                    Vtable = BoxedValueIReferenceImpl<Type>.AbiToProjectionVftablePtr
                };
            }

            return new ComInterfaceEntry
            {
                IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<>).MakeGenericType(type)),
                Vtable = (IntPtr)typeof(BoxedValueIReferenceImpl<>).MakeGenericType(type).GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
            };
        }

        private static bool ShouldProvideIReferenceArray(object obj)
        {
            return obj is Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0 && !obj.GetType().GetElementType().IsArray;
        }

        private static ComInterfaceEntry ProvideIReferenceArray(object obj)
        {
            Type type = obj.GetType().GetElementType();
            if (type == typeof(int))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<int>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<int>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(string))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<string>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<string>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(byte))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<byte>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<byte>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(short))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<short>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<short>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ushort))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<ushort>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<ushort>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(uint))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<uint>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<uint>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(long))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<long>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<long>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ulong))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<ulong>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<ulong>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(float))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<float>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<float>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(double))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<double>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<double>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(char))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<char>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<char>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(bool))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<bool>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<bool>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(Guid))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<Guid>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<Guid>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(DateTimeOffset))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<DateTimeOffset>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<DateTimeOffset>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(TimeSpan))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<TimeSpan>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<TimeSpan>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(object))
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<object>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<object>.AbiToProjectionVftablePtr
                };
            }
            if (obj is global::System.Type)
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<global::System.Type>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<global::System.Type>.AbiToProjectionVftablePtr
                };
            }
            return new ComInterfaceEntry
            {
                IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<>).MakeGenericType(type)),
                Vtable = (IntPtr)typeof(BoxedArrayIReferenceArrayImpl<>).MakeGenericType(type).GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
            };
        }

        internal class InspectableInfo
        {
            public readonly string RuntimeClassName;
            public readonly Guid[] IIDs;

            internal InspectableInfo(string runtimeClassName, Guid[] iids)
            {
                RuntimeClassName = runtimeClassName;
                IIDs = iids;
            }

        }
    }
}
