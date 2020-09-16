using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT
{
    public class SingleInterfaceOptimizedObject : IWinRTObject, IDynamicInterfaceCastable
    {
        private Type _type;
        private IObjectReference _obj;

        public SingleInterfaceOptimizedObject(Type type, IObjectReference objRef)
        {
            this._type = type;
            Type helperType = type.FindHelperType();
            var vftblType = helperType.GetNestedType("Vftbl");
            if (vftblType is null)
            {
                // The helper type might not have a vftbl type if it was linked away.
                // The only time the Vftbl type would be linked away is when we don't actually use
                // any of the methods on the interface (it was just a type cast/"is Type" check).
                // In that case, we can use the IUnknownVftbl-typed ObjectReference since
                // it has all of the information we'll need.
                _obj = objRef;
            }
            if (vftblType.IsGenericTypeDefinition)
            {
                vftblType = vftblType.MakeGenericType(type.GetGenericArguments());
            }
            this._obj = (IObjectReference)typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(vftblType).Invoke(objRef, null);
        }

        IObjectReference IWinRTObject.NativeObject => _obj;

        ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache { get; } = new();

        bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (_type.Equals(Type.GetTypeFromHandle(interfaceType)))
            {
                return true;
            }
            return (this as IWinRTObject).IsInterfaceImplemented(interfaceType, throwIfNotImplemented);
        }

        RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
        {
            var helperType = Type.GetTypeFromHandle(interfaceType).GetHelperType();
            if (helperType.IsInterface)
                return helperType.TypeHandle;
            return default;
        }

        IObjectReference IWinRTObject.GetObjectReferenceForType(RuntimeTypeHandle interfaceType)
        {
            if (_type.Equals(Type.GetTypeFromHandle(interfaceType)))
            {
                return _obj;
            }
            return (this as IWinRTObject).GetObjectReferenceForType(interfaceType);
        }

    }
}
