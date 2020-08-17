using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    public interface IWinRTObject : IDynamicInterfaceCastable
    {
        bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (QueryInterfaceCache.ContainsKey(interfaceType))
            {
                return true;
            }
            Type type = Type.GetTypeFromHandle(interfaceType);
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
            var helperType = Type.GetTypeFromHandle(interfaceType).GetHelperType();
            if (helperType.IsInterface)
                return helperType.TypeHandle;
            return default;
        }

        IObjectReference NativeObject { get; }

        protected ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> QueryInterfaceCache { get; }

        IObjectReference GetObjectReferenceForType(RuntimeTypeHandle type)
        {
            return QueryInterfaceCache[type];
        }
    }
}