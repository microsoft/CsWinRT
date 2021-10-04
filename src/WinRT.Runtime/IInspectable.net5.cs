using System;
using System.Collections.Concurrent;

namespace WinRT
{
#if EMBED
    internal
#else
    public
#endif
    partial class IInspectable : IWinRTObject
    {
        IObjectReference IWinRTObject.NativeObject => _obj;
        bool IWinRTObject.HasUnwrappableNativeObject => true;

        private Lazy<ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>> _lazyQueryInterfaceCache = new();
        ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _lazyQueryInterfaceCache.Value;
        private Lazy<ConcurrentDictionary<RuntimeTypeHandle, object>> _lazyAdditionalTypeData = new();
        ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _lazyAdditionalTypeData.Value;
    }

}
