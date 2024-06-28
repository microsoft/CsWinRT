// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Microsoft.UI.Xaml.Data;
using ABI.Windows.Foundation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

#if NET
using ComInterfaceEntry = System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry;
#endif

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
#if EMBED
    internal
#else
    public 
#endif
    static partial class ComWrappersSupport
    {
        internal const int GC_PRESSURE_BASE = 1000;

        private readonly static ConcurrentDictionary<Type, Func<IInspectable, object>> TypedObjectFactoryCacheForType = new ConcurrentDictionary<Type, Func<IInspectable, object>>();
        private readonly static ConditionalWeakTable<object, object> CCWTable = new ConditionalWeakTable<object, object>();
        private readonly static ConcurrentDictionary<Type, Func<IntPtr, object>> DelegateFactoryCache = new ConcurrentDictionary<Type, Func<IntPtr, object>>();

        public static TReturn MarshalDelegateInvoke<TDelegate, TReturn>(IntPtr thisPtr, Func<TDelegate, TReturn> invoke)
            where TDelegate : class, Delegate
        {
#if !NET
            using (new Mono.ThreadContext())
#endif
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
#if !NET
            using (new Mono.ThreadContext())
#endif
            {
                var target_invoke = FindObject<T>(thisPtr);
                if (target_invoke != null)
                {
                    invoke(target_invoke);
                }
            }
        }

        // If we are free threaded, we do not need to keep track of context.
        // This can either be if the object implements IAgileObject or the free threaded marshaler.
        internal unsafe static bool IsFreeThreaded(IObjectReference objRef)
        {
            if (objRef.TryAs(InterfaceIIDs.IAgileObject_IID, out var agilePtr) >= 0)
            {
                Marshal.Release(agilePtr);
                return true;
            }
            else if (objRef.TryAs(InterfaceIIDs.IMarshal_IID, out var marshalPtr) >= 0)
            {
                try
                {
                    Guid iid_IUnknown = InterfaceIIDs.IUnknown_IID;
                    Guid iid_unmarshalClass;
                    Marshal.ThrowExceptionForHR((**(ABI.WinRT.Interop.IMarshal.Vftbl**)marshalPtr).GetUnmarshalClass_0(
                        marshalPtr, &iid_IUnknown, IntPtr.Zero, MSHCTX.InProc, IntPtr.Zero, MSHLFLAGS.Normal, &iid_unmarshalClass));
                    if (iid_unmarshalClass == ABI.WinRT.Interop.IMarshal.IID_InProcFreeThreadedMarshaler.Value)
                    {
                        return true;
                    }
                }
                finally
                {
                    Marshal.Release(marshalPtr);
                }
            }
            return false;
        }

        public static IObjectReference GetObjectReferenceForInterface(IntPtr externalComObject)
        {
            return GetObjectReferenceForInterface<IUnknownVftbl>(externalComObject);
        }

        public static ObjectReference<T> GetObjectReferenceForInterface<T>(IntPtr externalComObject)
        {
            if (externalComObject == IntPtr.Zero)
            {
                return null;
            }

            ObjectReference<T> objRef = ObjectReference<T>.FromAbi(externalComObject);
            if (IsFreeThreaded(objRef))
            {
                return objRef;
            }
            else
            {
                using (objRef)
                {
                    return new ObjectReferenceWithContext<T>(
                        objRef.GetRef(),
                        Context.GetContextCallback(),
                        Context.GetContextToken());
                }
            }
        }

        public static ObjectReference<T> GetObjectReferenceForInterface<T>(IntPtr externalComObject, Guid iid)
        {
            return GetObjectReferenceForInterface<T>(externalComObject, iid, true);
        }

        internal static ObjectReference<T> GetObjectReferenceForInterface<T>(IntPtr externalComObject, Guid iid, bool requireQI)
        {
            if (externalComObject == IntPtr.Zero)
            {
                return null;
            }

            ObjectReference<T> objRef;
            if (requireQI)
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(externalComObject, ref iid, out IntPtr ptr));
                objRef = ObjectReference<T>.Attach(ref ptr);
            }
            else
            {
                objRef = ObjectReference<T>.FromAbi(externalComObject);
            }

            if (IsFreeThreaded(objRef))
            {
                return objRef;
            }
            else
            {
                using (objRef)
                {
                    return new ObjectReferenceWithContext<T>(
                        objRef.GetRef(),
                        Context.GetContextCallback(),
                        Context.GetContextToken(),
                        iid);
                }
            }
        }

        public static void RegisterProjectionAssembly(Assembly assembly) => TypeNameSupport.RegisterProjectionAssembly(assembly);

        public static void RegisterProjectionTypeBaseTypeMapping(IDictionary<string, string> typeNameToBaseTypeNameMapping) => TypeNameSupport.RegisterProjectionTypeBaseTypeMapping(typeNameToBaseTypeNameMapping);

        internal static object GetRuntimeClassCCWTypeIfAny(object obj)
        {
            var type = obj.GetType();
            var ccwType = type.GetRuntimeClassCCWType();
            if (ccwType != null)
            {
                return CCWTable.GetValue(obj, obj => {
                    var ccwConstructor = ccwType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { type }, null);
                    return ccwConstructor.Invoke(new[] { obj });
                });
            }

            return obj;
        }

        internal static List<ComInterfaceEntry> GetInterfaceTableEntries(
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
            Type type)
        {
            var entries = new List<ComInterfaceEntry>();
            bool hasCustomIMarshalInterface = false;

            if (type.IsDelegate())
            {
                // Delegates have no interfaces that they implement, so adding default WinRT entries.
                var helperType = type.FindHelperType();
                if (helperType is object)
                {
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(type),
                        Vtable = helperType.GetAbiToProjectionVftblPtr()
                    });
                }

                if (type.ShouldProvideIReference())
                {
                    entries.Add(IPropertyValueEntry);
                    entries.Add(ProvideIReference(type));
                }
            }
            else
            {
                var objType = type.GetRuntimeClassCCWType() ?? type;
                var interfaces = objType.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    if (Projections.IsTypeWindowsRuntimeType(iface))
                    {
                        var ifaceAbiType = iface.FindHelperType();
                        Guid iid = GuidGenerator.GetIID(ifaceAbiType);
                        entries.Add(new ComInterfaceEntry
                        {
                            IID = iid,
                            Vtable = (IntPtr)ifaceAbiType.GetAbiToProjectionVftblPtr()
                        });

                        if (!hasCustomIMarshalInterface && iid == InterfaceIIDs.IMarshal_IID)
                        {
                            hasCustomIMarshalInterface = true;
                        }
                    }

                    if (iface.IsConstructedGenericType
                        && Projections.TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, null, out var compatibleIfaces))
                    {
                        foreach (var compatibleIface in compatibleIfaces)
                        {
                            var compatibleIfaceAbiType = compatibleIface.FindHelperType();
                            entries.Add(new ComInterfaceEntry
                            {
                                IID = GuidGenerator.GetIID(compatibleIfaceAbiType),
                                Vtable = (IntPtr)compatibleIfaceAbiType.GetAbiToProjectionVftblPtr()
                            });
                        }
                    }
                }

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
                    // We treat arrays as if they implemented IIterable<T>, IVector<T>, and IVectorView<T> (WinRT only)
                    var elementType = type.GetElementType();
                    if (elementType.ShouldProvideIReference())
                    {
                        entries.Add(IPropertyValueEntry);
                        entries.Add(ProvideIReferenceArray(type));
                    }
                }
                else if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
                {
                    var ifaceAbiType = objType.FindHelperType();
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(ifaceAbiType),
                        Vtable = (IntPtr)ifaceAbiType.GetAbiToProjectionVftblPtr()
                    });
                }
                else if (type.ShouldProvideIReference())
                {
                    entries.Add(IPropertyValueEntry);
                    entries.Add(ProvideIReference(type));
                }
            }

            entries.Add(new ComInterfaceEntry
            {
                IID = ManagedIStringableVftbl.IID,
                Vtable = ManagedIStringableVftbl.AbiToProjectionVftablePtr
            });

            entries.Add(new ComInterfaceEntry
            {
                IID = ManagedCustomPropertyProviderVftbl.IID,
                Vtable = ManagedCustomPropertyProviderVftbl.AbiToProjectionVftablePtr
            });

            entries.Add(new ComInterfaceEntry
            {
                IID = InterfaceIIDs.IWeakReferenceSource_IID,
                Vtable = ABI.WinRT.Interop.IWeakReferenceSource.AbiToProjectionVftablePtr
            });

            // Add IMarhal implemented using the free threaded marshaler
            // to all CCWs if it doesn't already have its own.
            if (!hasCustomIMarshalInterface)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = InterfaceIIDs.IMarshal_IID,
                    Vtable = ABI.WinRT.Interop.IMarshal.Vftbl.AbiToProjectionVftablePtr
                });
            }

            // Add IAgileObject to all CCWs
            entries.Add(new ComInterfaceEntry
            {
                IID = InterfaceIIDs.IAgileObject_IID,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });

            entries.Add(new ComInterfaceEntry
            {
                IID = InterfaceIIDs.IInspectable_IID,
                Vtable = IInspectable.Vftbl.AbiToProjectionVftablePtr
            });

            // This should be the last entry as it is included / excluded based on the flags.
            entries.Add(new ComInterfaceEntry
            {
                IID = InterfaceIIDs.IUnknown_IID,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });

            return entries;
        }

