// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace WinRT;

#if EMBED
internal
#else
public
#endif
partial class IInspectable : IWinRTObject
{
    IObjectReference IWinRTObject.NativeObject => _obj;
    bool IWinRTObject.HasUnwrappableNativeObject => true;
   
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
}
