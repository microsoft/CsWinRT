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
using ABI.Microsoft.UI.Xaml.Data;

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
                    type.GetProperty(projectedClass.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(o),
                    out objRef);
            }

            objRef = null;
            return false;
        }

        public static IObjectReference GetObjectReferenceForInterface(IntPtr externalComObject)
        {
            using var unknownRef = ObjectReference<IUnknownVftbl>.FromAbi(externalComObject);

            if (unknownRef.TryAs<IUnknownVftbl>(typeof(ABI.WinRT.Interop.IAgileObject.Vftbl).GUID, out var agileRef) >= 0)
            {
                agileRef.Dispose();
                return unknownRef.As<IUnknownVftbl>();
            }
            else
            {
                return new ObjectReferenceWithContext<IUnknownVftbl>(
                    unknownRef.GetRef(),
                    Context.GetContextCallback());
            }
        }

        public static void RegisterProjectionAssembly(Assembly assembly) => TypeNameSupport.RegisterProjectionAssembly(assembly);

        internal static object GetRuntimeClassCCWTypeIfAny(object obj)
        {
            var type = obj.GetType();
            var ccwType = type.GetRuntimeClassCCWType();
            if (ccwType != null)
            {
                // TODO: have some weak conditional table lookup before constructing new one.
                var objReferenceConstructor = ccwType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { type }, null);
                return objReferenceConstructor.Invoke(new[] { obj });
            }

            return obj;
        }

        internal static List<ComInterfaceEntry> GetInterfaceTableEntries(object obj)
        {
            var entries = new List<ComInterfaceEntry>();
            var interfaces = obj.GetType().GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (Projections.IsTypeWindowsRuntimeType(iface))
                {
                    var ifaceAbiType = iface.FindHelperType();
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(ifaceAbiType),
                        Vtable = (IntPtr)ifaceAbiType.FindVftblType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                    });
                }

                if (iface.IsConstructedGenericType
                    && Projections.TryGetCompatibleWindowsRuntimeTypeForVariantType(iface, out var compatibleIface))
                {
                    var compatibleIfaceAbiType = compatibleIface.FindHelperType();
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(compatibleIfaceAbiType),
                        Vtable = (IntPtr)compatibleIfaceAbiType.FindVftblType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                    });
                }
            }

            if (obj is Delegate)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(obj.GetType()),
                    Vtable = (IntPtr)obj.GetType().GetHelperType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }

            var objType = obj.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                var ifaceAbiType = objType.FindHelperType();
                entries.Add(new ComInterfaceEntry
                {
                    IID = GuidGenerator.GetIID(ifaceAbiType),
                    Vtable = (IntPtr)ifaceAbiType.FindVftblType().GetField("AbiToProjectionVftablePtr", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                });
            }
            else if (ShouldProvideIReference(obj))
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
                IID = typeof(ManagedIStringableVftbl).GUID,
                Vtable = ManagedIStringableVftbl.AbiToProjectionVftablePtr
            });

            entries.Add(new ComInterfaceEntry
            {
                IID = typeof(ManagedCustomPropertyProviderVftbl).GUID,
                Vtable = ManagedCustomPropertyProviderVftbl.AbiToProjectionVftablePtr
            });

            entries.Add(new ComInterfaceEntry
            {
                IID = typeof(ABI.WinRT.Interop.IWeakReferenceSource.Vftbl).GUID,
                Vtable = ABI.WinRT.Interop.IWeakReferenceSource.Vftbl.AbiToProjectionVftablePtr
            });

            // Add IAgileObject to all CCWs
            entries.Add(new ComInterfaceEntry
            {
                IID = typeof(ABI.WinRT.Interop.IAgileObject.Vftbl).GUID,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });
            return entries;
        }

        internal static (InspectableInfo inspectableInfo, List<ComInterfaceEntry> interfaceTableEntries) PregenerateNativeTypeInformation(object obj)
        {
            var interfaceTableEntries = GetInterfaceTableEntries(obj);
            var iids = new Guid[interfaceTableEntries.Count];
            for (int i = 0; i < interfaceTableEntries.Count; i++)
            {
                iids[i] = interfaceTableEntries[i].IID;
            }

            Type type = obj.GetType();

            if (type.FullName.StartsWith("ABI."))
            {
                type = Projections.FindCustomPublicTypeForAbiType(type) ?? type.Assembly.GetType(type.FullName.Substring("ABI.".Length)) ?? type;
            }

            return (
                new InspectableInfo(type, iids),
                interfaceTableEntries);
        }

        private static bool IsNullableT(Type implementationType)
        {
            return implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeof(System.Nullable<>);
        }

        private static bool IsIReferenceArray(Type implementationType)
        {
            return implementationType.FullName.StartsWith("Windows.Foundation.IReferenceArray`1");
        }

        private static Func<IInspectable, object> CreateKeyValuePairFactory(Type type)
        {
            var parms = new[] { Expression.Parameter(typeof(IInspectable), "obj") };
            return Expression.Lambda<Func<IInspectable, object>>(
                Expression.Call(type.GetHelperType().GetMethod("CreateRcw", BindingFlags.Public | BindingFlags.Static), 
                    parms), parms).Compile();
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
            // If runtime class name is empty or "Object", then just use IInspectable.
            if (string.IsNullOrEmpty(runtimeClassName) || runtimeClassName == "Object")
            {
                return (IInspectable obj) => obj;
            }
            // PropertySet and ValueSet can return IReference<String> but Nullable<String> is illegal
            if (runtimeClassName == "Windows.Foundation.IReference`1<String>")
            {
                return (IInspectable obj) => new ABI.System.Nullable<String>(obj.ObjRef);
            }
            else if (runtimeClassName == "Windows.Foundation.IReference`1<Windows.UI.Xaml.Interop.TypeName>")
            {
                return (IInspectable obj) => new ABI.System.Nullable<Type>(obj.ObjRef);
            }

            Type implementationType = null;

            try
            {
                (implementationType, _) = TypeNameSupport.FindTypeByName(runtimeClassName.AsSpan());
            }
            catch (TypeLoadException)
            {
                // If we reach here, then we couldn't find a type that matches the runtime class name.
                // Fall back to using IInspectable directly.
                return (IInspectable obj) => obj;
            }

            if (implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                return CreateKeyValuePairFactory(implementationType);
            }

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

        private static bool ShouldProvideIReference(object obj)
        {
            return obj.GetType().IsValueType || obj is string || obj is Type || obj is Delegate;
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
            if (obj is Type)
            {
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<Type>)),
                    Vtable = BoxedArrayIReferenceArrayImpl<Type>.AbiToProjectionVftablePtr
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
            private readonly Lazy<string> runtimeClassName;

            public Guid[] IIDs { get; }
            public string RuntimeClassName => runtimeClassName.Value;

            internal InspectableInfo(Type type, Guid[] iids)
            {
                runtimeClassName = new Lazy<string>(() => TypeNameSupport.GetNameForType(type, TypeNameGenerationFlags.GenerateBoxedName | TypeNameGenerationFlags.NoCustomTypeName));
                IIDs = iids;
            }

        }
    }
}