// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace ABI.System.Collections;

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <c>IReadOnlyList</c>.
/// </summary>
/// <remarks>
/// <para>
/// There is no non-generic <see cref="global::System.Collections.Generic.IReadOnlyList{T}"/> type in .NET, however this
/// type still uses "IReadOnlyList" in its name to match the naming convention of adapter types matching .NET type names.
/// </para>
/// <para>
/// Because this interface is not projected, only the marshaller attribute type and the proxy type are needed. The proxy
/// type will be generated at compile time, as its runtime class name will depend on the XAML configuration being used.
/// </para>
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class IReadOnlyListComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableVectorView,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeReadOnlyList(valueReference);
    }
}