using System;
using System.Collections.Concurrent;

namespace WinRT
{
    public partial class IInspectable : IWinRTObject
    {
        IObjectReference IWinRTObject.NativeObject => _obj;
        bool IWinRTObject.HasUnwrappableNativeObject => true;
       
        private volatile ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _QueryInterfaceCache = null;
        private ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
        {
            System.Threading.Interlocked.CompareExchange(ref _QueryInterfaceCache, new ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null); 
            return _QueryInterfaceCache;
        }
        ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _QueryInterfaceCache ?? MakeQueryInterfaceCache();

        private volatile ConcurrentDictionary<RuntimeTypeHandle, object> _AdditionalTypeData = null;
        private ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
        {
            System.Threading.Interlocked.CompareExchange(ref _AdditionalTypeData, new ConcurrentDictionary<RuntimeTypeHandle, object>(), null); 
            return _AdditionalTypeData;
        }
        ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _AdditionalTypeData ?? MakeAdditionalTypeData();
    }

}
