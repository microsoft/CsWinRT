// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using ABI.Microsoft.UI.Xaml.Data;
using ABI.Windows.Foundation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    internal static class KeyValuePairHelper
    {
        internal static readonly ConcurrentDictionary<Type, ComInterfaceEntry> KeyValuePairCCW = new();

        internal static void TryAddKeyValuePairCCW(Type keyValuePairType, Guid iid, IntPtr abiToProjectionVftablePtr)
        {
            KeyValuePairCCW.TryAdd(keyValuePairType, new ComInterfaceEntry { IID = iid, Vtable = abiToProjectionVftablePtr });
        }
    }

#if EMBED
    internal
#else
    public 
#endif
    static partial class ComWrappersSupport
    {
        internal const int GC_PRESSURE_BASE = 1000;

        private static readonly ConcurrentDictionary<Type, Func<IInspectable, object>> TypedObjectFactoryCacheForType = new();
        private static readonly ConcurrentDictionary<Type, Func<IntPtr, object>> DelegateFactoryCache = new();

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
        internal unsafe static bool IsFreeThreaded(IntPtr iUnknown)
        {
            if (Marshal.QueryInterface(iUnknown, ref Unsafe.AsRef(in IID.IID_IAgileObject), out var agilePtr) >= 0)
            {
                Marshal.Release(agilePtr);
                return true;
            }

            if (Marshal.QueryInterface(iUnknown, ref Unsafe.AsRef(in IID.IID_IMarshal), out var marshalPtr) >= 0)
            {
                try
                {
                    Guid iid_IUnknown = IID.IID_IUnknown;
                    Guid iid_unmarshalClass;
                    Marshal.ThrowExceptionForHR((**(ABI.WinRT.Interop.IMarshal.Vftbl**)marshalPtr).GetUnmarshalClass_0(
                        marshalPtr, &iid_IUnknown, IntPtr.Zero, MSHCTX.InProc, IntPtr.Zero, MSHLFLAGS.Normal, &iid_unmarshalClass));
                    if (iid_unmarshalClass == ABI.WinRT.Interop.IMarshal.IID_InProcFreeThreadedMarshaler)
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

        internal unsafe static bool IsFreeThreaded(IObjectReference objRef)
        {
            var isFreeThreaded = IsFreeThreaded(objRef.ThisPtr);
            // ThisPtr is owned by objRef, so need to make sure objRef stays alive.
            GC.KeepAlive(objRef);
            return isFreeThreaded;
        }

        public static IObjectReference GetObjectReferenceForInterface(IntPtr externalComObject)
        {
            // Here the ptr itself might not point to IUnknown, but we are using IUnknown for the purposes of getting
            // an agile reference if needed.  Due to that and to keep back compat, making sure to not trigger a QI
            // In addition, the ptr is already pointing to the correct interface which the IObjectReference is expected for.
            return GetObjectReferenceForInterface<IUnknownVftbl>(externalComObject, IID.IID_IUnknown, requireQI: false);
        }

#if NET
        [RequiresUnreferencedCode(AttributeMessages.GenericRequiresUnreferencedCodeMessage)]
#endif
        public static ObjectReference<T> GetObjectReferenceForInterface<T>(IntPtr externalComObject)
        {
            if (externalComObject == IntPtr.Zero)
            {
                return null;
            }

            return ObjectReference<T>.FromAbi(externalComObject);
        }

        public static ObjectReference<T> GetObjectReferenceForInterface<T>(IntPtr externalComObject, Guid iid)
        {
            return GetObjectReferenceForInterface<T>(externalComObject, iid, requireQI: true);
        }

        /// <summary>
        /// Creates a <see cref="IObjectReference"/> object for a given <see cref="IntPtr"/> COM pointer.
        /// As part of this, the <see cref="Guid"/> IID is set in the <see cref="IObjectReference"/> object
        /// which is used in non agile scenarios.  In addition, if <paramref name="requireQI"/> is set to true, a QI to that <paramref name="iid"/> is done.
        /// Otherwise it is assumed, the passed COM pointer already points to the interface represented by the <paramref name="iid"/>.
        /// </summary>
        /// <param name="externalComObject">The native <see cref="IntPtr"/> object for which to construct the <see cref="IObjectReference"/> object.</param>
        /// <param name="iid">The <see cref="Guid"/> IID that represents the interface which the resulting <see cref="IObjectReference"/> object will be pointing to.</param>
        /// <param name="requireQI">Whether to QI as part of returning the object.</param>
        /// <returns>The <see cref="IObjectReference"/> holding onto the <paramref name="externalComObject"/> pointer passed or its QI.</returns>
        public static IObjectReference GetObjectReferenceForInterface(IntPtr externalComObject, Guid iid, bool requireQI)
        {
            return GetObjectReferenceForInterface<IUnknownVftbl>(externalComObject, iid, requireQI);
        }

        internal static ObjectReference<T> GetObjectReferenceForInterface<T>(IntPtr externalComObject, Guid iid, bool requireQI)
        {
            if (externalComObject == IntPtr.Zero)
            {
                return null;
            }

            if (requireQI)
            {
                Marshal.ThrowExceptionForHR(Marshal.QueryInterface(externalComObject, ref iid, out IntPtr ptr));
                return ObjectReference<T>.Attach(ref ptr, iid);
            }
            else
            {
                return ObjectReference<T>.FromAbi(externalComObject, iid);
            }
        }

        public static void RegisterProjectionAssembly(Assembly assembly) => TypeNameSupport.RegisterProjectionAssembly(assembly);

        public static void RegisterProjectionTypeBaseTypeMapping(IDictionary<string, string> typeNameToBaseTypeNameMapping) => TypeNameSupport.RegisterProjectionTypeBaseTypeMapping(typeNameToBaseTypeNameMapping);

        public static void RegisterAuthoringMetadataTypeLookup(Func<Type, Type> authoringMetadataTypeLookup) => TypeExtensions.RegisterAuthoringMetadataTypeLookup(authoringMetadataTypeLookup);

        public static void RegisterHelperType(
            Type type,
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods |
                                        DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type helperType) => TypeExtensions.HelperTypeCache.TryAdd(type, helperType);

        internal static List<ComInterfaceEntry> GetInterfaceTableEntries(Type type)
        {
            var entries = new List<ComInterfaceEntry>();
            bool hasUserImplementedIMarshalInterface = false;
            bool hasUserImplementedICustomPropertyProviderInterface = false;
            bool hasWinrtExposedClassAttribute = false;

#if NET
            // Check whether the type itself has the WinRTTypeExposed attribute and if so
            // use the new source generator approach.
            var winrtExposedClassAttribute = type.GetCustomAttribute<WinRTExposedTypeAttribute>(false);

            // Handle scenario where it can be an authored type
            // which means the attribute lives on the authoring metadata type.
            if (winrtExposedClassAttribute == null)
            {
                // Using GetCCWType rather than GetRuntimeClassCCWType given we want to handle boxed value types.
                var authoringMetadaType = type.GetCCWType();
                if (authoringMetadaType != null)
                {
                    winrtExposedClassAttribute = authoringMetadaType.GetCustomAttribute<WinRTExposedTypeAttribute>(false);
                }
            }

            if (winrtExposedClassAttribute != null)
            {
                hasWinrtExposedClassAttribute = true;
                entries.AddRange(winrtExposedClassAttribute.GetExposedInterfaces());
                if (type.IsClass)
                {
                    // Manual helper to save binary size (no LINQ, no lambdas) and get better performance
                    static bool GetHasCustomIMarshalInterface(List<ComInterfaceEntry> entries)
                    {
                        foreach (ref readonly ComInterfaceEntry entry in CollectionsMarshal.AsSpan(entries))
                        {
                            if (entry.IID == IID.IID_IMarshal)
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    // Same as above, for 'ICustomPropertyProvider' (separate method for a small perf boost).
                    // The method is very tiny, so the code duplication is not really a concern here.
                    static bool GetHasICustomPropertyProviderInterface(List<ComInterfaceEntry> entries)
                    {
                        foreach (ref readonly ComInterfaceEntry entry in CollectionsMarshal.AsSpan(entries))
                        {
                            if (entry.IID == IID.IID_ICustomPropertyProvider)
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    hasUserImplementedIMarshalInterface = GetHasCustomIMarshalInterface(entries);
                    hasUserImplementedICustomPropertyProviderInterface = GetHasICustomPropertyProviderInterface(entries);
                }
            }
            else if (type == typeof(global::System.EventHandler))
            {
                hasWinrtExposedClassAttribute = true;
                entries.AddRange(Projections.GetAbiEventHandlerExposedInterfaces());
            }
            else if (type == typeof(global::System.ComponentModel.PropertyChangedEventHandler))
            {
                hasWinrtExposedClassAttribute = true;
                entries.AddRange(Projections.GetAbiPropertyChangedEventHandlerExposedInterfaces());
            }
            else if (type == typeof(global::System.Collections.Specialized.NotifyCollectionChangedEventHandler))
            {
                hasWinrtExposedClassAttribute = true;
                entries.AddRange(Projections.GetAbiNotifyCollectionChangedEventHandlerExposedInterfaces());
            }
            else if (ComInterfaceEntriesForType.TryGetValue(type, out var registeredEntries))
            {
                hasWinrtExposedClassAttribute = true;
                entries.AddRange(registeredEntries);
            }
            else if (!type.IsEnum && GetComInterfaceEntriesForTypeFromLookupTable(type) is var lookupTableEntries && lookupTableEntries != null)
            {
                hasWinrtExposedClassAttribute = true;
                entries.AddRange(lookupTableEntries);
            }
            else if (RuntimeFeature.IsDynamicCodeCompiled)
#endif
            {
                static void AddInterfaceToVtable(
                    Type iface,
                    List<ComInterfaceEntry> entries,
                    ref bool hasUserImplementedIMarshalInterface,
                    ref bool hasUserImplementedICustomPropertyProviderInterface)
                {
                    var interfaceHelperType = iface.FindHelperType();
                    Guid iid = GuidGenerator.GetIID(interfaceHelperType);
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(interfaceHelperType),
                        Vtable = interfaceHelperType.GetAbiToProjectionVftblPtr()
                    });

                    if (!hasUserImplementedIMarshalInterface && iid == IID.IID_IMarshal)
                    {
                        hasUserImplementedIMarshalInterface = true;
                    }

                    if (!hasUserImplementedICustomPropertyProviderInterface && iid == IID.IID_ICustomPropertyProvider)
                    {
                        hasUserImplementedICustomPropertyProviderInterface = true;
                    }
                }

#if NET
                [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Fallback method for JIT environments that is not trim-safe by design.")]
                [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Fallback method for JIT environments that is not trim-safe by design.")]
                [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Fallback method for JIT environments that is not trim-safe by design.")]
                [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Fallback method for JIT environments that is not trim-safe by design.")]
                [MethodImpl(MethodImplOptions.NoInlining)]
#endif
                static void GetInterfaceTableEntriesForJitEnvironment(
                    Type type,
                    List<ComInterfaceEntry> entries,
                    ref bool hasUserImplementedIMarshalInterface,
                    ref bool hasUserImplementedICustomPropertyProviderInterface)
                {
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
                                AddInterfaceToVtable(iface, entries, ref hasUserImplementedIMarshalInterface, ref hasUserImplementedICustomPropertyProviderInterface);
                            }

                            if (iface.IsConstructedGenericType
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                                && Projections.TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, null, out var compatibleIfaces))
#pragma warning restore IL3050
                            {
                                foreach (var compatibleIface in compatibleIfaces)
                                {
                                    AddInterfaceToVtable(compatibleIface, entries, ref hasUserImplementedIMarshalInterface, ref hasUserImplementedICustomPropertyProviderInterface);
                                }
                            }
                        }
                    }
                }

                GetInterfaceTableEntriesForJitEnvironment(type, entries, ref hasUserImplementedIMarshalInterface, ref hasUserImplementedICustomPropertyProviderInterface);
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
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                if (KeyValuePairHelper.KeyValuePairCCW.TryGetValue(type, out var entry))
                {
                    entries.Add(entry);
                }
                else
                {
                    var ifaceAbiType = type.FindHelperType();
                    entries.Add(new ComInterfaceEntry
                    {
                        IID = GuidGenerator.GetIID(ifaceAbiType),
                        Vtable = (IntPtr)ifaceAbiType.GetAbiToProjectionVftblPtr()
                    });
                }
            }
            else if (!hasWinrtExposedClassAttribute)
            {
                // Splitting this check to ensure the linker can recognize the pattern correctly.
                // See: https://github.com/dotnet/runtime/blob/main/docs/design/tools/illink/feature-checks.md.
                if (type.ShouldProvideIReference())
                {
                    entries.Add(IPropertyValueEntry);
                    entries.Add(ProvideIReference(type));
                }
            }
            
            entries.Add(new ComInterfaceEntry
            {
                IID = IID.IID_IStringable,
                Vtable = ManagedIStringableVftbl.AbiToProjectionVftablePtr
            });

            // There are two scenarios where we want to support 'ICustomPropertyProvider':
            //   - The user is explicitly implementing the interface on their type
            //   - The user is using '[GeneratedBindableCustomProperty]', which uses our internal CCW
            if (FeatureSwitches.EnableICustomPropertyProviderSupport && !hasUserImplementedICustomPropertyProviderInterface)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = IID.IID_ICustomPropertyProvider,
                    Vtable = ManagedCustomPropertyProviderVftbl.AbiToProjectionVftablePtr
                });
            }

            entries.Add(new ComInterfaceEntry
            {
                IID = IID.IID_IWeakReferenceSource,
                Vtable = ABI.WinRT.Interop.IWeakReferenceSource.AbiToProjectionVftablePtr
            });

            // Add IMarhal implemented using the free threaded marshaler
            // to all CCWs if it doesn't already have its own.
            if (!hasUserImplementedIMarshalInterface)
            {
                entries.Add(new ComInterfaceEntry
                {
                    IID = IID.IID_IMarshal,
                    Vtable = ABI.WinRT.Interop.IMarshal.Vftbl.AbiToProjectionVftablePtr
                });
            }

            // Add IAgileObject to all CCWs
            entries.Add(new ComInterfaceEntry
            {
                IID = IID.IID_IAgileObject,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });

            entries.Add(new ComInterfaceEntry
            {
                IID = IID.IID_IInspectable,
                Vtable = IInspectable.Vftbl.AbiToProjectionVftablePtr
            });

            // This should be the last entry as it is included / excluded based on the flags.
            entries.Add(new ComInterfaceEntry
            {
                IID = IID.IID_IUnknown,
                Vtable = IUnknownVftbl.AbiToProjectionVftblPtr
            });

            return entries;
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
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

        internal static Func<IntPtr, object> GetOrCreateDelegateFactory(Type type)
        {
            return DelegateFactoryCache.GetOrAdd(type, CreateDelegateFactory);
        }

#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2067",
            Justification = "The type is a delegate type, so 'GuidGenerator.GetIID' doesn't need to access public fields from it (it uses the helper type).")]
#endif
        private static Func<IntPtr, object> CreateDelegateFactory(Type type)
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
        }

        public static bool RegisterDelegateFactory(Type implementationType, Func<IntPtr, object> delegateFactory) => DelegateFactoryCache.TryAdd(implementationType, delegateFactory);

        internal static Func<IInspectable, object> CreateNullableTFactory(Type implementationType)
        {
            var getValueMethod = implementationType.GetHelperType().GetMethod("GetValue", BindingFlags.Static | BindingFlags.Public);
            return (IInspectable obj) => getValueMethod.Invoke(null, new[] { obj });
        }

        internal static Func<IInspectable, object> CreateAbiNullableTFactory(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
            Type implementationType)
        {
            // This method is only called when 'implementationType' has been validated to be some ABI.System.Nullable_Delegate<T>.
            // As such, we know that the type definitely has a method with signature 'static Nullable GetValue(IInspectable)'.
            var getValueMethod = implementationType.GetMethod("GetValue", BindingFlags.Static | BindingFlags.Public);

            return (Func<IInspectable, object>)getValueMethod.CreateDelegate(typeof(Func<IInspectable, object>));
        }

        private static Func<IInspectable, object> CreateArrayFactory(Type implementationType)
        {
            // This method is only called when 'implementationType' is some 'Windows.Foundation.IReferenceArray<T>' type.
            // That interface is only implemented by 'ABI.Windows.Foundation.IReferenceArray<T>', and the method is public.
            var getValueMethod = implementationType.GetHelperType().GetMethod("GetValue", BindingFlags.Static | BindingFlags.Public);

            return (Func<IInspectable, object>)getValueMethod.CreateDelegate(typeof(Func<IInspectable, object>));
        }

        // This is used to hold the reference to the native value type object (IReference) until the actual value in it (boxed as an object) gets cleaned up by GC
        // This is done to avoid pointer reuse until GC cleans up the boxed object
        internal static readonly ConditionalWeakTable<object, IInspectable> BoxedValueReferenceCache = new();

        internal static Func<IInspectable, object> CreateReferenceCachingFactory(Func<IInspectable, object> internalFactory)
        {
            return internalFactory.InvokeWithBoxedValueReferenceCacheInsertion;
        }

        private static Func<IInspectable, object> CreateCustomTypeMappingFactory(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
            Type customTypeHelperType)
        {
            var fromAbiMethod = customTypeHelperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.Static);
            if (fromAbiMethod is null)
            {
                throw new MissingMethodException();
            }

            var fromAbiMethodFunc = (Func<IntPtr, object>) fromAbiMethod.CreateDelegate(typeof(Func<IntPtr, object>));
            return (IInspectable obj) =>
            {
                var fromAbiMethod = fromAbiMethodFunc(obj.ThisPtr);
                GC.KeepAlive(obj);
                return fromAbiMethod;
            };
        }

        internal static Func<IInspectable, object> CreateTypedRcwFactory(Type implementationType)
        {
            return CreateTypedRcwFactory(implementationType, null);
        }

        internal static Func<IInspectable, object> CreateTypedRcwFactory(Type implementationType, string runtimeClassName)
        {
            // If runtime class name is empty or "Object", then just use IInspectable.
            if (implementationType == null || implementationType == typeof(object))
            {
                // If we reach here, then we couldn't find a type that matches the runtime class name.
                // Fall back to using IInspectable directly.
                return (IInspectable obj) => obj;
            }

            if (implementationType == typeof(string) || 
                implementationType == typeof(Type) ||
                implementationType == typeof(Exception) || 
                implementationType.IsDelegate())
            {
                return ABI.System.NullableType.GetValueFactory(implementationType);
            }

            if (implementationType.IsValueType)
            {
                if (implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
                {
                    return CreateReferenceCachingFactory(CreateKeyValuePairFactory(implementationType));
                }
                else if (implementationType.IsNullableT())
                {
                    return ABI.System.NullableType.GetValueFactory(implementationType.GetGenericArguments()[0]);
                }
                else
                {
                    return ABI.System.NullableType.GetValueFactory(implementationType);
                }
            }

            var customHelperType = Projections.FindCustomHelperTypeMapping(implementationType, true);
            if (customHelperType != null)
            {
                return CreateReferenceCachingFactory(CreateCustomTypeMappingFactory(customHelperType));
            }

            if (implementationType.IsIReferenceArray())
            {
                return CreateReferenceCachingFactory(CreateArrayFactory(implementationType));
            }

            return CreateFactoryForImplementationType(runtimeClassName, implementationType);
        }

        internal static Type GetRuntimeClassForTypeCreation(IInspectable inspectable, Type staticallyDeterminedType)
        {
            string runtimeClassName = inspectable.GetRuntimeClassName(noThrow: true);
            Type implementationType = null;
            if (!string.IsNullOrEmpty(runtimeClassName))
            {
                // Check if this is a nullable type where there are no references to the nullable version, but
                // there is to the actual type.
                if (runtimeClassName.StartsWith("Windows.Foundation.IReference`1<", StringComparison.Ordinal))
                {
                    // runtimeClassName is of format Windows.Foundation.IReference`1<type>.
                    return TypeNameSupport.FindRcwTypeByNameCached(runtimeClassName.Substring(32, runtimeClassName.Length - 33));
                }

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
                     staticallyDeterminedType.IsAssignableFrom(implementationType))))
                {
                    return staticallyDeterminedType;
                }
            }

