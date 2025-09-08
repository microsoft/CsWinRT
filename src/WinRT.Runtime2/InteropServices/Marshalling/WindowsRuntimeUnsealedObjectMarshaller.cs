﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for unsealed Windows Runtime objects.
/// </summary>
public static unsafe class WindowsRuntimeUnsealedObjectMarshaller
{
    /// <summary>
    /// Converts an unmanaged pointer to an unsealed Windows Runtime object to a managed object.
    /// </summary>
    /// <typeparam name="TCallback">The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> type to use for marshalling.</typeparam>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed managed object.</returns>
    public static object? ConvertToManaged<TCallback>(void* value)
        where TCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback, allows ref struct
    {
        if (value is null)
        {
            return null;
        }

        // Unwrap CCWs we recognize (same as with opaque objects)
        if (WindowsRuntimeMarshal.IsReferenceToManagedObject(value))
        {
            return ComWrappers.ComInterfaceDispatch.GetInstance<object>((ComWrappers.ComInterfaceDispatch*)value);
        }

        // Marshal the value with the supplied callback for unsealed types (or interfaces)
        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
            externalComObject: (nint)value,
            objectComWrappersCallback: null,
            unsealedObjectComWrappersCallback: WindowsRuntimeUnsealedObjectComWrappersCallback.GetInstance<TCallback>());
    }
}
