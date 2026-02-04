using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.System.Com;
using WindowsRuntime.InteropServices;

namespace OOPExe
{
    class Program
    {
        public static ManualResetEvent done = new ManualResetEvent(false);

        static unsafe void Main(string[] args)
        {
            void* obj;
            int hr = PInvoke.CoCreateInstance(new Guid("15F1005B-E23A-4154-9417-CCD083D452BB"), null, CLSCTX.CLSCTX_LOCAL_SERVER, typeof(IAsyncAction).GUID, out obj);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            var asyncAction = (IAsyncAction) WindowsRuntimeMarshal.ConvertToManaged(obj);
            asyncAction.Completed = Completed;

            if (done.WaitOne(20000))
            {
                // Allow Completed handler to finish.
                Thread.Sleep(5000);
            }
        }

        public static void Completed(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            asyncInfo.GetResults();
            done.Set();
        }
    }
}