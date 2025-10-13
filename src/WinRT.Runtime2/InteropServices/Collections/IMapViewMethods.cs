// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IMapView&lt;K, V&gt;</c> types.
/// </summary>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IMapViewMethods
{
    /// <summary>
    /// Gets the number of items in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The number of items in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.imapview-2.size"/>
    public static uint Size(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' is identical between 'IMapView<T>' and 'IVectorView<T>'
        return IVectorViewMethods.Size(thisReference);
    }
}
