// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime objects.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
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
    /// <returns>The resulting managed object.</returns>
    public static object? ConvertToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        // If the value is a CCW we recognize, just unwrap it directly
        if (TryGetManagedObjectForProjection(value, out object? managedObject))
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
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object, always producing a
    /// wrapper for it, even when the pointer is a COM Callable Wrapper for an existing managed object.
    /// </summary>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed object.</returns>
    /// <remarks>
    /// This is needed to project a Windows Runtime class implemented in C#: the implementation does not derive
    /// from the projected class, so the projected type can only be obtained by wrapping it. Unwrapping (which is
    /// what <see cref="ConvertToManaged(void*)"/> does) would hand back the implementation instead.
    /// </remarks>
    public static object? ConvertToManagedUnsafe(void* value)
    {
        return value is null
            ? null
            : WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
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
        if (TryGetManagedObjectForProjection(value, out object? managedObject))
        {
            return managedObject;
        }

        // Marshal the object as an opaque object, as we have no static type information available
        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
            externalComObject: (nint)value,
            objectComWrappersCallback: WindowsRuntimeObjectComWrappersCallback.GetInstance<TCallback>(),
            unsealedObjectComWrappersCallback: null);
    }

    /// <summary>
    /// Retrieves the managed object implementing a Windows Runtime class, from a projected instance wrapping it.
    /// </summary>
    /// <param name="value">The projected instance to retrieve the implementation from.</param>
    /// <param name="implementableClassType">The generated base type the implementation is expected to derive from.</param>
    /// <returns>The managed object implementing the Windows Runtime class that <paramref name="value"/> represents.</returns>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="value"/> does not wrap such an implementation.</exception>
    /// <remarks>
    /// This backs the explicit conversion that the generated bases declare, which is how an author gets their own
    /// implementation back from a projected instance (see <see cref="IWindowsRuntimeImplementableClass"/>). It is
    /// the inverse of the implicit conversion those bases also declare, and unlike it, it can fail: the instance
    /// may be wrapping a native implementation, or an implementation of a different Windows Runtime class.
    /// </remarks>
    public static object ConvertToImplementation(object value, Type implementableClassType)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(implementableClassType);

        [DoesNotReturn]
        [StackTraceHidden]
        static object ThrowInvalidCastException(object value, Type implementableClassType)
        {
            throw new InvalidCastException(
                $"The object of type '{value.GetType()}' does not wrap an implementation of type '{implementableClassType}'. Only an instance " +
                $"that was obtained by marshalling such an implementation can be converted back to it: one representing a Windows Runtime " +
                $"object implemented natively, or in another process, has no managed implementation to return.");
        }

        // Only a projected type can be wrapping an implementation, and only when it is a wrapper to begin with
        // (an aggregated object is the managed type itself, so there is nothing underneath it to retrieve).
        if (value is not WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject)
        {
            return ThrowInvalidCastException(value, implementableClassType);
        }

        // Retrieve the managed object behind the wrapper, which is only possible if the native object it wraps is
        // a COM Callable Wrapper for one. Marshalling deliberately does not unwrap these (see
        // 'TryGetManagedObjectForProjection'), so this is the supported way to reach it.
        if (!WindowsRuntimeMarshal.TryGetManagedObject(windowsRuntimeObject.NativeObjectReference.GetThisPtrUnsafe(), out object? managedObject))
        {
            return ThrowInvalidCastException(value, implementableClassType);
        }

        // The managed object is an implementation of some Windows Runtime class, but not necessarily of the one
        // being converted to. The caller's cast to the implementation type would catch that too, but the checked
        // cast here is what makes the failure explain itself.
        return implementableClassType.IsInstanceOfType(managedObject)
            ? managedObject
            : ThrowInvalidCastException(value, implementableClassType);
    }

    /// <summary>
    /// Tries to retrieve a managed object from a pointer to a COM object, if it is a COM Callable Wrapper for one
    /// that can be handed back to callers expecting a projected type.
    /// </summary>
    /// <param name="value">The external COM object to try to get a managed object from.</param>
    /// <param name="result">The resulting managed object, if it can be handed back directly.</param>
    /// <returns>Whether <paramref name="result"/> was retrieved and can be handed back directly.</returns>
    /// <remarks>
    /// <para>
    /// Unwrapping a COM Callable Wrapper preserves reference identity, which is what callers want in the general
    /// case. It is wrong for an implementation of a Windows Runtime class declared in existing metadata: such an
    /// implementation derives from the generated <see cref="IWindowsRuntimeImplementableClass"/> base rather than
    /// from the projected class, so handing it back would give callers a type that is unrelated to the one the
    /// signature promises. They get a runtime callable wrapper for it instead, exactly as they would if the class
    /// had been implemented natively, or in another process.
    /// </para>
    /// <para>
    /// The implementation can still be recovered from that wrapper, by casting it to the generated base (or to
    /// the implementation type itself).
    /// </para>
    /// </remarks>
    internal static bool TryGetManagedObjectForProjection(void* value, [NotNullWhen(true)] out object? result)
    {
        if (!WindowsRuntimeMarshal.TryGetManagedObject(value, out object? managedObject))
        {
            result = null;

            return false;
        }

        if (managedObject is IWindowsRuntimeImplementableClass)
        {
            result = null;

            return false;
        }

        result = managedObject;

        return true;
    }
}
