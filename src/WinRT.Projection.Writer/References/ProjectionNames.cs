// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.References;

/// <summary>
/// Common string literals used throughout the projection writer (prefixes, suffixes, conventions).
/// Mirrors the `WinRT.Interop.Generator/References/InteropNames.cs` pattern -- centralizing
/// repeated literals so they have a single canonical source.
/// </summary>
internal static class ProjectionNames
{
    /// <summary>The C# global namespace prefix (<c>"global::"</c>).</summary>
    public const string GlobalPrefix = "global::";

    /// <summary>The ABI namespace prefix (<c>"ABI."</c>).</summary>
    public const string AbiPrefix = "ABI.";

    /// <summary>The fully-qualified ABI namespace prefix (<c>"global::ABI."</c>).</summary>
    public const string GlobalAbiPrefix = "global::ABI.";

    /// <summary>The suffix appended to ABI marshaller class names (<c>"Marshaller"</c>).</summary>
    public const string MarshallerSuffix = "Marshaller";

    /// <summary>The conventional name of the managed-to-native marshaller method (<c>"ConvertToUnmanaged"</c>).</summary>
    public const string ConvertToUnmanaged = "ConvertToUnmanaged";

    /// <summary>The conventional name of the native-to-managed marshaller method (<c>"ConvertToManaged"</c>).</summary>
    public const string ConvertToManaged = "ConvertToManaged";

    /// <summary>The conventional name of the static IID property on a projected interface (<c>"IID"</c>).</summary>
    public const string IID = "IID";

    /// <summary>The C# void-pointer keyword form (<c>"void*"</c>).</summary>
    public const string VoidPointer = "void*";
}
