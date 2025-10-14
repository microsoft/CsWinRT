// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// A <see cref="WindowsRuntimeObject"/> for a marshalled object with no available metadata.
/// </summary>
/// <remarks>
/// This type is used to create managed objects when no type information can be resolved. The
/// returned instances will be visible as just <see cref="object"/>-s. The only operations
/// that can be done with such objects are to pass them back to native, or to cast them to
/// some interface by leveraging <see cref="System.Runtime.InteropServices.IDynamicInterfaceCastable"/>
/// and <see cref="System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider"/>.
/// </remarks>
internal sealed class WindowsRuntimeInspectable : WindowsRuntimeObject
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    public WindowsRuntimeInspectable(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
