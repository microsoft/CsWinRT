// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The <c>IActivationFactory</c> implementation for managed types.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/activation/nn-activation-iactivationfactory"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IActivationFactoryImpl
{
    /// <summary>
    /// The <see cref="IActivationFactoryVftbl"/> value for the managed <c>IActivationFactory</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IActivationFactoryVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IActivationFactoryImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.ActivateInstance = &ActivateInstance;
    }

    /// <summary>
    /// Gets a pointer to the managed <c>IActivationFactory</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/windows/win32/api/activation/nf-activation-iactivationfactory-activateinstance"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT ActivateInstance(void* thisPtr, void** instance)
    {
        if (instance is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *instance = null;

        try
        {
            IActivationFactory factory = ComInterfaceDispatch.GetInstance<IActivationFactory>((ComInterfaceDispatch*)thisPtr);

            object activated = factory.ActivateInstance();

            WindowsRuntimeObjectReferenceValue value = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(activated);

            *instance = value.GetThisPtr();

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}