#if NET
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The existence of the ABI type implies the non-ABI type exists, as in authoring scenarios the ABI type is constructed from the non-ABI type.")] 
#endif
        internal static (InspectableInfo inspectableInfo, List<ComInterfaceEntry> interfaceTableEntries) PregenerateNativeTypeInformation(Type type)
        {
            var interfaceTableEntries = GetInterfaceTableEntries(type);
            var iids = new Guid[interfaceTableEntries.Count];
            for (int i = 0; i < interfaceTableEntries.Count; i++)
            {
                iids[i] = interfaceTableEntries[i].IID;
            }

            if (type.FullName.StartsWith("ABI.", StringComparison.Ordinal))
            {
                type = Projections.FindCustomPublicTypeForAbiType(type) ?? type.Assembly.GetType(type.FullName.Substring("ABI.".Length)) ?? type;
            }

            return (
                new InspectableInfo(type, iids),
                interfaceTableEntries);
        }

        private static Func<IInspectable, object> CreateKeyValuePairFactory(Type type)
        {
            var createRcwFunc = (Func<IInspectable, object>) type.GetHelperType().GetMethod("CreateRcw", BindingFlags.Public | BindingFlags.Static).
                    CreateDelegate(typeof(Func<IInspectable, object>));
            return createRcwFunc;
        }

        internal static Func<IntPtr, object> CreateDelegateFactory(Type type)
        {
            return DelegateFactoryCache.GetOrAdd(type, (type) =>
            {
                var createRcwFunc = (Func<IntPtr, object>)type.GetHelperType().GetMethod("CreateRcw", BindingFlags.Public | BindingFlags.Static).
                        CreateDelegate(typeof(Func<IntPtr, object>));
                var iid = GuidGenerator.GetIID(type);

                return (IntPtr externalComObject) =>
                {
                    // The CreateRCW function for delegates expect the pointer to be the delegate interface in CsWinRT 1.5.
                    // But CreateObject is passed the IUnknown interface. This would typically be fine for delegates as delegates
                    // don't implement interfaces and implementations typically have both the IUnknown vtable and the delegate
                    // vtable point to the same vtable.  But when the pointer is to a proxy, that can not be relied on.
                    Marshal.ThrowExceptionForHR(Marshal.QueryInterface(externalComObject, ref iid, out var ptr));
                    try
                    {
                        return createRcwFunc(ptr);
                    }
                    finally
                    {
                        Marshal.Release(ptr);
                    }
                };
            });
        }

        private static Func<IInspectable, object> CreateNullableTFactory(Type implementationType)
        {
            var getValueMethod = implementationType.GetHelperType().GetMethod("GetValue", BindingFlags.Static | BindingFlags.NonPublic);
            return (IInspectable obj) => getValueMethod.Invoke(null, new[] { obj });
        }

        private static Func<IInspectable, object> CreateAbiNullableTFactory(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
            Type implementationType)
        {
            var getValueMethod = implementationType.GetMethod("GetValue", BindingFlags.Static | BindingFlags.NonPublic);
            return (IInspectable obj) => getValueMethod.Invoke(null, new[] { obj });
        }

        private static Func<IInspectable, object> CreateArrayFactory(Type implementationType)
        {
            var getValueFunc = (Func<IInspectable, object>)implementationType.GetHelperType().GetMethod("GetValue", BindingFlags.Static | BindingFlags.NonPublic).
                CreateDelegate(typeof(Func<IInspectable, object>));
            return getValueFunc;
        }

        // This is used to hold the reference to the native value type object (IReference) until the actual value in it (boxed as an object) gets cleaned up by GC
        // This is done to avoid pointer reuse until GC cleans up the boxed object
        private static readonly ConditionalWeakTable<object, IInspectable> _boxedValueReferenceCache = new();

        private static Func<IInspectable, object> CreateReferenceCachingFactory(Func<IInspectable, object> internalFactory)
        {
            return inspectable =>
            {
                object resultingObject = internalFactory(inspectable);
                _boxedValueReferenceCache.Add(resultingObject, inspectable);
                return resultingObject;
            };
        }

        private static Func<IInspectable, object> CreateCustomTypeMappingFactory(Type customTypeHelperType)
        {
            var fromAbiMethod = customTypeHelperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fromAbiMethod is null)
            {
                throw new MissingMethodException();
            }

            var fromAbiMethodFunc = (Func<IntPtr, object>) fromAbiMethod.CreateDelegate(typeof(Func<IntPtr, object>));
            return (IInspectable obj) => fromAbiMethodFunc(obj.ThisPtr);
        }

        internal static Func<IInspectable, object> CreateTypedRcwFactory(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
            Type implementationType,
            string runtimeClassName = null)
        {
            // If runtime class name is empty or "Object", then just use IInspectable.
            if (implementationType == null || implementationType == typeof(object))
            {
                // If we reach here, then we couldn't find a type that matches the runtime class name.
                // Fall back to using IInspectable directly.
                return (IInspectable obj) => obj;
            }

            if (implementationType == typeof(ABI.System.Nullable_string))
            {
                return CreateReferenceCachingFactory((IInspectable obj) => ABI.System.Nullable_string.GetValue(obj));
            }
            else if (implementationType == typeof(ABI.System.Nullable_Type))
            {
                return CreateReferenceCachingFactory((IInspectable obj) => ABI.System.Nullable_Type.GetValue(obj));
            }

            var customHelperType = Projections.FindCustomHelperTypeMapping(implementationType, true);
            if (customHelperType != null)
            {
                return CreateReferenceCachingFactory(CreateCustomTypeMappingFactory(customHelperType));
            }

            if (implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                return CreateReferenceCachingFactory(CreateKeyValuePairFactory(implementationType));
            }

            if (implementationType.IsValueType)
            {
                if (implementationType.IsNullableT())
                {
                    return CreateReferenceCachingFactory(CreateNullableTFactory(implementationType));
                }
                else
                {
                    return CreateReferenceCachingFactory(CreateNullableTFactory(typeof(System.Nullable<>).MakeGenericType(implementationType)));
                }
            }
            else if (implementationType.IsAbiNullableDelegate())
            {
                return CreateReferenceCachingFactory(CreateAbiNullableTFactory(implementationType));
            }
            else if (implementationType.IsIReferenceArray())
            {
                return CreateReferenceCachingFactory(CreateArrayFactory(implementationType));
            }

            return CreateFactoryForImplementationType(runtimeClassName, implementationType);
        }

