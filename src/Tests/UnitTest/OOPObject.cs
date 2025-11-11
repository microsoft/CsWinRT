using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.System.Com;
using WindowsRuntime.InteropServices.Marshalling;

namespace UnitTest
{
    // https://docs.microsoft.com/windows/win32/api/unknwn/nn-unknwn-iclassfactory
    [ComImport]
    [ComVisible(false)]
    [Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IClassFactory
    {
        void CreateInstance(
            [MarshalAs(UnmanagedType.Interface)] object pUnkOuter,
            ref Guid riid,
            out IntPtr ppvObject);

        void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
    }

    [ComVisible(true)]
    internal class WinRTClassFactory<T> : IClassFactory
    {
        private static readonly Guid IUnknown = new Guid("00000000-0000-0000-C000-000000000046");


        public static void RegisterClass<T>(IClassFactory classFactory)
        {
            RegisterClassObject(typeof(T).GUID, classFactory);
        }

        private static void RegisterClassObject(Guid clsid, object factory)
        {
            int hr = PInvoke.CoRegisterClassObject(in clsid, factory, CLSCTX.CLSCTX_LOCAL_SERVER, (int)REGCLS.REGCLS_MULTIPLEUSE, out uint _);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        private readonly Func<T> createFunction;
        private readonly Dictionary<Guid, Func<object, IntPtr>> marshalFuncByGuid;

        public WinRTClassFactory(Func<T> createFunction, Dictionary<Guid, Func<object, IntPtr>> marshalFuncByGuid)
        {
            this.createFunction = createFunction ?? throw new ArgumentNullException(nameof(createFunction));
            this.marshalFuncByGuid = marshalFuncByGuid ?? throw new ArgumentNullException(nameof(marshalFuncByGuid));
        }

        public void CreateInstance(
            [MarshalAs(UnmanagedType.Interface)] object pUnkOuter,
            ref Guid riid,
            out IntPtr ppvObject)
        {
            if (pUnkOuter != null)
            {
                throw new COMException();
            }

            object obj = this.createFunction();
            if (riid == IUnknown)
            {
#pragma warning disable CSWINRT3001 // Type or member is obsolete
                unsafe
                {
                    ppvObject = (IntPtr)WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(obj).DetachThisPtrUnsafe();
                }
#pragma warning restore CSWINRT3001 // Type or member is obsolete
            }
            else
            {
                if (!this.marshalFuncByGuid.TryGetValue(riid, out Func<object, IntPtr> marshalFunc))
                {
                    throw new InvalidCastException();
                }

                ppvObject = marshalFunc(obj);
            }
        }

        public void LockServer(bool fLock)
        {
            // No-op
        }
    }

    [ComVisible(true)]
    [Guid("15F1005B-E23A-4154-9417-CCD083D452BB")]
    [ComDefaultInterface(typeof(IAsyncAction))]
    internal class OOPAsyncAction : IAsyncAction
    {
        public bool delegateCalled;

        public AsyncActionCompletedHandler Completed { get; set; }

        public Exception ErrorCode => throw new NotImplementedException();

        public uint Id => throw new NotImplementedException();

        public AsyncStatus Status => throw new NotImplementedException();

        public void Cancel()
        {
        }

        public void Close()
        {
            Completed(this, AsyncStatus.Completed);
        }

        public void GetResults()
        {
            delegateCalled = true;
        }
    }
}
