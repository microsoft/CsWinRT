using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace ComServerHelpers.Internal.Windows.Com.Marshalling;

[CustomMarshaller(typeof(BOOL), MarshalMode.UnmanagedToManagedOut, typeof(BoolMarshaller))]
[CustomMarshaller(typeof(BOOL), MarshalMode.UnmanagedToManagedIn, typeof(BoolMarshaller))]
[CustomMarshaller(typeof(BOOL), MarshalMode.ManagedToUnmanagedOut, typeof(BoolMarshaller))]
[CustomMarshaller(typeof(BOOL), MarshalMode.ManagedToUnmanagedIn, typeof(BoolMarshaller))]
internal static class BoolMarshaller
{
    public static BOOL ConvertToManaged(int nativeValue) => new BOOL(nativeValue);

    public static int ConvertToUnmanaged(BOOL value) => value.Value;
}