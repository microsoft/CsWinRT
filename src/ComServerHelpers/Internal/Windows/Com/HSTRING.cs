using System.Runtime.InteropServices.Marshalling;
using ComServerHelpers.Internal.Windows.Com.Marshalling;

namespace Windows.Win32.System.WinRT;

[NativeMarshalling(typeof(HStringMarshaller))]
partial struct HSTRING;