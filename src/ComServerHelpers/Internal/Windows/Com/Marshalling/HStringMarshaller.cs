using System;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.System.WinRT;

namespace ComServerHelpers.Internal.Windows.Com.Marshalling;

[CustomMarshaller(typeof(HSTRING), MarshalMode.UnmanagedToManagedOut, typeof(HStringMarshaller))]
[CustomMarshaller(typeof(HSTRING), MarshalMode.UnmanagedToManagedIn, typeof(HStringMarshaller))]
[CustomMarshaller(typeof(HSTRING), MarshalMode.ManagedToUnmanagedOut, typeof(HStringMarshaller))]
[CustomMarshaller(typeof(HSTRING), MarshalMode.ManagedToUnmanagedIn, typeof(HStringMarshaller))]
internal static class HStringMarshaller
{
    public static HSTRING ConvertToManaged(nint nativeValue) => new HSTRING(nativeValue);

    public static nint ConvertToUnmanaged(HSTRING value) => value.Value;
}
