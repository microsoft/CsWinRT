// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Emits non-nullable string types without changing other reference projection types.
/// </summary>
internal static class ReferenceStringTypeWriter
{
    /// <inheritdoc cref="WriteType(IndentedTextWriter, bool)"/>
    public static IndentedTextWriterCallback WriteType(bool referenceProjection)
    {
        return writer => WriteType(writer, referenceProjection);
    }

    /// <summary>
    /// Writes a string type, restoring the surrounding annotation context afterwards.
    /// </summary>
    public static void WriteType(IndentedTextWriter writer, bool referenceProjection)
    {
        if (!referenceProjection)
        {
            writer.Write("string");
            return;
        }

        writer.Write("\n#nullable enable annotations\nstring\n#nullable disable annotations\n");
    }
}
