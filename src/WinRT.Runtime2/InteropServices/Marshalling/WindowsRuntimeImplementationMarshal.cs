// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Marshalling support for C# implementations of Windows Runtime classes declared in existing Windows Runtime metadata (.winmd).
/// </summary>
/// <remarks>
/// An implementation derives from a generated <see cref="IWindowsRuntimeImplementableClass"/> base rather than from the
/// projected class, so the two are unrelated types. These methods convert between them, and let the rest of marshalling
/// keep handing out the projected type for an implementation.
/// </remarks>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class WindowsRuntimeImplementationMarshal
{
    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime object to a managed object, always producing a
    /// wrapper for it, even when the pointer is a COM Callable Wrapper for an existing managed object.
    /// </summary>
    /// <param name="value">The input object to convert to managed.</param>
    /// <returns>The resulting managed object.</returns>
    /// <remarks>
    /// This backs the implicit conversion that the generated bases declare: the implementation does not derive from the
    /// projected class, so the projected type can only be obtained by wrapping it. Unwrapping (which is what
    /// <see cref="WindowsRuntimeObjectMarshaller.ConvertToManaged(void*)"/> does) would hand back the implementation instead.
    /// </remarks>
    public static object? WrapToManaged(void* value)
    {
        if (value is null)
        {
            return null;
        }

        return WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
            externalComObject: (nint)value,
            objectComWrappersCallback: null,
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
    /// implementation back from a projected instance. It is the inverse of <see cref="WrapToManaged"/>, and unlike it,
    /// it can fail: the instance may be wrapping a native implementation, or an implementation of a different Windows
    /// Runtime class.
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
                $"obtained by marshalling such an implementation can be converted back to it. Instances backed by a native or out-of-process " +
                $"object, or by an implementation of a different type, have no such implementation to return.");
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
        if (!WindowsRuntimeMarshal.TryGetManagedObject(value, out object? managedObject) ||
            managedObject is IWindowsRuntimeImplementableClass)
        {
            result = null;

            return false;
        }

        result = managedObject;

        return true;
    }
}
