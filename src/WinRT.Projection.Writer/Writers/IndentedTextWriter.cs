// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Adapted from `src/Authoring/WinRT.SourceGenerator2/Helpers/IndentedTextWriter.cs`
// (which itself was ported from ComputeSharp). The source-generator variant is a
// `ref struct` backed by a `DefaultInterpolatedStringHandler` because it lives in
// a Roslyn source generator (where transient compilation context + zero-alloc are
// the priority). This variant is a `class` backed by a `StringBuilder` so it can
// be passed around freely and live as long as needed in a long-running tool.
//
// Per the refactor plan (v5, Pass 9), this initial revision intentionally does
// NOT include custom interpolated-string handlers. Callsite syntax is identical
// (`writer.Write($"...")`) against either the `string` overload (via interpolation)
// or a future handler-based overload, so adding handlers later is a non-breaking
// optimization that does not require any callsite churn.

using System;
using System.Text;

namespace WindowsRuntime.ProjectionWriter.Writers;

/// <summary>
/// A general-purpose helper for building C# source text with consistent indentation.
/// </summary>
/// <remarks>
/// <para>
/// This type only knows about writing indented text. WinRT-specific concerns (file headers,
/// projection namespace blocks, mode flags, metadata cache) live elsewhere -- typically as
/// extension methods. See <c>WinRT.Interop.Generator</c>'s separation of concerns for the
/// equivalent design.
/// </para>
/// <para>
/// Indentation is applied per line: the first <see cref="Write(string, bool)"/> after a
/// newline (or at buffer start) prepends the current indentation string; mid-line writes do not.
/// Multiline content (passed with <c>isMultiline: true</c>) normalizes <c>CRLF</c> -> <c>LF</c>
/// and indents each line via the current indentation level. Empty lines never receive
/// indentation, so raw multi-line literals with blank lines do not gain trailing whitespace.
/// </para>
/// </remarks>
internal sealed class IndentedTextWriter
{
    /// <summary>The default indentation (4 spaces).</summary>
    private const string DefaultIndentation = "    ";

    /// <summary>The default new line character (<c>'\n'</c>).</summary>
    private const char DefaultNewLine = '\n';

    /// <summary>The underlying buffer that text is written to.</summary>
    private readonly StringBuilder _buffer;

#pragma warning disable IDE0032 // CurrentIndentLevel exposes the field directly via a property.
    /// <summary>The current indentation level (number of <see cref="DefaultIndentation"/> repeats).</summary>
    private int _currentIndentationLevel;
#pragma warning restore IDE0032

    /// <summary>The current indentation string (cached for fast reuse).</summary>
    private string _currentIndentation;

    /// <summary>Cached pre-built indentation strings indexed by indentation level.</summary>
    private string[] _availableIndentations;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndentedTextWriter"/> class with an empty buffer.
    /// </summary>
    public IndentedTextWriter()
    {
        _buffer = new StringBuilder();
        _currentIndentationLevel = 0;
        _currentIndentation = string.Empty;
        _availableIndentations = new string[4];
        _availableIndentations[0] = string.Empty;
        for (int i = 1; i < _availableIndentations.Length; i++)
        {
            _availableIndentations[i] = _availableIndentations[i - 1] + DefaultIndentation;
        }
    }

    /// <summary>Increases the current indentation level by one.</summary>
    public void IncreaseIndent()
    {
        _currentIndentationLevel++;

        if (_currentIndentationLevel == _availableIndentations.Length)
        {
            Array.Resize(ref _availableIndentations, _availableIndentations.Length * 2);
        }

        _currentIndentation = _availableIndentations[_currentIndentationLevel]
            ??= _availableIndentations[_currentIndentationLevel - 1] + DefaultIndentation;
    }

    /// <summary>Decreases the current indentation level by one.</summary>
    public void DecreaseIndent()
    {
        _currentIndentationLevel--;
        _currentIndentation = _availableIndentations[_currentIndentationLevel];
    }