#if NET
            // On AOT, RCW type factories for generic interfaces are registered by the projection before it asks to create
            // an RCW for it.  But we can have scenarios where the actual implementation type is different than the statically
            // known type and the implementation type's RCW factory hasn't been initialized.  i.e. IList<object> and IEnumerable<object>
            // where the latter is the statically known type whose RCW factory is added already but IList<object> hasn't been added.
            // In this case, since the caller just needs a IEnumerable<object>, we fallback to the statically known type if IList
            // isn't already added.  On JIT, we can construct it at runtime.
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                if (implementationType != null &&
                    implementationType.IsInterface && 
                    implementationType.IsGenericType &&
                    !TypedObjectFactoryCacheForType.ContainsKey(implementationType))
                {
                    return staticallyDeterminedType;
                }
            }
#endif

            return implementationType;
        }

        private static ComInterfaceEntry IPropertyValueEntry
        {
            get
            {
                if (!FeatureSwitches.EnableIReferenceSupport)
                {
                    throw new NotSupportedException("Support for 'IPropertyValue' is not enabled (it depends on the support for 'IReference<T>').");
                }

                return new ComInterfaceEntry
                {
                    IID = IID.IID_IPropertyValue,
                    Vtable = ManagedIPropertyValueImpl.AbiToProjectionVftablePtr
                };
            }
        }

        private static ComInterfaceEntry ProvideIReference(Type type)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReference<T>' is not enabled.");
            }

            if (type == typeof(int))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableInt,
                    Vtable = ABI.System.Nullable_int.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(string))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableString,
                    Vtable = ABI.System.Nullable_string.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(byte))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableByte,
                    Vtable = ABI.System.Nullable_byte.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(short))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableShort,
                    Vtable = ABI.System.Nullable_short.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ushort))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableUShort,
                    Vtable = ABI.System.Nullable_ushort.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(uint))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableUInt,
                    Vtable = ABI.System.Nullable_uint.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(long))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableLong,
                    Vtable = ABI.System.Nullable_long.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ulong))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableULong,
                    Vtable = ABI.System.Nullable_ulong.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(float))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableFloat,
                    Vtable = ABI.System.Nullable_float.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(double))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableDouble,
                    Vtable = ABI.System.Nullable_double.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(char))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableChar,
                    Vtable = ABI.System.Nullable_char.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(bool))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableBool,
                    Vtable = ABI.System.Nullable_bool.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(Guid))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableGuid,
                    Vtable = ABI.System.Nullable_guid.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(DateTimeOffset))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableDateTimeOffset,
                    Vtable = ABI.System.Nullable_DateTimeOffset.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(TimeSpan))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableTimeSpan,
                    Vtable = ABI.System.Nullable_TimeSpan.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(object))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableObject,
                    Vtable = ABI.System.Nullable_Object.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type.IsTypeOfType())
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableType,
                    Vtable = ABI.System.Nullable_Type.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(sbyte))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableSByte,
                    Vtable = ABI.System.Nullable_sbyte.Vftbl.AbiToProjectionVftablePtr
                };
            }
            if (type.IsEnum)
            {
                if (type.IsDefined(typeof(FlagsAttribute)))
                {
                    return new ComInterfaceEntry
                    {
                        IID = ABI.System.Nullable_FlagsEnum.GetIID(type),
                        Vtable = ABI.System.Nullable_FlagsEnum.AbiToProjectionVftablePtr
                    };
                }
                else
                {
                    return new ComInterfaceEntry
                    {
                        IID = ABI.System.Nullable_IntEnum.GetIID(type),
                        Vtable = ABI.System.Nullable_IntEnum.AbiToProjectionVftablePtr
                    };
                }
            }
            if (type == typeof(EventHandler))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableEventHandler,
                    Vtable = ABI.System.Nullable_EventHandler.AbiToProjectionVftablePtr
                };
            }
            if (type.IsDelegate())
            {
#if NET
                if (!RuntimeFeature.IsDynamicCodeCompiled)
                {
                    throw new NotSupportedException($"Cannot provide IReference`1 support for delegate type '{type}'.");
                }
#endif

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                var delegateHelperType = typeof(ABI.System.Nullable_Delegate<>).MakeGenericType(type);
                return new ComInterfaceEntry
                {
                    IID = global::WinRT.GuidGenerator.GetIID(delegateHelperType),
                    Vtable = delegateHelperType.GetAbiToProjectionVftblPtr()
                };
#pragma warning restore IL3050
            }
            if (type == typeof(System.Numerics.Matrix3x2))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceMatrix3x2,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Matrix3x2, System.Numerics.Matrix3x2>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Matrix4x4))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceMatrix4x4,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Matrix4x4, System.Numerics.Matrix4x4>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Plane))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferencePlane,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Plane, System.Numerics.Plane>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Quaternion))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceQuaternion,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Quaternion, System.Numerics.Quaternion>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Vector2))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceVector2,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Vector2, System.Numerics.Vector2>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Vector3))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceVector3,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Vector3, System.Numerics.Vector3>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Vector4))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceVector4,
                    Vtable = BoxedValueIReferenceImpl<System.Numerics.Vector4, System.Numerics.Vector4>.AbiToProjectionVftablePtr
                };
            }
            if (type.IsTypeOfException())
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_NullableException,
                    Vtable = ABI.System.Nullable_Exception.Vftbl.AbiToProjectionVftablePtr
                };
            }

