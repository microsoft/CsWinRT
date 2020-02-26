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
            where TDelegate : class, System.Delegate
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
            where T : class, System.Delegate
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

            if (o is global::System.Delegate del)
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
                    objRefToReturn = new ObjectReferenceWithContext<IUnknownVftbl>(unknownRef.GetRef(), Platform.GetContextCallback());
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
                var ifaceAbiType = iface.Assembly.GetType("ABI." + iface.FullName);
                if (ifaceAbiType == null)
                {
                    // This interface isn't a WinRT interface.
                    // TODO: Handle WinRT -> .NET projected interfaces.
                    continue;
                }

                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(ifaceAbiType),
                    Vtable = (IntPtr)ifaceAbiType.GetNestedType("Vftbl").GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }

            if (obj is global::System.Delegate)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(obj.GetType()),
                    Vtable = (IntPtr)obj.GetType().GetHelperType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }

            // TODO: Handle Arrays

            entries.Add(new ComInterfaceEntry
            {
                IID = typeof(WinRT.Interop.IWeakReferenceSourceVftbl).GUID,
                Vtable = WinRT.Interop.IWeakReferenceSourceVftbl.AbiToProjectionVftablePtr
            });

            // Add IAgileObject to all CCWs
            entries.Add(new ComInterfaceEntry
            {
                IID = IID_IAgileObject,
                Vtable = WinRT.Interop.IUnknownVftbl.AbiToProjectionVftblPtr
            });
            return entries;
        }

        internal static (InspectableInfo inspectableInfo, List<ComInterfaceEntry> interfaceTableEntries) PregenerateNativeTypeInformation(object obj)
        {
            string typeName = obj.GetType().FullName;
            if (typeName.StartsWith("ABI.")) // If our type is an ABI type, get the real type name
            {
                typeName = typeName.Substring("ABI.".Length);
            }

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

        internal static Func<IInspectable, object> CreateTypedRcwFactory(string runtimeClassName)
        {
            var (implementationType, _) = FindTypeByName(runtimeClassName.AsSpan());

            Type classType;
            Type interfaceType;
            Type vftblType;
            if (implementationType.IsInterface)
            {
                classType = null;
                interfaceType = implementationType.GetHelperType() ??
                    throw new TypeLoadException($"Unable to find an ABI implementation for the type '{runtimeClassName}'");
                vftblType = interfaceType.GetNestedType("Vftbl") ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
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
                vftblType = interfaceType.GetNestedType("Vftbl") ?? throw new TypeLoadException($"Unable to find a vtable type for the type '{runtimeClassName}'");
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

        private static (Type type, int remaining) FindTypeByName(ReadOnlySpan<char> runtimeClassName)
        {
            var (genericTypeName, genericTypes, remaining) = ParseGenericTypeName(runtimeClassName);
            return (FindTypeByNameCore(genericTypeName, genericTypes), remaining);
        }

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

        private static (string genericTypeName, Type[] genericTypes, int remaining) ParseGenericTypeName(ReadOnlySpan<char> partialTypeName)
        {
            int possibleEndOfSimpleTypeName = partialTypeName.IndexOfAny(new[] { ',', '>' });
            int endOfSimpleTypeName = partialTypeName.Length;
            if (possibleEndOfSimpleTypeName != -1)
            {
                endOfSimpleTypeName = possibleEndOfSimpleTypeName;
            }
            var typeName = partialTypeName.Slice(0, endOfSimpleTypeName);

            if (!typeName.Contains("`".AsSpan(), StringComparison.Ordinal))
            {
                return (typeName.ToString(), null, endOfSimpleTypeName);
            }

            int genericTypeListStart = partialTypeName.IndexOf('<');
            var genericTypeName = partialTypeName.Slice(0, genericTypeListStart);
            var remaining = partialTypeName.Slice(genericTypeListStart + 1);
            int remainingIndex = genericTypeListStart + 1;
            List<Type> genericTypes = new List<Type>();
            while (true)
            {
                var (genericType, endOfGenericArgument) = FindTypeByName(remaining);
                remainingIndex += endOfGenericArgument;
                genericTypes.Add(genericType);
                remaining = remaining.Slice(endOfGenericArgument);
                if (remaining[0] == ',')
                {
                    // Skip the comma and the space in the type name.
                    remainingIndex += 2;
                    remaining = remaining.Slice(2);
                    continue;
                }
                else if (remaining[0] == '>')
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException("The provided type name is invalid.");
                }
            }
            return (genericTypeName.ToString(), genericTypes.ToArray(), partialTypeName.Length - remaining.Length);
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
