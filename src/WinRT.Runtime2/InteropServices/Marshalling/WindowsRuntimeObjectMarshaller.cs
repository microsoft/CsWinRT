// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime objects.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
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

        // If 'value' is a managed wrapper for a native delegate, it probably can't be marshalled
        if (value is Delegate { Target: WindowsRuntimeObjectReference windowsRuntimeDelegate })
        {
            // Try to do a 'QueryInterface' just in case, and throw if it fails (which is very likely)
            if (!windowsRuntimeDelegate.TryAsUnsafe(in WellKnownWindowsInterfaceIIDs.IID_IInspectable, out void* inspectablePtr))
            {
                [DoesNotReturn]
                [StackTraceHidden]
                static void ThrowNotSupportedException(object value)
                {
                    throw new NotSupportedException(
                        $"This delegate instance of type '{value.GetType()}' cannot be marshalled as a Windows Runtime 'IInspectable' object, because it is wrapping a native " +
                        $"Windows Runtime delegate object, which does not implement the 'IInspectable' interface. Only managed delegate instances can be marshalled this way.");
                }

                ThrowNotSupportedException(value);
            }

            return new(inspectablePtr);
        }

        // Marshal 'value' as an 'IInspectable' (this method will take care of correctly marshalling objects with the right vtables)
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, in WellKnownWindowsInterfaceIIDs.IID_IInspectable);

        return new(thisPtr);
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

        // If the value is a CCW we recognize, just unwrap it directly
        if (WindowsRuntimeMarshal.TryGetManagedObject(value, out object? managedObject))
        {
            return managedObject;
        }

        // Marshal the object as an opaque object, as we have no static type information available
        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
            externalComObject: (nint)value,
            objectComWrappersCallback: null,
            unsealedObjectComWrappersCallback: null);
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object.
    /// </summary>
    /// <typeparam name="TCallback">The <see cref="IWindowsRuntimeObjectComWrappersCallback"/> type to use for marshalling.</typeparam>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed managed object.</returns>
    /// <remarks>
    /// Unlike <see cref="ConvertToManaged(void*)"/>, this overload is meant to be used primarily for sealed types (e.g. sealed runtime classes),
    /// whenever there is static type information available for the type. This allows the marshalling logic to be optimized and to avoid having
    /// to perform a lookup via the interop type map to retrieve the marshalling attribute, and to perform one extra <c>QueryInterface</c> call.
    /// </remarks>
    public static object? ConvertToManaged<TCallback>(void* value)
        where TCallback : IWindowsRuntimeObjectComWrappersCallback, allows ref struct
    {
        if (value is null)
        {
            return null;
        }

        // If the value is a CCW we recognize, just unwrap it directly
        if (WindowsRuntimeMarshal.TryGetManagedObject(value, out object? managedObject))
        {
            return managedObject;
        }

        // Marshal the object as an opaque object, as we have no static type information available
        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
            externalComObject: (nint)value,
            objectComWrappersCallback: WindowsRuntimeObjectComWrappersCallback.GetInstance<TCallback>(),
            unsealedObjectComWrappersCallback: null);
    }
}
