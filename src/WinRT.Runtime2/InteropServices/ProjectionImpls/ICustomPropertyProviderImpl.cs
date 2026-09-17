// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The type-only <c>ICustomPropertyProvider</c> implementation for managed <c>FrameworkElement</c>-derived objects.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class ICustomPropertyProviderImpl
{
    /// <summary>
    /// The vtable for the type-only <c>ICustomPropertyProvider</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly ICustomPropertyProviderVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static ICustomPropertyProviderImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.GetCustomProperty = &GetCustomProperty;
        Vftbl.GetIndexedProperty = &GetIndexedProperty;
        Vftbl.GetStringRepresentation = &GetStringRepresentation;
        Vftbl.get_Type = &GetType;
    }

    /// <summary>
    /// Gets the IID shared by the UWP XAML and WinUI <c>ICustomPropertyProvider</c> interfaces.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownWindowsInterfaceIIDs.IID_ICustomPropertyProvider;
    }

    /// <summary>
    /// Gets a pointer to the type-only <c>ICustomPropertyProvider</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <summary>
    /// Reports that property binding requires an explicit provider.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetCustomProperty(void* thisPtr, HSTRING name, void** property)
    {
        *property = null;

        try
        {
            throw CreatePropertyBindingNotSupportedException(thisPtr);
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <summary>
    /// Reports that indexer binding requires an explicit provider.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetIndexedProperty(void* thisPtr, HSTRING name, ABI.System.Type type, void** property)
    {
        *property = null;

        try
        {
            throw CreatePropertyBindingNotSupportedException(thisPtr);
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <summary>
    /// Returns the managed object's string representation.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetStringRepresentation(void* thisPtr, HSTRING* value)
    {
        *value = null;

        try
        {
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            *value = HStringMarshaller.ConvertToUnmanaged(instance.ToString());

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <summary>
    /// Returns the actual managed type, independently of the CCW's runtime class name.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetType(void* thisPtr, ABI.System.Type* value)
    {
        *value = default;

        try
        {
            object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

            *value = ABI.System.TypeMarshaller.ConvertToUnmanaged(instance.GetType());

            return WellKnownErrorCodes.S_OK;
        }
        catch (Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <summary>
    /// Creates the unsupported-binding exception for a type-only provider.
    /// </summary>
    /// <param name="thisPtr">The COM interface pointer for the managed object.</param>
    /// <returns>An exception describing how to opt into property binding.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static NotSupportedException CreatePropertyBindingNotSupportedException(void* thisPtr)
    {
        object instance = ComInterfaceDispatch.GetInstance<object>((ComInterfaceDispatch*)thisPtr);

        return new NotSupportedException(
            $"ICustomProperty support used by XAML binding for type '{instance.GetType()}' requires an explicit 'ICustomPropertyProvider' implementation. " +
            "Mark the type with 'WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute' to generate one, or use a wrapper type that provides this support.");
    }
}
