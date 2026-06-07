// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Writers;

/// <summary>
/// A callback that writes content into an <see cref="IndentedTextWriter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Instances are typically produced by factory methods on the relevant helper class (e.g.
/// <c>TypedefNameWriter.WriteTypeName</c>), or directly as local lambdas, and then passed
/// as interpolation holes in
/// <see cref="IndentedTextWriter.Write(bool, ref IndentedTextWriter.AppendInterpolatedStringHandler)"/>
/// (or any other handler-overload) and dispatched by
/// <see cref="IndentedTextWriter.AppendInterpolatedStringHandler.AppendFormatted{T}(T)"/>
/// without going through a <see cref="object.ToString"/> intermediate. This keeps caller-side
/// emission as a single readable interpolated raw string while still producing indentation-aware output.
/// </para>
/// </remarks>
/// <param name="writer">The writer to emit content to at its current position and indentation level.</param>
internal delegate void IndentedTextWriterCallback(IndentedTextWriter writer);
