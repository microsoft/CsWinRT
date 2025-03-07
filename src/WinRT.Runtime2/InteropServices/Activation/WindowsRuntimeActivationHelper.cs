// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides support for activating Windows Runtime types.
/// </summary>
internal static unsafe class WindowsRuntimeActivationHelper
{
    /// <summary>
    /// Activates a new Windows Runtime composable instance, either standalone or with composition.
    /// </summary>
    /// <param name="activationFactoryObjectReference">The <see cref="WindowsRuntimeObjectReference"/> for the <c>IActivationFactory</c> instance.</param>
    /// <param name="baseInterface">The <see cref="WindowsRuntimeObject"/> instance being constructed (either projected or user-defined, derived from a projected type).</param>
    /// <param name="innerInterface">The resulting non-delegating <c>IInspectable</c> object.</param>
    /// <param name="defaultInterface">The resulting default interface pointer.</param>
    /// <exception cref="Exception">Thrown if activating the instance fails.</exception>
    /// <remarks>
    /// This shared factory helper can be used to activate Windows Runtime composable types that have a parameterless constructor.
    /// If additional parameters are needed, separate factory stubs should be used, to marshal them and update the signature.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/en-us/uwp/winrt-cref/winrt-type-system#composable-activation"/>
    public static unsafe void ActivateInstanceUnsafe(
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        WindowsRuntimeObject? baseInterface,
        out void* innerInterface,
        out void* defaultInterface)
    {
        using WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue baseInterfaceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanagedUnsafe(baseInterface);

        fixed (void** innerInterfacePtr = &innerInterface)
        fixed (void** defaultInterfacePtr = &defaultInterface)
        {
            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                baseInterface: baseInterfaceValue.GetThisPtrUnsafe(),
                innerInterface: innerInterfacePtr,
                instance: defaultInterfacePtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }
}
