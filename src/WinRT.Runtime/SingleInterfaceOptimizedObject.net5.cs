// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    sealed class SingleInterfaceOptimizedObject : IWinRTObject, IDynamicInterfaceCastable
    {
        private readonly Type _type;
        private readonly IObjectReference _obj;

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

                if (RuntimeFeature.IsDynamicCodeCompiled)
                {
                    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "If the 'Vftbl' type is kept, we can assume all its metadata will also have been rooted.")]
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    static IObjectReference TryGetObjectReferenceViaVftbl(IObjectReference objRef, Type helperType)
                    {
                        var vftblType = helperType.FindVftblType();

                        if (vftblType is not null)
                        {
#pragma warning disable IL3050 // https://github.com/dotnet/runtime/issues/97273
                            return (IObjectReference)typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(vftblType).Invoke(objRef, null);
#pragma warning restore IL3050
                        }

                        return null;
                    }

                    IObjectReference objRefViaVftbl = TryGetObjectReferenceViaVftbl(objRef, helperType);

                    if (objRefViaVftbl is not null)
                    {
                        _obj = objRefViaVftbl;

                        return;
                    }
                }

                _obj = objRef.As<IUnknownVftbl>(GuidGenerator.GetIID(helperType));

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

            if (!FeatureSwitches.EnableIDynamicInterfaceCastableSupport)
            {
                return false;
            }

            return ((IWinRTObject)this).IsInterfaceImplementedFallback(interfaceType, throwIfNotImplemented);
        }

        IObjectReference IWinRTObject.GetObjectReferenceForType(RuntimeTypeHandle interfaceType)
        {
            if (_type.Equals(Type.GetTypeFromHandle(interfaceType)))
            {
                return _obj;
            }
            return ((IWinRTObject)this).GetObjectReferenceForTypeFallback(interfaceType);
        }

    }
}
