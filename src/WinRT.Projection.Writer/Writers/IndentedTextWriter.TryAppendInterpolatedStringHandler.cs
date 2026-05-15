// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable IDE0038

namespace WindowsRuntime.ProjectionWriter.Writers;

/// <inheritdoc cref="IndentedTextWriter"/>
internal partial class IndentedTextWriter
{
    /// <summary>
    /// Provides a handler used by the language compiler to conditionally append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    /// <remarks>
    /// This handler differs from <see cref="AppendInterpolatedStringHandler"/> in that it accepts a leading <c>condition</c> argument and short-circuits
    /// the entire interpolation when that condition is <see langword="false"/>: no <c>AppendLiteral</c> or <c>AppendFormatted</c> call is emitted by
    /// the compiler, so neither the literal segments nor the interpolation expressions are evaluated.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct TryAppendInterpolatedStringHandler
    {
        /// <summary>The associated <see cref="IndentedTextWriter"/> to which to append.</summary>
        private readonly IndentedTextWriter _writer;

        /// <summary>When <see langword="true"/>, treats the content as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</summary>
        private readonly bool _isMultiline;

        /// <summary>Creates a handler used to conditionally append an interpolated string into a <see cref="IndentedTextWriter"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="writer">The associated <see cref="IndentedTextWriter"/> to which to append.</param>
        /// <param name="condition">When <see langword="true"/>, writes the content, otherwise it does nothing.</param>
        /// <param name="shouldAppend">Whether the handler is enabled.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public TryAppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            IndentedTextWriter writer,
            bool condition,
            out bool shouldAppend)
        {
            if (condition)
            {
                _writer = writer;
                _isMultiline = false;

                shouldAppend = true;
            }
            else
            {
                // We're intentionally suppressing the warning here: the writer shouldn't ever be
                // used if the handler is disabled, this just further validates it (it would throw).
                _writer = null!;
                _isMultiline = false;

                shouldAppend = false;
            }
        }

        /// <summary>Creates a handler used to conditionally append an interpolated string into a <see cref="IndentedTextWriter"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="writer">The associated <see cref="IndentedTextWriter"/> to which to append.</param>
        /// <param name="condition">When <see langword="true"/>, writes the content, otherwise it does nothing.</param>
        /// <param name="isMultiline">When <see langword="true"/>, treats the content as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</param>
        /// <param name="shouldAppend">Whether the handler is enabled.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public TryAppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            IndentedTextWriter writer,
            bool condition,
            bool isMultiline,
            out bool shouldAppend)
        {
            if (condition)
            {
                _writer = writer;
                _isMultiline = isMultiline;

                shouldAppend = true;
            }
            else
            {
                _writer = null!;
                _isMultiline = false;

                shouldAppend = false;
            }
        }

        /// <summary>Writes the specified string to the handler.</summary>
        /// <param name="value">The string to write.</param>
        public void AppendLiteral(string value)
        {
            _writer.Write(_isMultiline, value);
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value)
        {
            if (value is null)
            {
                return;
            }

            // Handle custom callbacks first (these are only value types)
            if (typeof(T).IsValueType && value is IIndentedTextWriterCallback)
            {
                ((IIndentedTextWriterCallback)value).Write(_writer);

                return;
            }

            // If the value is a 'string', write it while preserving the multiline semantics.
            // Otherwise, leverage the 'StringBuilder' handler for zero-alloc interpolation.
            if (value is string text)
            {
                _writer.Write(_isMultiline, text);
            }
            else
            {
                _ = _writer._buffer.Append($"{value}");
            }
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, string? format)
        {
            if (value is null)
            {
                return;
            }

            // If the value is a 'string', write it while preserving the multiline semantics.
            // Otherwise, leverage the 'StringBuilder' handler for zero-alloc interpolation.
            if (value is string text)
            {
                _writer.Write(_isMultiline, text);
            }
            else
            {
                StringBuilder.AppendInterpolatedStringHandler handler = new(0, 1, _writer._buffer);

                handler.AppendFormatted(value, format);

                _ = _writer._buffer.Append(ref handler);
            }
        }

        /// <summary>Writes the specified character span to the handler.</summary>
        /// <param name="value">The span to write.</param>
        public void AppendFormatted(scoped ReadOnlySpan<char> value)
        {
            _writer.Write(_isMultiline, value);
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value)
        {
            if (value is null)
            {
                return;
            }

            _writer.Write(_isMultiline, value);
        }
    }
}
