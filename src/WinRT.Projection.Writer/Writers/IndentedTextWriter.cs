// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Adapted from src/Authoring/WinRT.SourceGenerator2/Helpers/IndentedTextWriter.cs (which itself
// was ported from ComputeSharp); reshaped here as a class backed by StringBuilder so it can be
// passed around freely and live as long as needed in this long-running tool.

using System;
using System.IO;
using System.Runtime.CompilerServices;
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
internal sealed partial class IndentedTextWriter
{
    /// <summary>
    /// The default indentation (4 spaces).
    /// </summary>
    private const string DefaultIndentation = "    ";

    /// <summary>
    /// The default new line character (<c>'\n'</c>).
    /// </summary>
    private const char DefaultNewLine = '\n';

    /// <summary>
    /// The underlying buffer that text is written to.
    /// </summary>
    private readonly StringBuilder _buffer;

    /// <summary>
    /// The current indentation string (cached for fast reuse).
    /// </summary>
    private string _currentIndentation;

    /// <summary>
    /// Cached pre-built indentation strings indexed by indentation level.
    /// </summary>
    private string[] _availableIndentations;

    /// <summary>
    /// Gets the current indent level (number of <see cref="Block"/>-equivalent units of indentation).
    /// </summary>
    public int CurrentIndentLevel { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="IndentedTextWriter"/> class with an empty buffer.
    /// </summary>
    public IndentedTextWriter()
    {
        _buffer = new StringBuilder();
        CurrentIndentLevel = 0;
        _currentIndentation = string.Empty;
        _availableIndentations = new string[4];
        _availableIndentations[0] = string.Empty;
        for (int i = 1; i < _availableIndentations.Length; i++)
        {
            _availableIndentations[i] = _availableIndentations[i - 1] + DefaultIndentation;
        }
    }

    /// <summary>
    /// Increases the current indentation level by one.
    /// </summary>
    public void IncreaseIndent()
    {
        CurrentIndentLevel++;

        if (CurrentIndentLevel == _availableIndentations.Length)
        {
            Array.Resize(ref _availableIndentations, _availableIndentations.Length * 2);
        }

        _currentIndentation = _availableIndentations[CurrentIndentLevel]
            ??= _availableIndentations[CurrentIndentLevel - 1] + DefaultIndentation;
    }

    /// <summary>
    /// Decreases the current indentation level by one.
    /// </summary>
    public void DecreaseIndent()
    {
        CurrentIndentLevel--;
        _currentIndentation = _availableIndentations[CurrentIndentLevel];
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
    /// Writes an interpolated expression to the underlying buffer, applying current indentation
    /// at the start of each new line.
    /// </summary>
    /// <param name="handler">The interpolated content to write.</param>
    public void Write([InterpolatedStringHandlerArgument("")] ref AppendInterpolatedStringHandler handler)
    {
        _ = this;
    }

    /// <summary>
    /// Writes an interpolated expression to the underlying buffer, applying current indentation
    /// at the start of each new line.
    /// </summary>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="handler"/> as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</param>
    /// <param name="handler">The interpolated content to write.</param>
    public void Write(bool isMultiline, [InterpolatedStringHandlerArgument("", nameof(isMultiline))] ref AppendInterpolatedStringHandler handler)
    {
        _ = this;
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
    /// <remarks>
    /// If the buffer is mid-line (does not end with a newline) and the buffer's last character
    /// is a brace (<c>{</c> or <c>}</c>), and the new content starts with whitespace (i.e. is
    /// indented as a fresh code statement), a newline is inserted before the content is
    /// processed. This prevents an indented code line from being jammed onto the same line as
    /// a structural brace from a previous emission. The whitespace check distinguishes "fresh
    /// indented statement" from "inline continuation" (e.g. emitting a GUID string like
    /// <c>{1234ABCD-...}</c> via consecutive single-character writes, where the bytes after
    /// <c>{</c> are not indented).
    /// </remarks>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline (normalizes <c>CRLF</c> -> <c>LF</c> and indents every line).</param>
    public void Write(scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (content.Length > 0
            && _buffer.Length > 0
            && (_buffer[^1] == '{' || _buffer[^1] == '}')
            && (isMultiline || content[0] == ' ' || content[0] == '\t'))
        {
            _ = _buffer.Append(DefaultNewLine);
        }

        if (isMultiline)
        {
            while (content.Length > 0)
            {
                int newLineIndex = content.IndexOf(DefaultNewLine);

                if (newLineIndex < 0)
                {
                    // No newline left -- write the rest as a single line.
                    WriteRawText(content);

                    // If the trailing chunk ends with a statement-terminator or block-closing
                    // character (`;`, `}`, `]`), append a newline so subsequent emissions start
                    // on a fresh line. This prevents a multi-line raw string ending mid-line
                    // (raw `"""..."""` strings never include a trailing newline) from getting
                    // jammed against the next single-line `Write` call. The character check
                    // is narrow enough to avoid breaking inline-continuation patterns where
                    // the multi-line content ends with `(`, `,`, `"`, `+`, etc. (where the
                    // next call is intended to concatenate on the same line).
                    if (content is [.., ';' or '}' or ']'])
                    {
                        WriteLine();
                    }
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
    /// <remarks>
    /// To prevent runs of blank lines and unnecessary blank lines immediately after an
    /// opening <c>{</c>, this method is idempotent: a newline is suppressed if the buffer
    /// already ends with a blank line (<c>\n\n</c>) or with <c>{\n</c>. This makes
    /// repeated <see cref="WriteLine()"/> calls (and multiline literals containing runs
    /// of blank lines) collapse to a single blank-line separator automatically.
    /// </remarks>
    public void WriteLine()
    {
        if (_buffer.Length > 0)
        {
            int j = _buffer.Length - 1;

            // Skip trailing spaces on the last line so the check ignores indentation
            // that may have been emitted speculatively for an empty line.
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

        _ = _buffer.Append(DefaultNewLine);
    }

    /// <summary>
    /// Writes a newline to the underlying buffer unconditionally (no idempotent
    /// collapsing). Reserved for the rare case where the caller wants to force a
    /// blank-line emission.
    /// </summary>
    public void WriteRawNewLine()
    {
        _ = _buffer.Append(DefaultNewLine);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer and appends a trailing newline.
    /// </summary>
    /// <remarks>
    /// If the buffer is mid-line (does not end with a newline) <em>and</em> the buffer's last
    /// character is a brace (<c>{</c> or <c>}</c>), a newline is inserted before
    /// <paramref name="content"/> is written so the line starts fresh. This prevents
    /// <see cref="WriteLine(string, bool)"/> from jamming structural content onto the previous
    /// line when the previous content was a multi-line raw string that ended with <c>{</c> or
    /// <c>}</c> (raw <c>"""..."""</c> strings never include a trailing newline before the
    /// closing token).
    /// </remarks>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteLine(string content, bool isMultiline = false)
    {
        WriteLine(content.AsSpan(), isMultiline);
    }

    /// <summary>
    /// Writes <paramref name="content"/> to the underlying buffer and appends a trailing newline.
    /// </summary>
    /// <remarks>
    /// If the buffer is mid-line (does not end with a newline) <em>and</em> the buffer's last
    /// character is a brace (<c>{</c> or <c>}</c>), a newline is inserted before
    /// <paramref name="content"/> is written so the line starts fresh. This prevents
    /// <see cref="WriteLine(string, bool)"/> from jamming structural content onto the previous
    /// line when the previous content was a multi-line raw string that ended with <c>{</c> or
    /// <c>}</c> (raw <c>"""..."""</c> strings never include a trailing newline before the
    /// closing token).
    /// </remarks>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">When <see langword="true"/>, treats <paramref name="content"/> as multiline.</param>
    public void WriteLine(scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (_buffer.Length > 0 && (_buffer[^1] == '}' || _buffer[^1] == '{'))
        {
            _ = _buffer.Append(DefaultNewLine);
        }

        Write(content, isMultiline);
        WriteLine();
    }

    /// <summary>
    /// Writes a newline if <paramref name="condition"/> is <see langword="true"/>.
    /// </summary>
    /// <param name="condition">When <see langword="true"/>, writes a newline; otherwise this call is a no-op.</param>
    public void WriteLineIf(bool condition)
    {
        if (condition)
        {
            WriteLine();
        }
    }

    /// <summary>
    /// Writes <paramref name="content"/> followed by a newline if <paramref name="condition"/> is <see langword="true"/>.
    /// </summary>
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

    /// <summary>
    /// Writes <paramref name="content"/> followed by a newline if <paramref name="condition"/> is <see langword="true"/>.
    /// </summary>
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
        _ = _buffer.Clear();
        return text;
    }

    /// <summary>
    /// Returns the current buffer contents without modifying the buffer.
    /// </summary>
    public override string ToString() => _buffer.ToString();

    /// <summary>
    /// Gets the current length (in chars) of the underlying buffer.
    /// </summary>
    public int Length => _buffer.Length;

    /// <summary>
    /// Returns the last character written to the buffer, or <c>'\0'</c> if the buffer is empty.
    /// </summary>
    public char Back() => _buffer.Length == 0 ? '\0' : _buffer[^1];

    /// <summary>
    /// Returns the contents of a substring of the buffer (used for capture-and-restore patterns).
    /// </summary>
    /// <param name="startIndex">The starting position.</param>
    /// <param name="length">The length of the substring to return.</param>
    /// <returns>The substring of the buffer at the requested position.</returns>
    public string GetSubstring(int startIndex, int length) => _buffer.ToString(startIndex, length);

    /// <summary>
    /// Removes a range of characters from the buffer.
    /// </summary>
    /// <param name="startIndex">The starting position to remove.</param>
    /// <param name="length">The number of characters to remove.</param>
    public void Remove(int startIndex, int length) => _buffer.Remove(startIndex, length);

    /// <summary>
    /// Sets the indent level back to zero (for emergency reset; rarely needed).
    /// </summary>
    public void ResetIndent()
    {
        CurrentIndentLevel = 0;
        _currentIndentation = _availableIndentations[0];
    }

    /// <summary>
    /// Clears the underlying buffer and resets the current indentation level back to zero.
    /// Used by <see cref="IndentedTextWriterPool.GetOrCreate"/> to recycle a returned writer
    /// before handing it back out, so callers always observe a fully reset writer regardless
    /// of how the previous lease left it.
    /// </summary>
    public void Clear()
    {
        _ = _buffer.Clear();
        ResetIndent();
    }

    /// <summary>
    /// Flushes the current buffer to <paramref name="path"/> (skipping the write if the file
    /// already exists with identical content), then clears the buffer.
    /// </summary>
    /// <remarks>
    /// If the destination file exists but cannot be read (e.g. due to a transient I/O failure or
    /// access denial), the catch block silently falls through to a fresh write. This is the
    /// intended behavior for a build tool: the worst case is an extra write of identical content
    /// that the OS will then re-permit; the alternative (failing the build) would create
    /// brittleness around incidental file-system noise.
    /// </remarks>
    /// <param name="path">The destination file path.</param>
    public void FlushToFile(string path)
    {
        string content = _buffer.ToString();

        if (File.Exists(path))
        {
            try
            {
                if (File.ReadAllText(path) == content)
                {
                    _ = _buffer.Clear();
                    return;
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Intentional: see <remarks/> -- a failed read falls through to a fresh write.
            }
        }

        File.WriteAllText(path, content);
        _ = _buffer.Clear();
    }

    /// <summary>
    /// Flushes the current buffer contents to a string (without trimming) and clears the buffer.
    /// </summary>
    /// <returns>The full buffer contents, untrimmed.</returns>
    public string FlushToString()
    {
        string text = _buffer.ToString();
        _ = _buffer.Clear();
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
            _ = _buffer.Append(_currentIndentation);
        }

        _ = _buffer.Append(content);
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
