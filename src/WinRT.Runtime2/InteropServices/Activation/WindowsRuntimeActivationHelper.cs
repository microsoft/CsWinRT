// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides support for activating Windows Runtime types.
/// </summary>
internal static unsafe class WindowsRuntimeActivationHelper
{
    /// <summary>
    /// Activates a new Windows Runtime sealed instance.
    /// </summary>
    /// <param name="activationFactoryObjectReference">The <see cref="WindowsRuntimeObjectReference"/> for the <c>IActivationFactory</c> instance.</param>
    /// <param name="inspectableInterface">The resulting <c>IInspectable</c> interface pointer.</param>
    /// <exception cref="Exception">Thrown if activating the instance fails.</exception>
    /// <remarks>
    /// This shared factory helper can be used to activate Windows Runtime sealed types that have a parameterless constructor.
    /// If additional parameters are needed, separate factory stubs should be used, to marshal them and update the signature.
    /// </remarks>
    /// <see href="https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#composable-activation"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActivateInstanceUnsafe(WindowsRuntimeObjectReference activationFactoryObjectReference, out void* inspectableInterface)
    {
        using WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue();

        fixed (void** inspectableInterfacePtr = &inspectableInterface)
        {
            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                instance: inspectableInterfacePtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }

    /// <param name="iid">The IID of the default interface pointer (from the activation factory) to return.</param>
    /// <param name="defaultInterface">The resulting default interface pointer.</param>
    /// <inheritdoc cref="ActivateInstanceUnsafe(WindowsRuntimeObjectReference, out void*)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActivateInstanceUnsafe(
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        in Guid iid,
        out void* defaultInterface)
    {
        void* inspectableInterface;

        // Get the 'IInspectable' object from the activation factory (same as above)
        using (WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue())
        {
            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                instance: &inspectableInterface);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }

        // Query the 'IInspectable' object for the default interface, which is what callers expect.
        // We only need this when using the parameterless constructor, since in this case we must
        // go through 'IActivationFactory', which only declares 'IInspectable' as the return type
        // for 'CreateInstance'. For other constructors instead, those would be declared on each
        // specialized factory type, and would return the default interface directly.
        try
        {
            fixed (void** defaultInterfacePtr = &defaultInterface)
            {
                IUnknownVftbl.QueryInterfaceUnsafe(inspectableInterface, in iid, out defaultInterface).Assert();
            }
        }
        finally
        {
            _ = IUnknownVftbl.ReleaseUnsafe(inspectableInterface);
        }
    }

    /// <summary>
    /// Activates a new Windows Runtime instance.
    /// </summary>
    /// <param name="activationFactoryObjectReference">The <see cref="WindowsRuntimeObjectReference"/> for the <c>IActivationFactory</c> instance.</param>
    /// <param name="param0">The additional <see cref="string"/> parameter for the constructor.</param>
    /// <param name="defaultInterface">The resulting default interface pointer.</param>
    /// <exception cref="Exception">Thrown if activating the instance fails.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActivateInstanceUnsafe(
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        string? param0,
        out void* defaultInterface)
    {
        using WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue();

        fixed (char* param0Ptr = param0)
        fixed (void** defaultInterfacePtr = &defaultInterface)
        {
            HStringMarshaller.ConvertToUnmanagedUnsafe(param0Ptr, param0?.Length, out HStringReference param0Reference);

            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                param0: param0Reference.HString,
                instance: defaultInterfacePtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }

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
    /// <see href="https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#composable-activation"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActivateInstanceUnsafe(
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        WindowsRuntimeObject? baseInterface,
        out void* innerInterface,
        out void* defaultInterface)
    {
        using WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue baseInterfaceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);

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

    /// <param name="param0">The additional <see cref="string"/> parameter for the constructor.</param>
    /// <remarks>
    /// This shared factory helper can be used to activate Windows Runtime composable types that have an additional <see cref="string"/> parameter.
    /// </remarks>
    /// <inheritdoc cref="ActivateInstanceUnsafe(WindowsRuntimeObjectReference, WindowsRuntimeObject?, out void*, out void*)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActivateInstanceUnsafe(
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        string? param0,
        WindowsRuntimeObject? baseInterface,
        out void* innerInterface,
        out void* defaultInterface)
    {
        using WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue baseInterfaceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);

        fixed (char* param0Ptr = param0)
        fixed (void** innerInterfacePtr = &innerInterface)
        fixed (void** defaultInterfacePtr = &defaultInterface)
        {
            HStringMarshaller.ConvertToUnmanagedUnsafe(param0Ptr, param0?.Length, out HStringReference param0Reference);

            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                param0: param0Reference.HString,
                baseInterface: baseInterfaceValue.GetThisPtrUnsafe(),
                innerInterface: innerInterfacePtr,
                instance: defaultInterfacePtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }

    /// <param name="param0">The additional <see cref="NotifyCollectionChangedAction"/> parameter for the constructor.</param>
    /// <param name="param1">The additional <see cref="IList"/> parameter for the constructor.</param>
    /// <param name="param2">The additional <see cref="IList"/> parameter for the constructor.</param>
    /// <param name="param3">The additional <see cref="int"/> parameter for the constructor.</param>
    /// <param name="param4">The additional <see cref="int"/> parameter for the constructor.</param>
    /// <remarks>
    /// This shared factory helper can be used to activate Windows Runtime composable types that have additional parameters.
    /// </remarks>
    /// <inheritdoc cref="ActivateInstanceUnsafe(WindowsRuntimeObjectReference, WindowsRuntimeObject?, out void*, out void*)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActivateInstanceUnsafe(
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        NotifyCollectionChangedAction param0,
        IList? param1,
        IList? param2,
        int param3,
        int param4,
        WindowsRuntimeObject? baseInterface,
        out void* innerInterface,
        out void* defaultInterface)
    {
        using WindowsRuntimeObjectReferenceValue activationFactoryValue = activationFactoryObjectReference.AsValue();
        using WindowsRuntimeObjectReferenceValue baseInterfaceValue = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(baseInterface);

        using WindowsRuntimeObjectReferenceValue param1Value = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(param1);
        using WindowsRuntimeObjectReferenceValue param2Value = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(param2);

        fixed (void** innerInterfacePtr = &innerInterface)
        fixed (void** defaultInterfacePtr = &defaultInterface)
        {
            HRESULT hresult = IActivationFactoryVftbl.ActivateInstanceUnsafe(
                thisPtr: activationFactoryValue.GetThisPtrUnsafe(),
                param0: param0,
                param1: param1Value.GetThisPtrUnsafe(),
                param2: param2Value.GetThisPtrUnsafe(),
                param3: param3,
                param4: param4,
                baseInterface: baseInterfaceValue.GetThisPtrUnsafe(),
                innerInterface: innerInterfacePtr,
                instance: defaultInterfacePtr);

            RestrictedErrorInfo.ThrowExceptionForHR(hresult);
        }
    }
}