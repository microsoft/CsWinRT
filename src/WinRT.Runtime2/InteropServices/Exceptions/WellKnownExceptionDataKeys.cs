// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Keys for special key-value pairs stored in <see cref="Exception.Data"/> for marshalled exceptions.
/// </summary>
internal static class WellKnownExceptionDataKeys
{
    /// <summary>
    /// The <c>"RestrictedErrorObjectReference"</c> key for the stored <see cref="WindowsRuntimeObjectReference"/> instance.
    /// </summary>
    public const string RestrictedErrorObjectReference = "__RestrictedErrorObjectReference";

    /// <summary>
    /// The <c>"HasRestrictedLanguageErrorObject"</c> key indicating whether there's a stored <see cref="WindowsRuntimeObjectReference"/> instance.
    /// </summary>
    public const string HasRestrictedLanguageErrorObject = "__HasRestrictedLanguageErrorObject";
}