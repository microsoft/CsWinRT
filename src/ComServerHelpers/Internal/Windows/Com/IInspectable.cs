using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComServerHelpers.Windows.Com;

[GeneratedComInterface]
[Guid("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90")]
internal unsafe partial interface IInspectable
{
    [PreserveSig]
    global::Windows.Win32.Foundation.HRESULT GetIids(uint* iidCount, Guid** iids);

    [PreserveSig]
    global::Windows.Win32.Foundation.HRESULT GetRuntimeClassName(global::Windows.Win32.System.WinRT.HSTRING* className);

    [PreserveSig]
    global::Windows.Win32.Foundation.HRESULT GetTrustLevel(global::Windows.Win32.System.WinRT.TrustLevel* trustLevel);
}
