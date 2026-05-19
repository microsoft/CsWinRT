// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.References;

/// <summary>
/// Well-known ABI type names emitted as literals in projection source. Each constant is the
/// fully-qualified C# expression (with the <c>global::</c> prefix) for the corresponding
/// type as referenced from generated code.
/// </summary>
internal static class WellKnownAbiTypeNames
{
    /// <summary>The <c>global::ABI.System.Type</c> ABI helper struct.</summary>
    public const string AbiSystemType = "global::ABI.System.Type";

    /// <summary>The <c>global::ABI.System.Type*</c> pointer-to-ABI form.</summary>
    public const string AbiSystemTypePointer = "global::ABI.System.Type*";

    /// <summary>The <c>global::ABI.System.Exception</c> ABI helper struct.</summary>
    public const string AbiSystemException = "global::ABI.System.Exception";

    /// <summary>The <c>global::ABI.System.Exception*</c> pointer-to-ABI form.</summary>
    public const string AbiSystemExceptionPointer = "global::ABI.System.Exception*";

    /// <summary>The <c>global::ABI.System.Exception* data</c> parameter signature.</summary>
    public const string AbiSystemExceptionPointerData = "global::ABI.System.Exception* data";

    /// <summary>The <c>global::ABI.System.DateTimeOffset</c> ABI helper struct.</summary>
    public const string AbiSystemDateTimeOffset = "global::ABI.System.DateTimeOffset";

    /// <summary>The <c>global::ABI.System.TimeSpan</c> ABI helper struct.</summary>
    public const string AbiSystemTimeSpan = "global::ABI.System.TimeSpan";

    /// <summary>The <c>global::ABI.System.TypeMarshaller</c> static class.</summary>
    public const string AbiSystemTypeMarshaller = "global::ABI.System.TypeMarshaller";

    /// <summary>The <c>global::ABI.System.ExceptionMarshaller</c> static class.</summary>
    public const string AbiSystemExceptionMarshaller = "global::ABI.System.ExceptionMarshaller";
}
