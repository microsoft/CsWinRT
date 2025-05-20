using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.System.WinRT;

namespace ComServerHelpers.Internal.Windows.Com.Marshalling;

[CustomMarshaller(typeof(TrustLevel), MarshalMode.UnmanagedToManagedOut, typeof(TrustLevelMarshaller))]
[CustomMarshaller(typeof(TrustLevel), MarshalMode.UnmanagedToManagedIn, typeof(TrustLevelMarshaller))]
[CustomMarshaller(typeof(TrustLevel), MarshalMode.ManagedToUnmanagedOut, typeof(TrustLevelMarshaller))]
[CustomMarshaller(typeof(TrustLevel), MarshalMode.ManagedToUnmanagedIn, typeof(TrustLevelMarshaller))]
internal static class TrustLevelMarshaller
{
    public static TrustLevel ConvertToManaged(int nativeValue) => (TrustLevel)nativeValue;

    public static int ConvertToUnmanaged(TrustLevel value) => (int)value;
}
