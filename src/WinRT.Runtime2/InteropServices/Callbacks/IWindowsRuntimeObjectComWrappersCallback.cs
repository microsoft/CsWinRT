// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for callbacks for <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>, for Windows Runtime objects.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe interface IWindowsRuntimeObjectComWrappersCallback
{
    /// <summary>
    /// Creates a managed Windows Runtime object for a given native object.
    /// </summary>
    /// <param name="value">The input native object to marshal.</param>
    /// <param name="wrapperFlags">Flags used to describe the created wrapper object.</param>
    /// <returns>The resulting managed Windows Runtime object.</returns>
    /// <remarks>
    /// <para>
    /// The <paramref name="value"/> parameter may be a specific interface pointer depending on the specific
    /// use of each <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation, as defined by the
    /// invoked method in one of the available marshaller types for CsWinRT. It is guaranteed to be some
    /// <c>IUnknown</c> pointer, but it won't always be just <c>IUnknown</c>, unlike the actual input value
    /// for <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>.
    /// This allows implementations to avoid redundant <c>QueryInterface</c> calls, if the exact interface
    /// is statically visible.
    /// </para>
    /// <para>
    /// For instance, if a given native API returns a <c>Windows.UI.Xaml.Data.PropertyChangedHandler</c> object,
    /// the generated code can rely on that pointer being to the delegate interface for this type. Which means
    /// that <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementations for that can assume <paramref name="value"/>
    /// will be such an interface pointer, and avoid doing a <c>QueryInterface</c> call for that same interface.
    /// </para>
    /// <para>
    /// This method will be called from the <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> method,
    /// so implementations must not call back into <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/>
    /// themselves, but rather they should just marshal the object directly, and return it. Calling back into
    /// <see cref="ComWrappers.CreateObject(nint, CreateObjectFlags, object?, out CreatedWrapperFlags)"/> is not supported, and it will likely lead
    /// to a stack overflow exception.
    /// </para>
    /// </remarks>
    static abstract object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags);
}