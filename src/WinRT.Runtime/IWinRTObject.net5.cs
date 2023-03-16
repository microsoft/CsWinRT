// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else
    public
#endif
    interface IWinRTObject : IDynamicInterfaceCastable
    {
        bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            return IsInterfaceImplementedFallback(interfaceType, throwIfNotImplemented);
        }

        bool IsInterfaceImplementedFallback(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (QueryInterfaceCache.ContainsKey(interfaceType))
            {
                return true;
            }
            Type type = Type.GetTypeFromHandle(interfaceType);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IReadOnlyCollection<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    Type iReadOnlyDictionary = typeof(IReadOnlyDictionary<,>).MakeGenericType(itemType.GetGenericArguments());
                    if (IsInterfaceImplemented(iReadOnlyDictionary.TypeHandle, false))
                    {
                        if (QueryInterfaceCache.TryGetValue(iReadOnlyDictionary.TypeHandle, out var typedObjRef) && !QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                        {
                            typedObjRef.Dispose();
                        }
                        return true;
                    }
                }
                Type iReadOnlyList = typeof(IReadOnlyList<>).MakeGenericType(new[] { itemType });
                if (IsInterfaceImplemented(iReadOnlyList.TypeHandle, throwIfNotImplemented))
                {
                    if (QueryInterfaceCache.TryGetValue(iReadOnlyList.TypeHandle, out var typedObjRef) && !QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                    {
                        typedObjRef.Dispose();
                    }
                    return true;
                }

                return false;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    Type iDictionary = typeof(IDictionary<,>).MakeGenericType(itemType.GetGenericArguments());
                    if (IsInterfaceImplemented(iDictionary.TypeHandle, false))
                    {
                        if (QueryInterfaceCache.TryGetValue(iDictionary.TypeHandle, out var typedObjRef) && !QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                        {
                            typedObjRef.Dispose();
                        }
                        return true;
                    }
                }
                Type iList = typeof(IList<>).MakeGenericType(new[] { itemType });
                if (IsInterfaceImplemented(iList.TypeHandle, throwIfNotImplemented))
                {
                    if (QueryInterfaceCache.TryGetValue(iList.TypeHandle, out var typedObjRef) && !QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                    {
                        typedObjRef.Dispose();
                    }
                    return true;
                }

                return false;
            }
            else if (type == typeof(System.Collections.IEnumerable))
            {
                Type iEnum = typeof(System.Collections.Generic.IEnumerable<object>);
                if (IsInterfaceImplemented(iEnum.TypeHandle, false))
                {
                    if (QueryInterfaceCache.TryGetValue(iEnum.TypeHandle, out var typedObjRef) && !QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                    {
                        typedObjRef.Dispose();
                    }
                    return true;
                }
            }
            else if (type == typeof(System.Collections.ICollection))
            {
                Type iList = typeof(global::System.Collections.IList);
                if (IsInterfaceImplemented(iList.TypeHandle, false))
                {
                    if (QueryInterfaceCache.TryGetValue(iList.TypeHandle, out var typedObjRef) && !QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                    {
                        typedObjRef.Dispose();
                    }
                    return true;
                }
            }

            Type helperType = type.FindHelperType();
            if (helperType is null || !helperType.IsInterface)
            {
                return false;
            }
            int hr = NativeObject.TryAs<IUnknownVftbl>(GuidGenerator.GetIID(helperType), out var objRef);
            if (hr < 0)
            {
                if (throwIfNotImplemented)
                {
                    ExceptionHelpers.ThrowExceptionForHR(hr);
                }
                return false;
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                RuntimeTypeHandle projectIEnum = typeof(System.Collections.IEnumerable).TypeHandle;
                AdditionalTypeData.GetOrAdd(projectIEnum, (_) => new ABI.System.Collections.IEnumerable.AdaptiveFromAbiHelper(type, this));
            }

            var vftblType = helperType.FindVftblType();
            using (objRef)
            {
                if (vftblType is null)
                {
                    var qiObjRef = objRef.As<IUnknownVftbl>(GuidGenerator.GetIID(helperType));
                    if (!QueryInterfaceCache.TryAdd(interfaceType, qiObjRef))
                    {
                        qiObjRef.Dispose();
                    }
                    return true;
                }
                IObjectReference typedObjRef = (IObjectReference)typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(vftblType).Invoke(objRef, null);
                if (!QueryInterfaceCache.TryAdd(interfaceType, typedObjRef))
                {
                    typedObjRef.Dispose();
                }
                return true;
            }
        }

        RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
        {
            var type = Type.GetTypeFromHandle(interfaceType);
            var helperType = type.GetHelperType();
            if (helperType.IsInterface)
                return helperType.TypeHandle;
            return default;
        }

        IObjectReference NativeObject { get; }
        bool HasUnwrappableNativeObject { get; }

        protected ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> QueryInterfaceCache { get; }

        IObjectReference GetObjectReferenceForType(RuntimeTypeHandle type)
        {
            return GetObjectReferenceForTypeFallback(type);
        }

        IObjectReference GetObjectReferenceForTypeFallback(RuntimeTypeHandle type)
        {
            if (IsInterfaceImplemented(type, true))
            {
                return QueryInterfaceCache[type];
            }
            throw new Exception("Interface " + Type.GetTypeFromHandle(type) +" is not implemented.");
        }

        ConcurrentDictionary<RuntimeTypeHandle, object> AdditionalTypeData { get; }

        object GetOrCreateTypeHelperData(RuntimeTypeHandle type, Func<object> helperDataFactory)
        {
            return AdditionalTypeData.GetOrAdd(type, (type) => helperDataFactory());
        }

        internal void Resurrect()
        {
            if (NativeObject.Resurrect())
            {
                foreach (var cached in QueryInterfaceCache)
                {
                    cached.Value.Resurrect();
                }

                // Delegates store their agile reference as an additional type data.
                // These should be recreated when instances are resurrect.
                if (AdditionalTypeData.TryGetValue(typeof(AgileReference).TypeHandle, out var agileObj))
                {
                    AdditionalTypeData.TryUpdate(typeof(AgileReference).TypeHandle, new AgileReference(NativeObject), agileObj);
                }
            }
        }
    }
}