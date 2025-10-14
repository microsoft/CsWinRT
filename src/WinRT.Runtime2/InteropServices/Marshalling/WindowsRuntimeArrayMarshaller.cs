// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime arrays.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeArrayMarshaller
{
    /// <summary>
    /// Unboxes and converts an unmanaged pointer to a Windows Runtime array to its managed representation.
    /// </summary>
    /// <typeparam name="TCallback">The type of static callback for <see cref="ComWrappers"/> to marshal the array.</typeparam>
    /// <param name="value">The input object to unbox and convert to managed.</param>
    /// <param name="iid">The IID of the <c>IReferenceArray`1</c> interface for the array type.</param>
    /// <returns>The resulting managed Windows Runtime array value.</returns>
    /// <remarks>
    /// <para>
    /// This method should only be used to unbox <c>IReferenceArray`1</c> objects to their underlying Windows Runtime array type.
    /// </para>
    /// <para>
    /// The <paramref name="value"/> parameter can be any <c>IInspectable</c> pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    public static Array? UnboxToManaged<TCallback>(void* value, in Guid iid)
        where TCallback : IWindowsRuntimeArrayComWrappersCallback, allows ref struct
    {
        if (value is null)
        {
            return null;
        }

        // First, make sure we have the right 'IReferenceArray<T>' interface on 'value'
        IUnknownVftbl.QueryInterfaceUnsafe(value, in iid, out void* referencePtr).Assert();

        try
        {
            uint count;
            void* result;

            // Unbox the underlying array (we always just discard the outer reference)
            IReferenceArrayVftbl.get_ValueUnsafe(referencePtr, &count, &result).Assert();

            // Forward to the supplied callback
            return TCallback.CreateArray(count, result);
        }
        finally
        {
            WindowsRuntimeObjectMarshaller.Free(referencePtr);
        }
    }
}
