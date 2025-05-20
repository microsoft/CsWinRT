// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;

namespace ComServerHelpers.Internal.Windows.Com.Marshalling;

[CustomMarshaller(typeof(HRESULT), MarshalMode.UnmanagedToManagedOut, typeof(HResultMarshaller))]
[CustomMarshaller(typeof(HRESULT), MarshalMode.UnmanagedToManagedIn, typeof(HResultMarshaller))]
[CustomMarshaller(typeof(HRESULT), MarshalMode.ManagedToUnmanagedOut, typeof(HResultMarshaller))]
[CustomMarshaller(typeof(HRESULT), MarshalMode.ManagedToUnmanagedIn, typeof(HResultMarshaller))]
internal static class HResultMarshaller
{
    public static HRESULT ConvertToManaged(int nativeValue) => new HRESULT(nativeValue);

    public static int ConvertToUnmanaged(HRESULT value) => value.Value;
}
