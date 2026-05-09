// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Legacy character-stream writer surface inherited from the C++ port. Wraps a single
/// <see cref="IndentedTextWriter"/> internally so that text emitted through this surface
/// shares a buffer with code that uses <see cref="IndentedTextWriter"/> directly.
/// </summary>
/// <remarks>
/// <para>
/// During Pass 10 of the refactor this type acts as a "passthrough overload" -- existing
/// callers continue to use it (preserving the legacy <c>%</c>/<c>@</c>/<c>^</c> format
/// placeholder semantics + brace-tracked auto-indent), while migrated callers can switch
/// to <see cref="IndentedTextWriter"/> + <see cref="ProjectionEmitContext"/> directly via
/// <see cref="Writer"/>. Once every writer family has migrated, this type will be deleted
/// (Pass 10c).
/// </para>
/// <para>
/// Brace-tracked auto-indent: when this writer sees <c>{</c> followed by <c>\n</c> it calls
/// <see cref="IndentedTextWriter.IncreaseIndent"/> on the underlying writer; when it sees
/// <c>}</c> it calls <see cref="IndentedTextWriter.DecreaseIndent"/>. This preserves the
/// historical "no leading whitespace in source strings" convention from the C++ port without
/// requiring callers to use the <c>WriteBlock</c> API yet (Pass 15 will do that sweep).
/// </para>
/// </remarks>
internal class TextWriter
{
    /// <summary>The underlying writer all emission flows through.</summary>
    protected readonly IndentedTextWriter _writer;

    /// <summary>Whether to apply brace-auto-indent + per-line indentation. Disabled inside <see cref="WriteTemp(string, object[])"/>.</summary>
    private bool _enableIndent = true;

    /// <summary>Brace-state tracker for auto-indent.</summary>
    private WriterState _state = WriterState.None;

    private enum WriterState
    {
        None,
        OpenParen,
        OpenParenNewline,
    }

    /// <summary>
    /// Initializes a new <see cref="TextWriter"/> with a fresh underlying buffer.
    /// </summary>
    public TextWriter()
        : this(new IndentedTextWriter())
    {
    }

    /// <summary>
    /// Initializes a new <see cref="TextWriter"/> wrapping the supplied <paramref name="writer"/>.
    /// All emission flows through the supplied writer.
    /// </summary>
    /// <param name="writer">The underlying <see cref="IndentedTextWriter"/> to write to.</param>
    public TextWriter(IndentedTextWriter writer)
    {
        _writer = writer;
    }

    /// <summary>Gets the underlying <see cref="IndentedTextWriter"/> (for migrated callers).</summary>
    public IndentedTextWriter Writer => _writer;

