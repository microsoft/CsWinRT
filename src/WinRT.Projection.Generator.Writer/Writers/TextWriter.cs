// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Mirrors the C++ <c>indented_writer_base</c> in <c>text_writer.h</c>.
/// <para>
/// Supports the C++ format placeholders:
/// <list type="bullet">
///   <item><description><c>%</c>: Insert any value (calls <see cref="WriteValue"/>).</description></item>
///   <item><description><c>@</c>: Insert a code identifier (strips backticks, escapes invalid chars).</description></item>
///   <item><description><c>^</c>: Escape next character (usually <c>%</c>, <c>@</c>, or <c>^</c>).</description></item>
/// </list>
/// Indentation is automatically managed: <c>{</c> increases indent by 4 spaces, <c>}</c> decreases.
/// </para>
/// </summary>
internal class TextWriter
{
    private const int TabWidth = 4;

    private enum WriterState
    {
        None,
        OpenParen,
        OpenParenNewline,
    }

    protected readonly List<char> _first = new(16 * 1024);
    private readonly List<char> _second = new();

    private WriterState _state = WriterState.None;
    private readonly List<int> _scopes = new() { 0 };
    private int _indent;
    private bool _enableIndent = true;

    /// <summary>Writes a literal string verbatim (with indentation handling).</summary>
    public void Write(ReadOnlySpan<char> value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            WriteChar(value[i]);
        }
    }

    /// <summary>Writes a literal string verbatim (with indentation handling).</summary>
    public void Write(string value)
    {
        Write(value.AsSpan());
    }

    /// <summary>Writes a single character (with indentation handling).</summary>
    public void Write(char value)
    {
        WriteChar(value);
    }

    /// <summary>Writes an integer value.</summary>
    public void Write(int value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Writes an unsigned integer value.</summary>
    public void Write(uint value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Writes a long value.</summary>
    public void Write(long value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Writes an unsigned long value.</summary>
    public void Write(ulong value) => Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Calls a writer callback.</summary>
    public void Write(Action<TextWriter> callback)
    {
        callback(this);
    }

    /// <summary>Writes a value boxed as object.</summary>
    public virtual void WriteValue(object? value)
    {
        switch (value)
        {
            case null:
                break;
            case string s:
                Write(s);
                break;
            case char c:
                Write(c);
                break;
            case int i:
                Write(i);
                break;
            case uint ui:
                Write(ui);
                break;
            case long l:
                Write(l);
                break;
            case ulong ul:
                Write(ul);
                break;
            case Action<TextWriter> a:
                a(this);
                break;
            default:
                Write(value.ToString() ?? string.Empty);
                break;
        }
    }

    /// <summary>Writes a code identifier (default: same as WriteValue, but specific writers may override).</summary>
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

    /// <summary>Writes a code identifier, stripping anything from a backtick onwards (matches C++ writer.write_code).</summary>
    public virtual void WriteCode(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '`')
            {
                return;
            }
            WriteChar(c);
        }
    }

    /// <summary>Writes a format string, with C++-style %/@/^ placeholders.</summary>
    public void Write(string format, params object?[] args)
    {
        WriteSegment(format.AsSpan(), args, 0);
    }

    /// <summary>Writes formatted string into temporary buffer and returns it (matches C++ write_temp).</summary>
    public string WriteTemp(string format, params object?[] args)
    {
        bool restoreIndent = _enableIndent;
        _enableIndent = false;
        int sizeBefore = _first.Count;

        WriteSegment(format.AsSpan(), args, 0);

        string result = new string(CollectionsMarshalSpan(_first, sizeBefore, _first.Count - sizeBefore));
        _first.RemoveRange(sizeBefore, _first.Count - sizeBefore);
        _enableIndent = restoreIndent;
        return result;
    }

    private static char[] CollectionsMarshalSpan(List<char> list, int start, int length)
    {
        char[] arr = new char[length];
        for (int i = 0; i < length; i++)
        {
            arr[i] = list[start + i];
        }
        return arr;
    }

    /// <summary>Internal recursive segment writer.</summary>
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

            // Write everything up to the placeholder
            if (offset > 0)
            {
                Write(value.Slice(0, offset));
            }

            char placeholder = value[offset];
            if (placeholder == '^')
            {
                Debug.Assert(offset + 1 < value.Length, "Escape ^ must be followed by another character");
                Write(value[offset + 1]);
                value = value.Slice(offset + 2);
            }
            else if (placeholder == '%')
            {
                Debug.Assert(argIndex < args.Length, "Format string references more args than provided");
                WriteValue(args[argIndex++]);
                value = value.Slice(offset + 1);
            }
            else // '@'
            {
                Debug.Assert(argIndex < args.Length, "Format string references more args than provided");
                WriteCode(args[argIndex++]);
                value = value.Slice(offset + 1);
            }
        }
    }

    /// <summary>Writes a single character with indentation handling.</summary>
    private void WriteChar(char c)
    {
        if (_enableIndent)
        {
            UpdateState(c);
            if (_first.Count > 0 && _first[^1] == '\n' && c != '\n')
            {
                WriteIndent();
            }
        }
        _first.Add(c);
    }

    private void WriteIndent()
    {
        for (int i = 0; i < _indent; i++)
        {
            _first.Add(' ');
        }
    }

    private void UpdateState(char c)
    {
        if (_state == WriterState.OpenParenNewline && c != ' ' && c != '\t')
        {
            _scopes[^1] = TabWidth;
            _indent += TabWidth;
        }

        switch (c)
        {
            case '{':
                _state = WriterState.OpenParen;
                _scopes.Add(0);
                break;
            case '}':
                _state = WriterState.None;
                _indent -= _scopes[^1];
                _scopes.RemoveAt(_scopes.Count - 1);
                break;
            case '\n':
                _state = _state == WriterState.OpenParen ? WriterState.OpenParenNewline : WriterState.None;
                break;
            default:
                _state = WriterState.None;
                break;
        }
    }

    /// <summary>Returns the last character written (or '\0').</summary>
    public char Back()
    {
        return _first.Count == 0 ? '\0' : _first[^1];
    }

    /// <summary>Swaps the primary and secondary buffers.</summary>
    public void Swap()
    {
        // Use a temp list since we can't swap List<T> contents directly
        char[] tmpArr = new char[_first.Count];
        _first.CopyTo(tmpArr);
        _first.Clear();
        _first.AddRange(_second);
        _second.Clear();
        _second.AddRange(tmpArr);
    }

    /// <summary>Flushes both buffers to a string and clears them.</summary>
    public string FlushToString()
    {
        StringBuilder sb = new(_first.Count + _second.Count);
        for (int i = 0; i < _first.Count; i++) { sb.Append(_first[i]); }
        for (int i = 0; i < _second.Count; i++) { sb.Append(_second[i]); }
        _first.Clear();
        _second.Clear();
        return sb.ToString();
    }

    /// <summary>Flushes both buffers to a file and clears them; only writes if file content differs.</summary>
    public void FlushToFile(string path)
    {
        // Build the full content
        char[] arr = new char[_first.Count + _second.Count];
        _first.CopyTo(arr);
        _second.CopyTo(arr, _first.Count);
        string content = new string(arr);

        if (FileEqual(path, content))
        {
            _first.Clear();
            _second.Clear();
            return;
        }

        File.WriteAllText(path, content);
        _first.Clear();
        _second.Clear();
    }

    private static bool FileEqual(string path, string content)
    {
        if (!File.Exists(path)) return false;
        try
        {
            string existing = File.ReadAllText(path);
            return existing == content;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Flushes to console out (matches C++ flush_to_console).</summary>
    public void FlushToConsole()
    {
        for (int i = 0; i < _first.Count; i++) { Console.Write(_first[i]); }
        for (int i = 0; i < _second.Count; i++) { Console.Write(_second[i]); }
        _first.Clear();
        _second.Clear();
    }

    /// <summary>Flushes to console error (matches C++ flush_to_console_error).</summary>
    public void FlushToConsoleError()
    {
        for (int i = 0; i < _first.Count; i++) { Console.Error.Write(_first[i]); }
        for (int i = 0; i < _second.Count; i++) { Console.Error.Write(_second[i]); }
        _first.Clear();
        _second.Clear();
    }
}
