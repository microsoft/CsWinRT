// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.ComponentModel;
using ABI.System.Collections;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IEnumerable"/> types.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterable"/>
internal sealed class WindowsRuntimeEnumerable : WindowsRuntimeObject, IEnumerable, IWindowsRuntimeInterface<IEnumerable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeEnumerable"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeEnumerable(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        return IEnumerableMethods.GetEnumerator(NativeObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
