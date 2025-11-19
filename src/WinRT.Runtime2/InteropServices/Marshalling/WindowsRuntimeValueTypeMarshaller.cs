// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeValueTypeMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime value type value to a native COM object interface pointer for a boxed value.
    /// </summary>
    /// <typeparam name="T">The value type to marshal.</typeparam>
    /// <param name="value">The input value to marshal.</param>
    /// <param name="flags">Flags used to configure the generated interface.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the Windows Runtime value type.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>, as a boxed <c>IReference`1</c> interface pointer.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged<T>(T? value, CreateComInterfaceFlags flags, scoped in Guid iid)
        where T : struct
    {
        if (value is null)
        {
            return default;
        }

        // Box the value type and return the right interface pointer for it
        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, flags, in iid));
    }

    /// <summary>
    /// Marshals a Windows Runtime value type value to a native COM object interface pointer for a boxed value.
    /// </summary>
    /// <typeparam name="T">The value type to marshal.</typeparam>
    /// <param name="value">The input value to marshal.</param>
    /// <param name="flags">Flags used to configure the generated interface.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the Windows Runtime value type.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>, as a boxed <c>IReference`1</c> interface pointer.</returns>
    /// <exception cref="InvalidCastException">Thrown if <paramref name="value"/> is not of type <typeparamref name="T"/>.</exception>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    /// <remarks>
    /// This overload can be used when <paramref name="value"/> is already boxed, to avoid an additional allocation. In all other
    /// cases, <see cref="BoxToUnmanaged{T}(T?, CreateComInterfaceFlags, in Guid)"/> should be preferred, as it avoids a type check.
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged<T>(object? value, CreateComInterfaceFlags flags, scoped in Guid iid)
        where T : struct
    {
        if (value is null)
        {
            return default;
        }

        // Validate that the input type is actually of the expected type.
        // We do this separately so we can reuse the boxed object safely.
        _ = (T)value;

        // Create a CCW for the boxed value and return the right interface pointer for it
        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, flags, in iid));
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed representation.
    /// </summary>
    /// <typeparam name="T">The blittable struct type to marshal.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <returns>The resulting managed Windows Runtime blittable struct value.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime blittable struct type.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> parameter is expected to be an <c>IReference`1</c> pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static T? UnboxToManaged<T>(void* value)
        where T : unmanaged
    {
        if (value is null)
        {
            return null;
        }

        T result;

        // Unbox the blittable value (we always just discard the outer reference)
        IReferenceVftbl.get_ValueUnsafe(value, &result).Assert();

        return result;
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed representation.
    /// </summary>
    /// <typeparam name="T">The blittable struct type to marshal.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the struct type.</param>
    /// <returns>The resulting managed Windows Runtime blittable struct value.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime blittable struct type.
    /// </para>
    /// <para>
    /// Unlike <see cref="UnboxToManaged(void*)"/>, the <paramref name="value"/> parameter is expected to be an <c>IInspectable</c> pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static T? UnboxToManaged<T>(void* value, in Guid iid)
        where T : unmanaged
    {
        if (value is null)
        {
            return null;
        }

        // First, make sure we have the right 'IReference<T>' interface on 'value'
        IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* referencePtr).Assert();

        T result;

        // Now that we have the 'IReference<T>' pointer, unbox it normally
        HRESULT hresult = IReferenceVftbl.get_ValueUnsafe(referencePtr, &result);

        // Always release the temporary interface pointer before possibly throwing
        _ = IUnknownVftbl.ReleaseUnsafe(referencePtr);

        // There's a small chance that 'get_Value' might fail, so we need to check the result
        hresult.Assert();

        return result;
    }

    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime object to its managed representation.
    /// </summary>
    /// <typeparam name="T">The blittable struct type to marshal.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <param name="iid">The IID of the <c>IReference`1</c> interface for the struct type.</param>
    /// <returns>The resulting managed Windows Runtime blittable struct value.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReference`1</c> objects to their underlying Windows Runtime blittable struct type.
    /// </para>
    /// <para>
    /// Unlike <see cref="UnboxToManaged(void*)"/>, the <paramref name="value"/> parameter is expected to be an <c>IInspectable</c> pointer.
    /// </para>
    /// <para>
    /// Additionally, unlike <see cref="UnboxToManaged(void*, in Guid)"/>, <paramref name="value"/> is assumed to not be <see langword="null"/>.
    /// This method will not check that, and it will always return a non-nullable <typeparamref name="T"/> value as the result.
    /// </para>
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static T UnboxToManagedUnsafe<T>(void* value, in Guid iid)
        where T : unmanaged
    {
        IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* referencePtr).Assert();

        T result;

        // Try to get the value and handle errors, see notes in 'UnboxToManaged' above
        HRESULT hresult = IReferenceVftbl.get_ValueUnsafe(referencePtr, &result);

        _ = IUnknownVftbl.ReleaseUnsafe(referencePtr);

        hresult.Assert();

        return result;
    }
}