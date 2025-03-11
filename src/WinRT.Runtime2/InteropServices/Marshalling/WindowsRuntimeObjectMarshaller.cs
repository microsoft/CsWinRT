// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeObjectMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime object to a <see cref="WindowsRuntimeObjectReferenceValue"/> instance.
    /// </summary>
    /// <param name="value">The input object to marshal.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> instance for <paramref name="value"/>.</returns>
    /// <remarks>
    /// The returned <see cref="WindowsRuntimeObjectReferenceValue"/> value will own an additional
    /// reference for the marshalled <paramref name="value"/> instance (either its underlying native object, or
    /// a runtime-provided CCW for the managed object instance). It is responsibility of the caller to always
    /// make sure that the returned <see cref="WindowsRuntimeObjectReferenceValue"/> instance is disposed.
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(object? value)
    {
        if (value is null)
        {
            return default;
        }

        // If 'value' is a 'WindowsRuntimeObject', return the cached object reference for 'IInspectable'
        if (value is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject)
        {
            return new(windowsRuntimeObject.InspectableObjectReference);
        }

        using WindowsRuntimeObjectReferenceValue unmanagedValue = default;

        // If 'value' is not a projected Windows Runtime class, we need to consult the type map and try to
        // get the proxy type for the constructed object. If we have a result, then we can use the marshaller
        // on the proxy type to marshal the object to native. This will cover all cases such as custom mapped
        // types, generic type instantiations, and user-defined types implementing projected interfaces.
        if (WindowsRuntimeMarshallingInfo.TryGetInfo(value.GetType(), out WindowsRuntimeMarshallingInfo? info))
        {
            *&unmanagedValue = info.GetMarshaller().ConvertToUnmanaged(value);
        }
        else
        {
            // If we got here, we need to marshal the object ourselves, like we do for interfaces. This applies
            // to both normal user-defined types, and managed types derived from Windows Runtime classes. This
            // will cover cases where we're just marshalling an opaque object as just 'IInspectable'.
            void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);

            *&unmanagedValue = new WindowsRuntimeObjectReferenceValue(thisPtr);
        }

        // 'ComWrappers' returns an 'IUnknown' pointer, so we can't avoid an additional 'QueryInterface' for 'IInspectable'
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(unmanagedValue.GetThisPtrUnsafe(), in WellKnownInterfaceIds.IID_IInspectable, out void* interfacePtr);

        // All CCWs produced by our 'ComWrappers' implementation will always implement 'IInspectable'
        Debug.Assert(WellKnownErrorCodes.Succeeded(hresult));

        return new(interfacePtr);
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object.
    /// </summary>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed managed object.</returns>
    public static object? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        WindowsRuntimeComWrappers.CreateDelegateTargetType = null;
        WindowsRuntimeComWrappers.CreateObjectTargetType = null;

        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstance((nint)value, CreateObjectFlags.TrackerObject);
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to its managed <typeparamref name="T"/> object.
    /// </summary>
    /// <typeparam name="T">The type of object to marshal values to (it cannot be <see cref="object"/>).</typeparam>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed <typeparamref name="T"/> value.</returns>
    public static T? ConvertToManaged<T>(void* value)
        where T : class
    {
        if (value is null)
        {
            return null;
        }

        WindowsRuntimeComWrappers.CreateDelegateTargetType = null;
        WindowsRuntimeComWrappers.CreateObjectTargetType = typeof(T);

        object? managedObject = WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstance((nint)value, CreateObjectFlags.TrackerObject);

        WindowsRuntimeComWrappers.CreateObjectTargetType = null;

        return (T?)managedObject;
    }
}
