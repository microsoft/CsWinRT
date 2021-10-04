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

        ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache { get; } = new();
        ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData { get; } = new();
    }

}