    /// <summary>
    /// Writes an opening brace on the current line, increases the indentation level, and returns
    /// a <see cref="Block"/> token whose <see cref="Block.Dispose"/> method closes the block by
    /// decreasing the indentation level and writing the matching closing brace on its own line.
    /// </summary>
    /// <returns>A <see cref="Block"/> value that, when disposed, closes the open block.</returns>
    public Block WriteBlock()
    {
        WriteLine("{");
        IncreaseIndent();
        return new Block(this);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer, applying current indentation
    /// at the start of each new line.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</param>
    public void Write(string content, bool isMultiline = false)
    {
        Write(content.AsSpan(), isMultiline);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer, applying current indentation
    /// at the start of each new line.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</param>
    public void Write(scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (isMultiline)
        {
            while (content.Length > 0)
            {
                int newLineIndex = content.IndexOf(DefaultNewLine);

                if (newLineIndex < 0)
                {
                    // No newline left -- write the rest as a single line.
                    WriteRawText(content);
                    break;
                }

                ReadOnlySpan<char> line = content[..newLineIndex];

                // Strip trailing CR so output is normalized to LF regardless of source line endings.
                if (line is [.. var trim, '\r'])
                {
                    line = trim;
                }

                // Skip writing empty lines (avoids trailing whitespace from leading indentation
                // on what would otherwise be a blank line in the multi-line literal).
                if (!line.IsEmpty)
                {
                    WriteRawText(line);
                }
                WriteLine();

                content = content[(newLineIndex + 1)..];
            }
        }
        else
        {
            WriteRawText(content);
        }
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer if <paramref name="condition"/> is <see langword="true"/>; otherwise does nothing.
    /// </summary>
    /// <param name="condition">When <see langword="true"/>, writes <paramref name="content"/>; otherwise this call is a no-op.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteIf(bool condition, string content, bool isMultiline = false)
    {
        if (condition)
        {
            Write(content.AsSpan(), isMultiline);
        }
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer if <paramref name="condition"/> is <see langword="true"/>; otherwise does nothing.
    /// </summary>
    /// <param name="condition">When <see langword="true"/>, writes <paramref name="content"/>; otherwise this call is a no-op.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteIf(bool condition, scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (condition)
        {
            Write(content, isMultiline);
        }
    }

    /// <summary>
    /// Writes a newline to the underlying buffer.
    /// </summary>
    /// <param name="skipIfPresent">When <see langword="true"/>, skips writing the newline if the buffer already ends with a blank line or with <c>{\n</c> (collapses runs of blank lines to one).</param>
    public void WriteLine(bool skipIfPresent = false)
    {
        if (skipIfPresent && _buffer.Length > 0)
        {
            int len = _buffer.Length;

            // Check whether the buffer already ends with "\n\n" or "{\n" (after trimming
            // trailing spaces from the last line). If so, suppress the additional newline.
            int j = len - 1;
            while (j >= 0 && _buffer[j] == ' ')
            {
                j--;
            }

            if (j >= 1 && _buffer[j] == '\n' && _buffer[j - 1] == '\n')
            {
                return;
            }
            if (j >= 1 && _buffer[j] == '\n' && _buffer[j - 1] == '{')
            {
                return;
            }
        }

        _buffer.Append(DefaultNewLine);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer and appends a trailing newline.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteLine(string content, bool isMultiline = false)
    {
        WriteLine(content.AsSpan(), isMultiline);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer and appends a trailing newline.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteLine(scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        Write(content, isMultiline);
        WriteLine();
    }

    /// <summary>Writes a newline if <paramref name="condition"/> is <see langword="true"/>.</summary>
    /// <param name="condition">When <see langword="true"/>, writes a newline; otherwise this call is a no-op.</param>
    /// <param name="skipIfPresent">When <see langword="true"/>, suppresses runs of blank lines (see <see cref="WriteLine(bool)"/>).</param>
    public void WriteLineIf(bool condition, bool skipIfPresent = false)
    {
        if (condition)
        {
            WriteLine(skipIfPresent);
        }
    }

    /// <summary>Writes <paramref name="content"/> followed by a newline if <paramref name="condition"/> is <see langword="true"/>.</summary>
    /// <param name="condition">When <see langword="true"/>, writes <paramref name="content"/>+newline; otherwise this call is a no-op.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteLineIf(bool condition, string content, bool isMultiline = false)
    {
        if (condition)
        {
            WriteLine(content.AsSpan(), isMultiline);
        }
    }

    /// <summary>Writes <paramref name="content"/> followed by a newline if <paramref name="condition"/> is <see langword="true"/>.</summary>
    /// <param name="condition">When <see langword="true"/>, writes <paramref name="content"/>+newline; otherwise this call is a no-op.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteLineIf(bool condition, scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (condition)
        {
            Write(content, isMultiline);
            WriteLine();
        }
    }

    /// <summary>
    /// Returns the current buffer contents (trimmed) and clears the buffer.
    /// </summary>
    /// <returns>The text accumulated so far, trimmed of leading/trailing whitespace.</returns>
    public string ToStringAndClear()
    {
        string text = _buffer.ToString().Trim();
        _buffer.Clear();
        return text;
    }

    /// <summary>Returns the current buffer contents without modifying the buffer.</summary>
    public override string ToString() => _buffer.ToString();

    /// <summary>Gets the current length (in chars) of the underlying buffer.</summary>
    public int Length => _buffer.Length;

    /// <summary>Returns the last character written to the buffer, or <c>'\0'</c> if the buffer is empty.</summary>
    public char Back() => _buffer.Length == 0 ? '\0' : _buffer[^1];

    /// <summary>Returns the contents of a substring of the buffer (used for capture-and-restore patterns).</summary>
    /// <param name="startIndex">The starting position.</param>
    /// <param name="length">The length of the substring to return.</param>
    /// <returns>The substring of the buffer at the requested position.</returns>
    public string GetSubstring(int startIndex, int length) => _buffer.ToString(startIndex, length);

    /// <summary>Removes a range of characters from the buffer.</summary>
    /// <param name="startIndex">The starting position to remove.</param>
    /// <param name="length">The number of characters to remove.</param>
    public void Remove(int startIndex, int length) => _buffer.Remove(startIndex, length);

    /// <summary>Returns the current indent level (number of <see cref="Block"/>-equivalent units of indentation).</summary>
    public int CurrentIndentLevel => _currentIndentationLevel;

    /// <summary>
    /// Sets the indent level back to zero (for emergency reset; rarely needed).
    /// </summary>
    public void ResetIndent()
    {
        _currentIndentationLevel = 0;
        _currentIndentation = _availableIndentations[0];
    }

    /// <summary>
    /// Flushes the current buffer to <paramref name="path"/> (skipping the write if the file
    /// already exists with identical content), then clears the buffer.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    public void FlushToFile(string path)
    {
        string content = _buffer.ToString();
        if (System.IO.File.Exists(path))
        {
            try
            {
                if (System.IO.File.ReadAllText(path) == content)
                {
                    _buffer.Clear();
                    return;
                }
            }
            catch
            {
                // fall through to overwrite
            }
        }
        System.IO.File.WriteAllText(path, content);
        _buffer.Clear();
    }

    /// <summary>
    /// Flushes the current buffer contents to a string (without trimming) and clears the buffer.
    /// </summary>
    /// <returns>The full buffer contents, untrimmed.</returns>
    public string FlushToString()
    {
        string text = _buffer.ToString();
        _buffer.Clear();
        return text;
    }

    /// <summary>
    /// Writes raw text to the underlying buffer, prepending current indentation if positioned
    /// at the start of a new line.
    /// </summary>
    /// <param name="content">The raw text to write.</param>
    private void WriteRawText(scoped ReadOnlySpan<char> content)
    {
        // Skip writing indent for empty content so that empty lines never receive
        // trailing whitespace from the indentation prefix.
        if (content.IsEmpty)
        {
            return;
        }

        if (_buffer.Length == 0 || _buffer[^1] == DefaultNewLine)
        {
            _buffer.Append(_currentIndentation);
        }

        _buffer.Append(content);
    }

    /// <summary>
    /// Represents an open <c>{ ... }</c> block that needs to be closed (via
    /// <see cref="Dispose"/>). Returned from <see cref="WriteBlock"/>.
    /// </summary>
    public struct Block : IDisposable
    {
        private IndentedTextWriter? _writer;

        internal Block(IndentedTextWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// Closes the open block by decreasing the indentation level and writing a matching
        /// closing brace on its own line.
        /// </summary>
        public void Dispose()
        {
            IndentedTextWriter? writer = _writer;
            _writer = null;
            if (writer is not null)
            {
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
        }
    }
}