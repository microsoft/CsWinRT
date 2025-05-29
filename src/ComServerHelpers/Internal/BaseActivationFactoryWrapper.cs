// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ComServerHelpers.Windows.Com;
using Windows.Win32.Foundation;
using static Windows.Win32.PInvoke;

namespace ComServerHelpers.Internal;

[GeneratedComClass]
internal sealed partial class BaseActivationFactoryWrapper(BaseActivationFactory factory, ComWrappers comWrappers) : IActivationFactory
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "COM method, must not throw.")]
    public unsafe HRESULT ActivateInstance(void** instance)
    {
        if (instance is null)
        {
            return HRESULT.E_POINTER;
        }

        nint unknown = 0;
        try
        {
            object managedInstance = factory.ActivateInstance();
            factory.OnInstanceCreated(managedInstance);

            unknown = comWrappers.GetOrCreateComInterfaceForObject(managedInstance, CreateComInterfaceFlags.None);
            *instance = (void*)unknown;
        }
        catch (Exception e)
        {
            if (unknown != 0)
            {
                Marshal.Release(unknown);
            }

            WinRT.ExceptionHelpers.SetErrorInfo(e);
            return (HRESULT)WinRT.ExceptionHelpers.GetHRForException(e);
        }
        return HRESULT.S_OK;
    }

    public unsafe HRESULT GetIids(uint* iidCount, Guid** iids)
    {
        if (iidCount is null || iids is null)
        {
            return HRESULT.E_POINTER;
        }

        *iidCount = 1;
        *iids = (Guid*)NativeMemory.Alloc((nuint)sizeof(Guid));
        *iids[0] = global::Windows.Win32.System.WinRT.IActivationFactory.IID_Guid;
        return HRESULT.S_OK;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "COM method, must not throw.")]
    public unsafe HRESULT GetRuntimeClassName(global::Windows.Win32.System.WinRT.HSTRING* className)
    {
        if (className is null)
        {
            return HRESULT.E_POINTER;
        }

        try
        {
            string? fullName = factory.GetType().FullName;

            if (fullName is null)
            {
                return HRESULT.E_UNEXPECTED;
            }

            fixed (char* fullNamePtr = fullName)
            {
                return WindowsCreateString((PCWSTR)fullNamePtr, (uint)fullName.Length, className);
            }
        }
        catch (Exception e)
        {
            WinRT.ExceptionHelpers.SetErrorInfo(e);
            return (HRESULT)WinRT.ExceptionHelpers.GetHRForException(e);
        }
    }

    public unsafe HRESULT GetTrustLevel(global::Windows.Win32.System.WinRT.TrustLevel* trustLevel)
    {
        if (trustLevel is null)
        {
            return HRESULT.E_POINTER;
        }

        *trustLevel = global::Windows.Win32.System.WinRT.TrustLevel.BaseTrust;
        return HRESULT.S_OK;
    }
}
