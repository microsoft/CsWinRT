using System;
using System.Collections.Concurrent;

namespace WinRT
{
    public partial class IInspectable : IWinRTObject
    {
        IObjectReference IWinRTObject.NativeObject => _obj;

        ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache { get; } = new();
    }

}
