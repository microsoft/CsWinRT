// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Activation factory types to activate Windows Runtime objects with custom logic.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class WindowsRuntimeActivationFactoryCallback
{
    /// <summary>
    /// A type containing logic to activate a composed Windows Runtime object.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#composable-activation"/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class DerivedComposed
    {
        /// <summary>
        /// Invokes the activation logic.
        /// </summary>
        /// <param name="additionalParameters">The additional parameters to provide to the activation callback.</param>
        /// <param name="baseInterface">The <see cref="WindowsRuntimeObject"/> instance being constructed.</param>
        /// <param name="innerInterface">The resulting non-delegating <c>IInspectable</c> object.</param>
        /// <param name="defaultInterface">The resulting default interface pointer.</param>
        public abstract unsafe void Invoke(
            ReadOnlySpan<object?> additionalParameters,
            WindowsRuntimeObject? baseInterface,
            out void* innerInterface,
            out void* defaultInterface);
    }

    /// <summary>
    /// A type containing logic to activate a sealed Windows Runtime object.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#activation"/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class DerivedSealed
    {
        /// <summary>
        /// Invokes the activation logic.
        /// </summary>
        /// <param name="additionalParameters">The additional parameters to provide to the activation callback.</param>
        /// <param name="defaultInterface">The resulting default interface pointer.</param>
        public abstract unsafe void Invoke(ReadOnlySpan<object?> additionalParameters, out void* defaultInterface);
    }
}
