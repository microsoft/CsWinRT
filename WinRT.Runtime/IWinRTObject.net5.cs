using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    public interface IWinRTObject : IDynamicInterfaceCastable
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
                        return true;
                    }
                }
                Type iReadOnlyList = typeof(IReadOnlyList<>).MakeGenericType(new[] { itemType });
                if (IsInterfaceImplemented(iReadOnlyList.TypeHandle, throwIfNotImplemented))
                {
                    return true;
                }
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
            var vftblType = helperType.GetNestedType("Vftbl");
            if (vftblType is null)
            {
                // The helper type might not have a vftbl type if it was linked away.
                // The only time the Vftbl type would be linked away is when we don't actually use
                // any of the methods on the interface (it was just a type cast/"is Type" check).
                // In that case, we can use the IUnknownVftbl-typed ObjectReference since
                // it has all of the information we'll need.
                if (!QueryInterfaceCache.TryAdd(interfaceType, objRef))
                {
                    objRef.Dispose();
                }
                return true;
            }
            if (vftblType.IsGenericTypeDefinition)
            {
                vftblType = vftblType.MakeGenericType(Type.GetTypeFromHandle(interfaceType).GetGenericArguments());
            }
            using (objRef)
            {
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

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IReadOnlyCollection<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    Type iReadOnlyDictionary = typeof(IReadOnlyDictionary<,>).MakeGenericType(itemType.GetGenericArguments());
                    
                    if (IsInterfaceImplemented(iReadOnlyDictionary.TypeHandle, false))
                    {
                        return GetInterfaceImplementation(iReadOnlyDictionary.TypeHandle);
                    }
                }
                Type iReadOnlyList = typeof(IReadOnlyList<>).MakeGenericType(new[] { itemType });
                if (IsInterfaceImplemented(iReadOnlyList.TypeHandle, false))
                {
                    return GetInterfaceImplementation(iReadOnlyList.TypeHandle);
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    Type iDictionary = typeof(IDictionary<,>).MakeGenericType(itemType.GetGenericArguments());
                    if (IsInterfaceImplemented(iDictionary.TypeHandle, false))
                    {
                        return GetInterfaceImplementation(iDictionary.TypeHandle);
                    }
                }
                Type iList = typeof(IList<>).MakeGenericType(new[] { itemType });
                if (IsInterfaceImplemented(iList.TypeHandle, false))
                {
                    return GetInterfaceImplementation(iList.TypeHandle);
                }
            }

            var helperType = type.GetHelperType();
            if (helperType.IsInterface)
                return helperType.TypeHandle;
            return default;
        }

        IObjectReference NativeObject { get; }

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

        ConcurrentDictionary<RuntimeTypeHandle, object> AdditionalTypeData => new ConcurrentDictionary<RuntimeTypeHandle, object>();

        object GetOrCreateTypeHelperData(RuntimeTypeHandle type, Func<object> helperDataFactory)
        {
            return AdditionalTypeData.GetOrAdd(type, (type) => helperDataFactory());
        }
    }
}