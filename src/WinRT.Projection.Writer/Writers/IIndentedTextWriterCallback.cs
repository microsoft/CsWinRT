// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Writers;

/// <summary>
/// A callback that writes content into an <see cref="IndentedTextWriter"/>. Implemented by
/// readonly value-type closures (typically constructed via factory methods on the relevant
/// helper class) so they can be passed as interpolation holes in
/// <see cref="IndentedTextWriter.Write(bool, ref IndentedTextWriter.AppendInterpolatedStringHandler)"/>
/// (or any other handler-overload) and dispatched by
/// <see cref="IndentedTextWriter.AppendInterpolatedStringHandler.AppendFormatted{T}(T)"/>
/// without going through a <c>ToString()</c> intermediate. This keeps caller-side emission
/// as a single readable interpolated raw string while still producing zero-allocation,
/// indentation-aware output.
/// </summary>
internal interface IIndentedTextWriterCallback
{
    /// <summary>
    /// Writes the callback's content into <paramref name="writer"/> at the writer's current
    /// position and indentation level.
    /// </summary>
    /// <param name="writer">The writer to emit content to.</param>
    void Write(IndentedTextWriter writer);
}
