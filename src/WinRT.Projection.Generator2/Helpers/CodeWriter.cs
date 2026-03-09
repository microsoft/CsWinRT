// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;

namespace WindowsRuntime.ProjectionGenerator.Helpers;

/// <summary>
/// A helper type for building formatted C# code output with proper indentation.
/// Unlike <see cref="IndentedTextWriter"/>, this is a reference type that can safely
/// be passed to and used across multiple methods without value-copy issues.
/// </summary>
internal sealed class CodeWriter
{
    /// <summary>
    /// The default indentation (4 spaces).
    /// </summary>
    private const string DefaultIndentation = "    ";

    /// <summary>
    /// The underlying <see cref="StringBuilder"/> for the output.
    /// </summary>
    private readonly StringBuilder _builder = new(4096);

    /// <summary>
    /// The current indentation level.
    /// </summary>
    private int _indentLevel;

    /// <summary>
    /// Whether the next write should be preceded by indentation.
    /// </summary>
    private bool _needsIndent = true;

    /// <summary>
    /// Increases the current indentation level.
    /// </summary>
    public void IncreaseIndent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the current indentation level.
    /// </summary>
    public void DecreaseIndent()
    {
        _indentLevel--;
    }

    /// <summary>
    /// Writes a block opener (<c>{</c>), increases indentation, and returns a <see cref="Block"/>
    /// that will close the block when disposed.
    /// </summary>
    /// <returns>A <see cref="Block"/> that writes the closing <c>}</c> when disposed.</returns>
    public Block WriteBlock()
    {
        WriteLine("{");
        IncreaseIndent();

        return new Block(this);
    }

    /// <summary>
    /// Writes content to the underlying buffer.
    /// </summary>
    /// <param name="content">The content to write.</param>
    public void Write(string content)
    {
        WriteIndentIfNeeded();
        _ = _builder.Append(content);
    }

    /// <summary>
    /// Writes content to the underlying buffer.
    /// </summary>
    /// <param name="content">The content to write.</param>
    public void Write(ReadOnlySpan<char> content)
    {
        WriteIndentIfNeeded();
        _ = _builder.Append(content);
    }

    /// <summary>
    /// Writes a character to the underlying buffer.
    /// </summary>
    /// <param name="c">The character to write.</param>
    public void Write(char c)
    {
        WriteIndentIfNeeded();
        _ = _builder.Append(c);
    }

    /// <summary>
    /// Writes an interpolated string to the underlying buffer using a handler.
    /// </summary>
    /// <param name="handler">The interpolated string handler.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "The handler implicitly passes 'this' through the InterpolatedStringHandlerArgument attribute.")]
    public void Write([System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] ref WriteHandler handler)
    {
    }

    /// <summary>
    /// Writes a new line to the underlying buffer.
    /// </summary>
    /// <param name="skipIfPresent">Indicates whether to skip adding the line if there already is one.</param>
    public void WriteLine(bool skipIfPresent = false)
    {
        if (skipIfPresent && _builder.Length >= 2 && _builder[^1] == '\n' && _builder[^2] == '\n')
        {
            return;
        }

        _ = _builder.Append('\n');
        _needsIndent = true;
    }

    /// <summary>
    /// Writes content followed by a new line to the underlying buffer.
    /// </summary>
    /// <param name="content">The content to write.</param>
    public void WriteLine(string content)
    {
        Write(content);
        WriteLine();
    }

    /// <summary>
    /// Writes an interpolated string followed by a new line to the underlying buffer.
    /// </summary>
    /// <param name="handler">The interpolated string handler.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060", Justification = "The handler implicitly passes 'this' through the InterpolatedStringHandlerArgument attribute.")]
    public void WriteLine([System.Runtime.CompilerServices.InterpolatedStringHandlerArgument("")] ref WriteHandler handler)
    {
        WriteLine();
    }

    /// <summary>
    /// Writes multiline content to the underlying buffer, properly indenting each line.
    /// </summary>
    /// <param name="content">The content to write.</param>
    public void WriteMultiline(string content)
    {
        ReadOnlySpan<char> span = content.AsSpan();

        while (span.Length > 0)
        {
            int newLineIndex = span.IndexOf('\n');

            if (newLineIndex < 0)
            {
                if (!span.IsEmpty)
                {
                    Write(span);
                }

                break;
            }

            ReadOnlySpan<char> line = span[..newLineIndex];

            if (!line.IsEmpty)
            {
                Write(line);
            }

            WriteLine();
            span = span[(newLineIndex + 1)..];
        }
    }

    /// <summary>
    /// Returns the generated content as a string.
    /// </summary>
    /// <returns>The generated code content.</returns>
    public override string ToString()
    {
        return _builder.ToString().TrimEnd();
    }

    /// <summary>
    /// Writes the indentation prefix if needed (i.e., at the start of a new line).
    /// </summary>
    private void WriteIndentIfNeeded()
    {
        if (_needsIndent)
        {
            for (int i = 0; i < _indentLevel; i++)
            {
                _ = _builder.Append(DefaultIndentation);
            }

            _needsIndent = false;
        }
    }

    /// <summary>
    /// Represents an indented block that writes a closing brace when disposed.
    /// </summary>
    /// <param name="writer">The <see cref="CodeWriter"/> to write the closing brace to.</param>
    public readonly struct Block(CodeWriter writer) : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }

    /// <summary>
    /// Provides a handler used by the language compiler to append interpolated strings into <see cref="CodeWriter"/> instances.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Runtime.CompilerServices.InterpolatedStringHandler]
    public readonly ref struct WriteHandler
    {
        /// <summary>The associated <see cref="CodeWriter"/> to which to append.</summary>
        private readonly CodeWriter _writer;

        /// <summary>Creates a handler used to append an interpolated string.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions.</param>
        /// <param name="formattedCount">The number of interpolation expressions.</param>
        /// <param name="writer">The associated <see cref="CodeWriter"/> to which to append.</param>
        public WriteHandler(int literalLength, int formattedCount, CodeWriter writer)
        {
            _writer = writer;
        }

        /// <summary>Writes the specified string to the handler.</summary>
        /// <param name="value">The string to write.</param>
        public readonly void AppendLiteral(string value)
        {
            _writer.Write(value);
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public readonly void AppendFormatted(string? value)
        {
            if (value is not null)
            {
                _writer.Write(value);
            }
        }

        /// <summary>Writes the specified character span to the handler.</summary>
        /// <param name="value">The span to write.</param>
        public readonly void AppendFormatted(ReadOnlySpan<char> value)
        {
            _writer.Write(value);
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public readonly void AppendFormatted<T>(T? value)
        {
            if (value is not null)
            {
                _writer.Write(value.ToString()!);
            }
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public readonly void AppendFormatted<T>(T? value, string? format)
        {
            if (value is System.IFormattable formattable)
            {
                _writer.Write(formattable.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
            }
            else if (value is not null)
            {
                _writer.Write(value.ToString()!);
            }
        }
    }
}