    /// <summary>Writes a literal string verbatim, with brace-auto-indent applied.</summary>
    /// <param name="value">The text to write.</param>
    public void Write(ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            WriteChar(value[i]);
        }
    }

    /// <summary>Writes a literal string verbatim, with brace-auto-indent applied.</summary>
    /// <param name="value">The text to write.</param>
    public void Write(string value)
    {
        Write(value.AsSpan());
    }

    /// <summary>Writes a single character, with brace-auto-indent applied.</summary>
    /// <param name="value">The character to write.</param>
    public void Write(char value)
    {
        WriteChar(value);
    }

    /// <summary>Writes the invariant decimal representation of an integer value.</summary>
    public void Write(int value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Writes the invariant decimal representation of an unsigned integer value.</summary>
    public void Write(uint value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Writes the invariant decimal representation of a long value.</summary>
    public void Write(long value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Writes the invariant decimal representation of an unsigned long value.</summary>
    public void Write(ulong value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Calls a writer callback with this instance.</summary>
    /// <param name="callback">The callback to invoke.</param>
    public void Write(Action<TextWriter> callback)
    {
        callback(this);
    }

    /// <summary>
    /// Writes a value boxed as <see cref="object"/>, dispatching on the runtime type.
    /// Supports <see cref="string"/>, <see cref="char"/>, <see cref="int"/>, <see cref="uint"/>,
    /// <see cref="long"/>, <see cref="ulong"/>, <see cref="Action{TextWriter}"/>, and falls back
    /// to <see cref="object.ToString()"/> for any other type.
    /// </summary>
    /// <param name="value">The value to write (or <see langword="null"/>, which writes nothing).</param>
    public virtual void WriteValue(object? value)
    {
        switch (value)
        {
            case null: break;
            case string s: Write(s); break;
            case char c: Write(c); break;
            case int i: Write(i); break;
            case uint ui: Write(ui); break;
            case long l: Write(l); break;
            case ulong ul: Write(ul); break;
            case Action<TextWriter> a: a(this); break;
            default: Write(value.ToString() ?? string.Empty); break;
        }
    }

    /// <summary>
    /// Writes a code identifier (default: same as <see cref="WriteValue"/>, but specific writers may override).
    /// </summary>
    /// <param name="value">The value to write as a code identifier.</param>
    public virtual void WriteCode(object? value)
    {
        if (value is string s)
        {
            WriteCode(s);
        }
        else
        {
            WriteValue(value);
        }
    }

    /// <summary>
    /// Writes a code identifier, stripping anything from a backtick onwards
    /// (matches the historical C++ writer's <c>writer.write_code</c> semantics).
    /// </summary>
    /// <param name="value">The identifier to write (with any generic-arity suffix stripped).</param>
    public virtual void WriteCode(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '`') { return; }
            WriteChar(c);
        }
    }

    /// <summary>
    /// Writes a format string with the legacy C++-style <c>%</c>/<c>@</c>/<c>^</c> placeholders:
    /// <list type="bullet">
    /// <item><description><c>%</c>: insert any value (calls <see cref="WriteValue"/>).</description></item>
    /// <item><description><c>@</c>: insert a code identifier (strips backticks via <see cref="WriteCode(string)"/>).</description></item>
    /// <item><description><c>^</c>: escape next character (usually <c>%</c>, <c>@</c>, or <c>^</c>).</description></item>
    /// </list>
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The arguments to substitute into <c>%</c>/<c>@</c> placeholders.</param>
    public void Write(string format, params object?[] args)
    {
        WriteSegment(format.AsSpan(), args, 0);
    }

    /// <summary>
    /// Writes formatted output into a temporary buffer (without indentation) and returns it as a string.
    /// Matches the historical C++ writer's <c>write_temp</c> semantics.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The arguments to substitute into <c>%</c>/<c>@</c> placeholders.</param>
    /// <returns>The formatted output as a string.</returns>
    public string WriteTemp(string format, params object?[] args)
    {
        bool restoreIndent = _enableIndent;
        _enableIndent = false;
        int sizeBefore = _writer.Length;

        WriteSegment(format.AsSpan(), args, 0);

        string result = _writer.GetSubstring(sizeBefore, _writer.Length - sizeBefore);
        _writer.Remove(sizeBefore, _writer.Length - sizeBefore);
        _enableIndent = restoreIndent;
        return result;
    }

    /// <summary>Internal recursive segment writer for the format string.</summary>
    private void WriteSegment(ReadOnlySpan<char> value, object?[] args, int argIndex)
    {
        while (!value.IsEmpty)
        {
            int offset = -1;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '^' || c == '%' || c == '@')
                {
                    offset = i;
                    break;
                }
            }

            if (offset < 0)
            {
                Write(value);
                return;
            }

            if (offset > 0)
            {
                Write(value[..offset]);
            }

            char placeholder = value[offset];
            if (placeholder == '^')
            {
                Debug.Assert(offset + 1 < value.Length, "Escape ^ must be followed by another character");
                Write(value[offset + 1]);
                value = value[(offset + 2)..];
            }
            else if (placeholder == '%')
            {
                Debug.Assert(argIndex < args.Length, "Format string references more args than provided");
                WriteValue(args[argIndex++]);
                value = value[(offset + 1)..];
            }
            else // '@'
            {
                Debug.Assert(argIndex < args.Length, "Format string references more args than provided");
                WriteCode(args[argIndex++]);
                value = value[(offset + 1)..];
            }
        }
    }

    /// <summary>Writes a single character with brace-auto-indent handling.</summary>
    private void WriteChar(char c)
    {
        // Normalize line endings: skip CR characters (we use LF only, matching IndentedTextWriter).
        if (c == '\r') { return; }

        if (_enableIndent)
        {
            // Brace-tracked auto-indent: drive the underlying IndentedTextWriter's indent state
            // before writing the character, so that line-leading indent is computed correctly.
            UpdateState(c);
        }

        // Write the character through the underlying IndentedTextWriter so its per-line indentation
        // is applied automatically. When _enableIndent is false (inside WriteTemp), we still write
        // through the writer but the indent state is left at zero by the caller.
        _writer.Write(c.ToString());
    }

    private void UpdateState(char c)
    {
        if (_state == WriterState.OpenParenNewline && c != ' ' && c != '\t')
        {
            _writer.IncreaseIndent();
        }

        switch (c)
        {
            case '{':
                _state = WriterState.OpenParen;
                break;
            case '}':
                if (_writer.CurrentIndentLevel > 0)
                {
                    _writer.DecreaseIndent();
                }
                _state = WriterState.None;
                break;
            case '\n':
                _state = _state == WriterState.OpenParen ? WriterState.OpenParenNewline : WriterState.None;
                break;
            default:
                _state = WriterState.None;
                break;
        }
    }

    /// <summary>Returns the last character written to the buffer (or <c>'\0'</c> if empty).</summary>
    public char Back() => _writer.Back();

    /// <summary>Flushes the current buffer to a string and clears it.</summary>
    public string FlushToString() => _writer.FlushToString();

    /// <summary>
    /// Flushes the current buffer to <paramref name="path"/>, skipping the write if the file
    /// already exists with identical content, then clears the buffer.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    public void FlushToFile(string path) => _writer.FlushToFile(path);
}
