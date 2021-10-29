﻿using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    class SingleInterfaceOptimizedObject : IWinRTObject, IDynamicInterfaceCastable
    {
        private Type _type;
        private IObjectReference _obj;

        public SingleInterfaceOptimizedObject(Type type, IObjectReference objRef)
            : this(type, objRef, true)
        {
        }

        internal SingleInterfaceOptimizedObject(Type type, IObjectReference objRef, bool requireQI)
        {
            _type = type;
            if (requireQI)
            {
                Type helperType = type.FindHelperType();
                var vftblType = helperType.FindVftblType();
                if (vftblType is null)
                {
                    _obj = objRef.As<IUnknownVftbl>(GuidGenerator.GetIID(helperType));
                }
                else
                {
                    _obj = (IObjectReference)typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(vftblType).Invoke(objRef, null);
                }
            }
            else 
            {
                _obj = objRef;
            }
        }

        IObjectReference IWinRTObject.NativeObject => _obj;
        bool IWinRTObject.HasUnwrappableNativeObject => false;


        private volatile ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
        private ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
        {
            System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null);
            return _queryInterfaceCache;
        }
        ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();

        private volatile ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
        private ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
        {
            System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new ConcurrentDictionary<RuntimeTypeHandle, object>(), null);
            return _additionalTypeData;
        }
        ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();

        bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (_type.Equals(Type.GetTypeFromHandle(interfaceType)))
            {
                return true;
            }
            return (this as IWinRTObject).IsInterfaceImplementedFallback(interfaceType, throwIfNotImplemented);
        }

        IObjectReference IWinRTObject.GetObjectReferenceForType(RuntimeTypeHandle interfaceType)
        {
            if (_type.Equals(Type.GetTypeFromHandle(interfaceType)))
            {
                return _obj;
            }
            return (this as IWinRTObject).GetObjectReferenceForTypeFallback(interfaceType);
        }

    }
}