#if NET
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Cannot provide IReference`1 support for type '{type}'.");
            }
#endif

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            return new ComInterfaceEntry
            {
                IID = global::WinRT.GuidGenerator.GetIID(typeof(ABI.System.Nullable<>).MakeGenericType(type)),
                Vtable = typeof(BoxedValueIReferenceImpl<>).MakeGenericType(type).GetAbiToProjectionVftblPtr()
            };
#pragma warning restore IL3050
        }

        private static ComInterfaceEntry ProvideIReferenceArray(Type arrayType)
        {
            if (!FeatureSwitches.EnableIReferenceSupport)
            {
                throw new NotSupportedException("Support for 'IReferenceArray<T>' is not enabled.");
            }

            Type type = arrayType.GetElementType();
            if (type == typeof(int))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfInt32,
                    Vtable = BoxedArrayIReferenceArrayImpl<int>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(string))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfString,
                    Vtable = BoxedArrayIReferenceArrayImpl<string>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(byte))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfByte,
                    Vtable = BoxedArrayIReferenceArrayImpl<byte>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(short))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfInt16,
                    Vtable = BoxedArrayIReferenceArrayImpl<short>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ushort))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfUInt16,
                    Vtable = BoxedArrayIReferenceArrayImpl<ushort>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(uint))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfUInt32,
                    Vtable = BoxedArrayIReferenceArrayImpl<uint>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(long))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfInt64,
                    Vtable = BoxedArrayIReferenceArrayImpl<long>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(ulong))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfUInt64,
                    Vtable = BoxedArrayIReferenceArrayImpl<ulong>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(float))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfSingle,
                    Vtable = BoxedArrayIReferenceArrayImpl<float>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(double))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfDouble,
                    Vtable = BoxedArrayIReferenceArrayImpl<double>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(char))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfChar,
                    Vtable = BoxedArrayIReferenceArrayImpl<char>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(bool))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfBoolean,
                    Vtable = BoxedArrayIReferenceArrayImpl<bool>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(Guid))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfGuid,
                    Vtable = BoxedArrayIReferenceArrayImpl<Guid>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(DateTimeOffset))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfDateTimeOffset,
                    Vtable = BoxedArrayIReferenceArrayImpl<DateTimeOffset>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(TimeSpan))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfTimeSpan,
                    Vtable = BoxedArrayIReferenceArrayImpl<TimeSpan>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(object))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfObject,
                    Vtable = BoxedArrayIReferenceArrayImpl<object>.AbiToProjectionVftablePtr
                };
            }
            if (type.IsTypeOfType())
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfType,
                    Vtable = BoxedArrayIReferenceArrayImpl<Type>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Matrix3x2))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfMatrix3x2,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Matrix3x2>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Matrix4x4))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfMatrix4x4,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Matrix4x4>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Plane))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfPlane,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Plane>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Quaternion))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfQuaternion,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Quaternion>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Vector2))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfVector2,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Vector2>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Vector3))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfVector3,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Vector3>.AbiToProjectionVftablePtr
                };
            }
            if (type == typeof(System.Numerics.Vector4))
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfVector4,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Numerics.Vector4>.AbiToProjectionVftablePtr
                };
            }
            if (type.IsTypeOfException())
            {
                return new ComInterfaceEntry
                {
                    IID = IID.IID_IReferenceArrayOfException,
                    Vtable = BoxedArrayIReferenceArrayImpl<System.Exception>.AbiToProjectionVftablePtr
                };
            }

#if NET
            if (!RuntimeFeature.IsDynamicCodeCompiled)
            {
                throw new NotSupportedException($"Cannot provide IReferenceArray`1 support for element type '{type}'.");
            }
