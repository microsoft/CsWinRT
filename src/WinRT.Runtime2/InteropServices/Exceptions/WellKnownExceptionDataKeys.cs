// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Keys for special key-value pairs stored in <see cref="System.Exception.Data"/> for marshalled exceptions.
/// </summary>
internal static class WellKnownExceptionDataKeys
{
    /// <summary>
    /// The <c>"Description"</c> key for the exception description.
    /// </summary>
    public const string Description = "Description";

    /// <summary>
    /// The <c>"RestrictedDescription"</c> key for the error description from the <c>IRestrictedErrorInfo</c> infrastructure.
    /// </summary>
    public const string RestrictedDescription = "RestrictedDescription";

    /// <summary>
    /// The <c>"RestrictedErrorReference"</c> key for the error reference from the <c>IRestrictedErrorInfo</c> infrastructure.
    /// </summary>
    public const string RestrictedErrorReference = "RestrictedErrorReference";

    /// <summary>
    /// The <c>"RestrictedCapabilitySid"</c> key for the error capability SID from the <c>IRestrictedErrorInfo</c> infrastructure.
    /// </summary>
    public const string RestrictedCapabilitySid = "RestrictedCapabilitySid";

    /// <summary>
    /// The <c>"__RestrictedErrorObjectReference"</c> key for the stored <see cref="WindowsRuntimeObjectReference"/> instance (undocumented).
    /// </summary>
    public const string RestrictedErrorObjectReference = "__RestrictedErrorObjectReference";

    /// <summary>
    /// The <c>"__HasRestrictedLanguageErrorObject"</c> key indicating whether there's a stored <see cref="WindowsRuntimeObjectReference"/> instance (undocumented).
    /// </summary>
    public const string HasRestrictedLanguageErrorObject = "__HasRestrictedLanguageErrorObject";

    /// <summary>
    /// The <c>"__InternalCsWinRTException"</c> key for the internal stored <see cref="System.Exception"/> instance (undocumented).
    /// </summary>
    public const string InternalCsWinRTException = "__InternalCsWinRTException";
}