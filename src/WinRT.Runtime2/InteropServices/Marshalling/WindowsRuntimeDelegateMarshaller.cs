// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime delegates.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeDelegateMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime delegate to a native COM object interface pointer.
    /// </summary>
    /// <param name="value">The input delegate to marshal.</param>
    /// <param name="iid">The IID of the Windows Runtime delegate.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method assumes that <paramref name="value"/> will only ever have a <see cref="WindowsRuntimeObjectReference"/>
    /// instance as target if produced by generated projection code. It is not valid to manually create a delegate around
    /// an incompatible <see cref="WindowsRuntimeObjectReference"/> instance and pass it to this method. Doing so is
    /// undefined behavior, and it will likely lead to memory corruption and/or runtime crashes.
    /// </para>
    /// <para>
    /// The returned <see cref="WindowsRuntimeObjectReferenceValue"/> value will own an additional
    /// reference for the marshalled <paramref name="value"/> instance (either its underlying native object, or
    /// a runtime-provided CCW for the managed object instance). It is responsibility of the caller to always
    /// make sure that the returned <see cref="WindowsRuntimeObjectReferenceValue"/> instance is disposed.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(Delegate? value, scoped in Guid iid)
    {
        if (value is null)
        {
            return default;
        }

        // Delegates coming from native code are projected with an extension implementation that creates a delegate instance
        // with the inner 'WindowsRuntimeObjectReference' as delegate target. Such delegate instances are only produced by
        // generated code, so we can rely on them wrapping the exact interface pointer for the delegate type. So if we get
        // such a delegate, we can just unwrap it and return it. Note that doing this also means that we can completely avoid
        // any 'AddRef' or 'QueryInterface' calls. If that's not the case, just marshal the managed delegate object normally.
        return value.Target is WindowsRuntimeObjectReference objectReference
            ? objectReference.AsValue()
            : new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport, in iid));
    }

    /// <summary>
    /// Converts an unmanaged pointer to a Windows Runtime delegate to its managed representation.
    /// </summary>
    /// <typeparam name="TCallback">The type of static callback for <see cref="ComWrappers"/> to marshal the delegate.</typeparam>
    /// <param name="value">The input delegate to convert to managed.</param>
    /// <returns>The resulting managed Windows Runtime delegate object.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static Delegate? ConvertToManaged<TCallback>(void* value)
        where TCallback : IWindowsRuntimeObjectComWrappersCallback, allows ref struct
    {
        if (value is null)
        {
            return null;
        }

        // If the value is a CCW we recognize, unwrap it (and assume it's a 'Delegate' instance).
        // We use 'ComInterfaceDispatch' directly as we also need a fast type cast on the result.
        if (WindowsRuntimeMarshal.IsReferenceToManagedObject(value))
        {
            return ComWrappers.ComInterfaceDispatch.GetInstance<Delegate>((ComWrappers.ComInterfaceDispatch*)value);
        }

        // Marshal the object with the supplied callback for sealed runtime class types
        object? managedDelegate = WindowsRuntimeComWrappers.Default.GetOrCreateObjectForComInstanceUnsafe(
            externalComObject: (nint)value,
            objectComWrappersCallback: WindowsRuntimeObjectComWrappersCallback.GetInstance<TCallback>(),
            unsealedObjectComWrappersCallback: null);

        return Unsafe.As<Delegate>(managedDelegate);
    }

    /// <summary>
    /// Marshals a Windows Runtime delegate to a native COM object interface pointer for a boxed value.
    /// </summary>
    /// <param name="value">The input delegate to marshal.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the Windows Runtime delegate type.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>, as a boxed <c>IReference`1</c> interface pointer.</returns>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="value"/> is a native delegate object that can't be boxed.</exception>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(Delegate? value, scoped in Guid iid)
    {
        if (value is null)
        {
            return default;
        }

        // If we have a native delegate, chances are we can't marshal it as an 'IReference<T>' object
        if (value.Target is WindowsRuntimeObjectReference objectReference)
        {
            // Try to do a 'QueryInterface' just in case, and throw if it fails (which is very likely)
            if (!objectReference.TryAsUnsafe(in iid, out void* interfacePtr))
            {
                [DoesNotReturn]
                [StackTraceHidden]
                static void ThrowNotSupportedException(object value)
                {
                    throw new NotSupportedException(
                        $"This delegate instance of type '{value.GetType()}' cannot be marshalled as a Windows Runtime 'IReference`1<T>' object, because it is wrapping a native " +
                        $"Windows Runtime delegate object, which does not implement the 'IReference`1<T>' interface. Only managed delegate instances can be marshalled this way.");
                }

                ThrowNotSupportedException(value);
            }

            return new(interfacePtr);
        }

        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport, in iid));
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed representation.
    /// </summary>
    /// <typeparam name="TCallback">The type of static callback for <see cref="ComWrappers"/> to marshal the delegate.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <returns>The resulting managed Windows Runtime delegate object.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime delegate type.
    /// </para>
    /// <para>
    /// Unlike <see cref="ConvertToManaged"/>, the <paramref name="value"/> parameter is expected to be an <c>IReference`1</c> pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static Delegate? UnboxToManaged<TCallback>(void* value)
        where TCallback : IWindowsRuntimeObjectComWrappersCallback, allows ref struct
    {
        if (value is null)
        {
            return null;
        }

        void* delegatePtr;

        // Unbox the native delegate (we always just discard the outer reference)
        IReferenceVftbl.get_ValueUnsafe(value, &delegatePtr).Assert();

        // At this point, we just convert the native delegate to a 'T' instance normally
        return ConvertToManaged<TCallback>(delegatePtr);
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed representation.
    /// </summary>
    /// <typeparam name="TCallback">The type of static callback for <see cref="ComWrappers"/> to marshal the delegate.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the Windows Runtime delegate type.</param>
    /// <returns>The resulting managed Windows Runtime delegate object.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime delegate type.
    /// </para>
    /// <para>
    /// Unlike <see cref="UnboxToManaged(void*)"/>, the <paramref name="value"/> parameter is expected to be an <c>IInspectable</c> pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static Delegate? UnboxToManaged<TCallback>(void* value, in Guid iid)
        where TCallback : IWindowsRuntimeObjectComWrappersCallback, allows ref struct
    {
        if (value is null)
        {
            return null;
        }

        // First, make sure we have the right 'IReference<T>' interface on 'value'
        IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* referencePtr).Assert();

        // Now that we have the 'IReference<T>' pointer, unbox it normally
        return UnboxToManaged<TCallback>(referencePtr);
    }
}
