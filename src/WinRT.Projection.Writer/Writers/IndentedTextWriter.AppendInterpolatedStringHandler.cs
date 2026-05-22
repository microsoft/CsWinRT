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
    /// Provides a handler used by the language compiler to append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In multiline mode, the handler automatically suppresses blank lines that exist in the
    /// template only because every interpolation hole on that line expanded to empty content
    /// (e.g. an attribute callback that early-returns based on build configuration). Literal
    /// blank lines in the source template (consecutive newlines not separated by an interpolation
    /// hole) are always preserved unchanged. Both LF and CRLF source line endings are handled.
    /// </para>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public ref struct AppendInterpolatedStringHandler
    {
        /// <summary>The associated <see cref="IndentedTextWriter"/> to which to append.</summary>
        private readonly IndentedTextWriter _writer;

        /// <summary>When <see langword="true"/>, treats the content as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</summary>
        private readonly bool _isMultiline;

        /// <summary>
        /// Tracks whether any <c>AppendFormatted</c> call between the previous and the next <c>AppendLiteral</c>
        /// emitted any content. Reset to <see langword="false"/> at every <c>AppendLiteral</c>, set to
        /// <see langword="true"/> by <c>AppendFormatted</c> calls that grow the buffer. Used by the blank-line
        /// suppression rule to decide whether a literal segment's leading newline can be dropped.
        /// </summary>
        private bool _anyContentBetweenLiterals;

        /// <summary>Creates a handler used to append an interpolated string into a <see cref="IndentedTextWriter"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="writer">The associated <see cref="IndentedTextWriter"/> to which to append.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            IndentedTextWriter writer)
        {
            _writer = writer;
            _isMultiline = false;
            _anyContentBetweenLiterals = false;
        }

        /// <summary>Creates a handler used to append an interpolated string into a <see cref="IndentedTextWriter"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="writer">The associated <see cref="IndentedTextWriter"/> to which to append.</param>
        /// <param name="isMultiline">When <see langword="true"/>, treats the content as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            IndentedTextWriter writer,
            bool isMultiline)
        {
            _writer = writer;
            _isMultiline = isMultiline;
            _anyContentBetweenLiterals = false;
        }

        /// <summary>Writes the specified string to the handler.</summary>
        /// <param name="value">The string to write.</param>
        public void AppendLiteral(string value)
        {
            ReadOnlySpan<char> span = value;

            // Blank-line suppression rule: a literal segment that begins with a newline (LF or
            // CRLF) immediately after one or more empty interpolation holes whose surrounding
            // literals also ended in '\n' would emit a stray blank line. Strip the leading
            // newline to collapse the would-be blank line.
            //
            // The rule fires only when:
            //   1. The previous content in the buffer ended with '\n' (the line is "fresh"), AND
            //   2. No 'AppendFormatted' call since the previous literal emitted any content, AND
            //   3. This literal starts with a newline (matched as either '\n' or '\r\n', since
            //      raw-string literals inherit the source file's line endings, i.e. CRLF on
            //      Windows, LF on Unix). CR-only line endings are not supported.
            //
            // Literal blank lines (e.g. "...\n\n...") within a single 'AppendLiteral' are always
            // preserved because they are emitted by the multiline parser in one streaming pass
            // without the rule getting a chance to fire between them.
            if (span.Length > 0 &&
                !_anyContentBetweenLiterals &&
                _writer._buffer.Length > 0 &&
                _writer._buffer[^1] == DefaultNewLine)
            {
                if (span[0] == '\n')
                {
                    span = span[1..];
                }
                else if (span.Length > 1 && span[0] == '\r' && span[1] == '\n')
                {
                    span = span[2..];
                }
            }

            _writer.Write(_isMultiline, span);

            // We just wrote a literal, so reset the "any content between literals" flag for next time
            _anyContentBetweenLiterals = false;
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

            int beforeLength = _writer._buffer.Length;

            // Handle callbacks first: invoke the delegate against the writer.
            if (value is IndentedTextWriterCallback writeCallback)
            {
                writeCallback(_writer);
            }
            else if (value is string text)
            {
                // If the value is a 'string', write it while preserving the multiline semantics.
                _writer.Write(_isMultiline, text);
            }
            else
            {
                // Otherwise, leverage the 'StringBuilder' handler for zero-alloc interpolation.
                _ = _writer._buffer.Append($"{value}");
            }

            // Track whether we actually wrote any content as part of this interpolation hole
            if (_writer._buffer.Length > beforeLength)
            {
                _anyContentBetweenLiterals = true;
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

            int beforeLength = _writer._buffer.Length;

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

            // Track the interpolation result (see above)
            if (_writer._buffer.Length > beforeLength)
            {
                _anyContentBetweenLiterals = true;
            }
        }

        /// <summary>Writes the specified character span to the handler.</summary>
        /// <param name="value">The span to write.</param>
        public void AppendFormatted(scoped ReadOnlySpan<char> value)
        {
            int beforeLength = _writer._buffer.Length;

            _writer.Write(_isMultiline, value);

            // Track the interpolation result (see above)
            if (_writer._buffer.Length > beforeLength)
            {
                _anyContentBetweenLiterals = true;
            }
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value)
        {
            if (value is null)
            {
                return;
            }

            int beforeLength = _writer._buffer.Length;

            _writer.Write(_isMultiline, value);

            // Track the interpolation result (see above)
            if (_writer._buffer.Length > beforeLength)
            {
                _anyContentBetweenLiterals = true;
            }
        }
    }
}