#endif

#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
            return new ComInterfaceEntry
            {
                IID = global::WinRT.GuidGenerator.GetIID(typeof(IReferenceArray<>).MakeGenericType(type)),
                Vtable = (IntPtr)typeof(BoxedArrayIReferenceArrayImpl<>).MakeGenericType(type).GetAbiToProjectionVftblPtr()
            };
#pragma warning restore IL3050
        }

        internal sealed class InspectableInfo
        {
            private readonly Type _type;

            public Guid[] IIDs { get; }

            private volatile string _runtimeClassName;

            private string MakeRuntimeClassName()
            {
                global::System.Threading.Interlocked.CompareExchange(ref _runtimeClassName, TypeNameSupport.GetNameForType(_type, TypeNameGenerationFlags.GenerateBoxedName | TypeNameGenerationFlags.ForGetRuntimeClassName), null);
                return _runtimeClassName;
            }

            public string RuntimeClassName => _runtimeClassName ?? MakeRuntimeClassName();

            internal InspectableInfo(Type type, Guid[] iids)
            {
                _type = type;
                IIDs = iids;
            }
        }

        internal static ObjectReference<T> CreateCCWForObject<T>(object obj, Guid iid)
        {
            IntPtr ccw = CreateCCWForObjectForABI(obj, iid);
            return ObjectReference<T>.Attach(ref ccw, iid);
        }

        internal static ObjectReferenceValue CreateCCWForObjectForMarshaling(object obj, Guid iid)
        {
            IntPtr ccw = CreateCCWForObjectForABI(obj, iid);
            return new ObjectReferenceValue(ccw);
        }
    }
}