// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for Windows Runtime or COM objects that does not do any additional <c>QueryInterface</c> calls for specific interfaces.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeUnknownMarshaller
{
    /// <summary>
    /// Marshals a Windows Runtime or COM object to a <see cref="WindowsRuntimeObjectReferenceValue"/> instance.
    /// </summary>
    /// <param name="value">The input object to marshal.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReferenceValue"/> instance for <paramref name="value"/>.</returns>
    /// <remarks>
    /// <para><inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged" path="remarks/node()"/></para>
    /// <para>The returned interface pointer is only guaranteed to be some <c>IUnknown</c> object.</para>
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(object? value)
    {
        if (value is null)
        {
            return default;
        }

        // If 'value' is a 'WindowsRuntimeObject', return the cached object reference (whatever interface it may be).
        // This is the most efficient thing to do, as this object reference is always guaranteed to be initialized.
        if (value is WindowsRuntimeObject { HasUnwrappableNativeObjectReference: true } windowsRuntimeObject)
        {
            return new(windowsRuntimeObject.NativeObjectReference);
        }

        // If 'value' is a managed wrapper for a native delegate, return the wrapped native delegate directly
        if (value is Delegate { Target: WindowsRuntimeObjectReference windowsRuntimeDelegate })
        {
            return windowsRuntimeDelegate.AsValue();
        }

        // Marshal 'value' as an 'IUnknown' (this method will take care of correctly marshalling objects with the right vtables)
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value);

        return new(thisPtr);
    }

    /// <summary>
    /// Release a given Windows Runtime or COM object.
    /// </summary>
    /// <param name="value">The input object to free.</param>
    /// <remarks>
    /// Unlike <see cref="Marshal.Release"/>, this method will not throw <see cref="ArgumentNullException"/>
    /// if <paramref name="value"/> is <see langword="null"/>. This method can be used with any object type.
    /// </remarks>
    public static void Free(void* value)
    {
        if (value is null)
        {
            return;
        }

        _ = IUnknownVftbl.ReleaseUnsafe(value);
    }
}
