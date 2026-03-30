// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A marshaller for <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class WindowsRuntimeKeyValuePairTypeMarshaller
{
    /// <summary>
    /// Marshals a boxed Windows Runtime value type value to a native COM object interface pointer.
    /// </summary>
    /// <param name="value">The input boxed value to marshal.</param>
    /// <param name="flags">Flags used to configure the generated interface.</param>
    /// <param name="iid">The IID of the interface for the Windows Runtime value type.</param>
    /// <returns>The resulting marshalled object for <paramref name="value"/>, as an interface pointer.</returns>
    /// <exception cref="Exception">Thrown if <paramref name="value"/> cannot be marshalled.</exception>
    /// <remarks>
    /// This method should only be used to marshal pre-boxed <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/>
    /// values. It is not generic to reduce binary size, and so that the boxing stub can always be inlined in the caller. Note
    /// that the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> is special, in that it's a custom-mapped
    /// value type in C#, but it's an interface type (i.e. <c>IKeyValuePair&lt;K, V&gt;</c>) on the Windows Runtime native side.
    /// </remarks>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanagedUnsafe(object value, CreateComInterfaceFlags flags, scoped in Guid iid)
    {
        return new((void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, flags, in iid));
    }
}
#endif