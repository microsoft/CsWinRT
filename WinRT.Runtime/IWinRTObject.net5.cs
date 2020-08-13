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
            Type type = Type.GetTypeFromHandle(interfaceType);
            Type helperType = type.FindHelperType();
            if (helperType is null)
            {
                return false;
            }
            var vftblType = helperType.GetNestedType("Vftbl");
            int hr = NativeObject.TryAs<IUnknownVftbl>(GuidGenerator.GetIID(helperType), out var objRef);
            if (hr < 0)
            {
                if (throwIfNotImplemented)
                {
                    ExceptionHelpers.ThrowExceptionForHR(hr);
                }
                return false;
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
            return Type.GetTypeFromHandle(interfaceType).GetHelperType().TypeHandle;
        }

        protected IObjectReference NativeObject { get; }

        protected ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> QueryInterfaceCache { get; }

        IObjectReference GetObjectReferenceForType(RuntimeTypeHandle type)
        {
            return QueryInterfaceCache[type];
        }
    }
}