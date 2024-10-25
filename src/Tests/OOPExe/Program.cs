using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.System.Com;
using WinRT;

namespace OOPExe
{
    class Program
    {
        public static ManualResetEvent done = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            object obj;
            int hr = PInvoke.CoCreateInstance(new Guid("15F1005B-E23A-4154-9417-CCD083D452BB"), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IAsyncAction).GUID, out obj);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            var asyncAction = MarshalInterface<IAsyncAction>.FromAbi(Marshal.GetIUnknownForObject(obj));
            asyncAction.Completed = Completed;

            done.WaitOne(15000);

            GC.KeepAlive(asyncAction);
        }

        public static void Completed(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            asyncInfo.GetResults();
            done.Set();
        }
    }
}
