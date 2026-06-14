// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Writers;

/// <summary>
/// Extension methods for <see cref="IndentedTextWriterCallback"/>.
/// </summary>
internal static class IndentedTextWriterCallbackExtensions
{
    /// <summary>
    /// Writes the delegate's content into a pooled <see cref="IndentedTextWriter"/> at indent
    /// level <c>0</c> and returns the resulting string. Use this overload when the caller needs
    /// the emitted text as a standalone string (for example, to compose into another string or
    /// to pass through APIs that take <see cref="string"/>) rather than appending it inline as
    /// an interpolation hole inside a larger writer call.
    /// </summary>
    /// <param name="callback">The callback to invoke.</param>
    /// <returns>The string produced by the callback.</returns>
    public static string Format(this IndentedTextWriterCallback callback)
    {
        using IndentedTextWriterOwner owner = IndentedTextWriterPool.GetOrCreate();

        callback(owner.Writer);

        return owner.Writer.ToString();
    }
}