#if NET
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        internal static Type GetRuntimeClassForTypeCreation(IInspectable inspectable, Type staticallyDeterminedType)
        {
            string runtimeClassName = inspectable.GetRuntimeClassName(noThrow: true);
            Type implementationType = null;
            if (!string.IsNullOrEmpty(runtimeClassName))
            {
                implementationType = TypeNameSupport.FindRcwTypeByNameCached(runtimeClassName);
            }

            if (staticallyDeterminedType != null && staticallyDeterminedType != typeof(object))
            {
                // We have a static type which we can use to construct the object.  But, we can't just use it for all scenarios
                // and primarily use it for tear off scenarios and for scenarios where runtimeclass isn't accurate.
                // For instance if the static type is an interface, we return an IInspectable to represent the interface.
                // But it isn't convertable back to the class via the as operator which would be possible if we use runtimeclass.
                // Similarly for composable types, they can be statically retrieved using the parent class, but can then no longer
                // be cast to the sub class via as operator even if it is really an instance of it per rutimeclass.
                // To handle these scenarios, we use the runtimeclass if we find it is assignable to the statically determined type.
                // If it isn't, we use the statically determined type as it is a tear off.
                if (!(implementationType != null &&
                    (staticallyDeterminedType == implementationType ||
                     staticallyDeterminedType.IsAssignableFrom(implementationType) ||
                     staticallyDeterminedType.IsGenericType && implementationType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == staticallyDeterminedType.GetGenericTypeDefinition()))))
                {
                    return staticallyDeterminedType;
                }
            }

            return implementationType;
        }

        private static ComInterfaceEntry IPropertyValueEntry =>
            new ComInterfaceEntry
            {
                IID = ManagedIPropertyValueImpl.IID,
                Vtable = ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
            };

        private static ComInterfaceEntry ProvideIReference(Type type)
        {
            if (type == typeof(int))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_int.IID,
                    Vtable = ABI.System.Nullable_int.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(string))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_string.IID,
                    Vtable = ABI.System.Nullable_string.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(byte))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_byte.IID,
                    Vtable = ABI.System.Nullable_byte.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(short))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_short.IID,
                    Vtable = ABI.System.Nullable_short.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ushort))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_ushort.IID,
                    Vtable = ABI.System.Nullable_ushort.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(uint))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_uint.IID,
                    Vtable = ABI.System.Nullable_uint.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(long))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_long.IID,
                    Vtable = ABI.System.Nullable_long.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ulong))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_ulong.IID,
                    Vtable = ABI.System.Nullable_ulong.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(float))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_float.IID,
                    Vtable = ABI.System.Nullable_float.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(double))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_double.IID,
                    Vtable = ABI.System.Nullable_double.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(char))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_char.IID,
                    Vtable = ABI.System.Nullable_char.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(bool))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_bool.IID,
                    Vtable = ABI.System.Nullable_bool.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(Guid))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_guid.IID,
                    Vtable = ABI.System.Nullable_guid.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(DateTimeOffset))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_DateTimeOffset.IID,
                    Vtable = ABI.System.Nullable_DateTimeOffset.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(TimeSpan))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_TimeSpan.IID,
                    Vtable = ABI.System.Nullable_TimeSpan.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(object))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_Object.IID,
                    Vtable = ABI.System.Nullable_Object.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type.IsTypeOfType())
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_Type.IID,
                    Vtable = ABI.System.Nullable_Type.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(sbyte))
            {
                return new ComInterfaceEntry
                {
                    IID = ABI.System.Nullable_sbyte.IID,
                    Vtable = ABI.System.Nullable_sbyte.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type.IsDelegate())
            {
                var delegateHelperType = typeof(ABI.System.Nullable_Delegate<>).MakeGenericType(type);
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(delegateHelperType),
                    Vtable = delegateHelperType.GetAbiToProjectionVftblPtr()
                };
            }

            return new ComInterfaceEntry
            {
                IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<>).MakeGenericType(type)),
                Vtable = typeof(BoxedValueIReferenceImpl<>).MakeGenericType(type).GetAbiToProjectionVftblPtr()
            };
        }

        private static ComInterfaceEntry ProvideIReferenceArray(Type arrayType)
        {
            Type type = arrayType.GetElementType();
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
            if (type.IsTypeOfType())
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
                Vtable = (IntPtr)typeof(BoxedArrayIReferenceArrayImpl<>).MakeGenericType(type).GetAbiToProjectionVftblPtr()
            };
        }

        internal sealed class InspectableInfo
        {
            private readonly Lazy<string> runtimeClassName;

            public Guid[] IIDs { get; }
            public string RuntimeClassName => runtimeClassName.Value;

            internal InspectableInfo(Type type, Guid[] iids)
            {
                runtimeClassName = new Lazy<string>(() => TypeNameSupport.GetNameForType(type, TypeNameGenerationFlags.GenerateBoxedName | TypeNameGenerationFlags.ForGetRuntimeClassName));
                IIDs = iids;
            }
        }

        internal static ObjectReference<T> CreateCCWForObject<T>(object obj, Guid iid)
        {
            IntPtr ccw = CreateCCWForObjectForABI(obj, iid);
            return ObjectReference<T>.Attach(ref ccw);
        }

        internal static ObjectReferenceValue CreateCCWForObjectForMarshaling(object obj, Guid iid)
        {
            IntPtr ccw = CreateCCWForObjectForABI(obj, iid);
            return new ObjectReferenceValue(ccw);
        }
    }
}