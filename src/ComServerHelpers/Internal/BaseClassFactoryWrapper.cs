using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ComServerHelpers.Windows.Com;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;
using IUnknown = Windows.Win32.System.Com.IUnknown;

namespace ComServerHelpers.Internal;

[GeneratedComClass]
internal sealed partial class BaseClassFactoryWrapper(BaseClassFactory factory, ComWrappers comWrappers) : IClassFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "COM method, must not throw.")]
    public unsafe HRESULT CreateInstance(void* pUnkOuter, Guid* riid, void** ppvObject)
    {
        if (pUnkOuter is not null)
        {
            return HRESULT.CLASS_E_NOAGGREGATION;
        }

        if (!riid->Equals(IUnknown.IID_Guid) && !riid->Equals(factory.Iid))
        {
            return HRESULT.E_NOINTERFACE;
        }

        bool shouldReleaseUnknown = false;
        nint unknown = 0;
        try
        {
            var instance = factory.CreateInstance();
            unknown = comWrappers.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);

            if (riid->Equals(IUnknown.IID_Guid))
            {
                *ppvObject = (void*)unknown;
            }
            else
            {
                var hr = (HRESULT)Marshal.QueryInterface(unknown, ref *riid, out nint ppv);
                shouldReleaseUnknown = true;
                if (hr.Failed)
                {
                    return hr;
                }
                *ppvObject = (void*)ppv;
            }

            factory.OnInstanceCreated(instance);
        }
        catch (Exception e)
        {
            return (HRESULT)Marshal.GetHRForException(e);
        }
        finally
        {
            if (shouldReleaseUnknown)
            {
                Marshal.Release(unknown);
            }
        }
        return HRESULT.S_OK;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "COM method, must not throw.")]
    public HRESULT LockServer(BOOL fLock)
    {
        try
        {
            if (fLock != 0)
            {
                _ = CoAddRefServerProcess();
            }
            else
            {
                _ = CoReleaseServerProcess();
            }
        }
        catch (Exception e)
        {
            return (HRESULT)Marshal.GetHRForException(e);
        }
        return HRESULT.S_OK;
    }
}
