using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace WinRT
{
    static class Context
    {
        [DllImport("api-ms-win-core-com-l1-1-0.dll")]
        private static extern int CoGetObjectContext(ref Guid riid, out IntPtr ppv);

        public static IContextCallback GetContextCallback()
        {
            Guid riid = typeof(IContextCallback).GUID;
            Marshal.ThrowExceptionForHR(CoGetObjectContext(ref riid, out IntPtr contextCallbackPtr));
            return new ABI.WinRT.Interop.IContextCallback(ObjectReference<ABI.WinRT.Interop.IContextCallback.Vftbl>.Attach(ref contextCallbackPtr));
        }
    }
}
