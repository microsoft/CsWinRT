// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from ComputeSharp.
// See: https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.SourceGeneration/Helpers/IndentedTextWriter.cs.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable IDE0060, IDE0290

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// A helper type to build sequences of values with pooled buffers.
/// </summary>
internal ref struct IndentedTextWriter
{
    /// <summary>
    /// The default indentation (4 spaces).
    /// </summary>
    private const string DefaultIndentation = "    ";

    /// <summary>
    /// The default new line (<c>'\n'</c>).
    /// </summary>
    private const char DefaultNewLine = '\n';

    /// <summary>
    /// The <see cref="DefaultInterpolatedStringHandler"/> instance that text will be written to.
    /// </summary>
    private DefaultInterpolatedStringHandler _handler;

    /// <summary>
    /// The current indentation level.
    /// </summary>
    private int _currentIndentationLevel;

    /// <summary>
    /// The current indentation, as text.
    /// </summary>
    private string _currentIndentation;

    /// <summary>
    /// The cached array of available indentations, as text.
    /// </summary>
    private string[] _availableIndentations;

    /// <summary>
    /// Creates a new <see cref="IndentedTextWriter"/> value with the specified parameters.
    /// </summary>
    /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
    /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
    public IndentedTextWriter(int literalLength, int formattedCount)
    {
        _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount);
        _currentIndentationLevel = 0;
        _currentIndentation = "";
        _availableIndentations = new string[4];
        _availableIndentations[0] = "";

        for (int i = 1, n = _availableIndentations.Length; i < n; i++)
        {
            _availableIndentations[i] = _availableIndentations[i - 1] + DefaultIndentation;
        }
    }

    /// <summary>
    /// Increases the current indentation level.
    /// </summary>
    public void IncreaseIndent()
    {
        _currentIndentationLevel++;

        if (_currentIndentationLevel == _availableIndentations.Length)
        {
            Array.Resize(ref _availableIndentations, _availableIndentations.Length * 2);
        }

        // Set both the current indentation and the current position in the indentations
        // array to the expected indentation for the incremented level (ie. one level more).
        _currentIndentation = _availableIndentations[_currentIndentationLevel]
            ??= _availableIndentations[_currentIndentationLevel - 1] + DefaultIndentation;
    }

    /// <summary>
    /// Decreases the current indentation level.
    /// </summary>
    public void DecreaseIndent()
    {
        _currentIndentationLevel--;
        _currentIndentation = _availableIndentations[_currentIndentationLevel];
    }

    /// <summary>
    /// Writes a block to the underlying buffer.
    /// </summary>
    /// <returns>A <see cref="Block"/> value to close the open block with.</returns>
    [UnscopedRef]
    public Block WriteBlock()
    {
        WriteLine("{");
        IncreaseIndent();

        return new(ref this);
    }

    /// <summary>
    /// Writes content to the underlying buffer.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void Write(string content, bool isMultiline = false)
    {
        Write(content.AsSpan(), isMultiline);
    }

    /// <summary>
    /// Writes content to the underlying buffer.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void Write(scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (isMultiline)
        {
            while (content.Length > 0)
            {
                int newLineIndex = content.IndexOf(DefaultNewLine);

                if (newLineIndex < 0)
                {
                    // There are no new lines left, so the content can be written as a single line
                    WriteRawText(content);

                    break;
                }
                else
                {
                    ReadOnlySpan<char> line = content[..newLineIndex];

                    // Write the current line (if it's empty, we can skip writing the text entirely).
                    // This ensures that raw multiline string literals with blank lines don't have
                    // extra whitespace at the start of those lines, which would otherwise happen.
                    WriteIf(!line.IsEmpty, line);
                    WriteLine();

                    // Move past the new line character (the result could be an empty span)
                    content = content[(newLineIndex + 1)..];
                }
            }
        }
        else
        {
            WriteRawText(content);
        }
    }

    /// <summary>
    /// Writes content to the underlying buffer.
    /// </summary>
    /// <param name="handler">The interpolated string handler with content to write.</param>
    [UnconditionalSuppressMessage("Performance", "CA1822", Justification = "This method implicitly passes 'this' to the input handler.")]
    public readonly void Write([InterpolatedStringHandlerArgument("")] scoped ref WriteInterpolatedStringHandler handler)
    {
    }

    /// <summary>
    /// Writes content to the underlying buffer.
    /// </summary>
    /// <param name="handler">The interpolated string handler with content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    [UnconditionalSuppressMessage("Performance", "CA1822", Justification = "This method implicitly passes 'this' to the input handler.")]
    public readonly void Write(scoped ref DefaultInterpolatedStringHandler handler, bool isMultiline)
    {
        Unsafe.AsRef(in this).Write(handler.Text, isMultiline);

        handler.Clear();
    }

    /// <summary>
    /// Writes content to the underlying buffer depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void WriteIf(bool condition, string content, bool isMultiline = false)
    {
        if (condition)
        {
            Write(content.AsSpan(), isMultiline);
        }
    }

    /// <summary>
    /// Writes content to the underlying buffer depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void WriteIf(bool condition, scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (condition)
        {
            Write(content, isMultiline);
        }
    }

    /// <summary>
    /// Writes content to the underlying buffer depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="handler">The interpolated string handler with content to write.</param>
    [UnconditionalSuppressMessage("Performance", "CA1822", Justification = "This method implicitly passes 'this' to the input handler.")]
    public readonly void WriteIf(bool condition, [InterpolatedStringHandlerArgument("", nameof(condition))] scoped ref WriteIfInterpolatedStringHandler handler)
    {
    }

    /// <summary>
    /// Writes a line to the underlying buffer.
    /// </summary>
    /// <param name="skipIfPresent">Indicates whether to skip adding the line if there already is one.</param>
    public void WriteLine(bool skipIfPresent = false)
    {
        if (skipIfPresent && _handler.Text is [.., '\n', '\n'])
        {
            return;
        }

        _handler.AppendFormatted(DefaultNewLine);
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void WriteLine(string content, bool isMultiline = false)
    {
        WriteLine(content.AsSpan(), isMultiline);
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void WriteLine(scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        Write(content, isMultiline);
        WriteLine();
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line.
    /// </summary>
    /// <param name="handler">The interpolated string handler with content to write.</param>
    public readonly void WriteLine([InterpolatedStringHandlerArgument("")] scoped ref WriteInterpolatedStringHandler handler)
    {
        Unsafe.AsRef(in this).WriteLine();
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line.
    /// </summary>
    /// <param name="handler">The interpolated string handler with content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public readonly void WriteLine(scoped ref DefaultInterpolatedStringHandler handler, bool isMultiline)
    {
        Unsafe.AsRef(in this).WriteLine(handler.Text, isMultiline);

        handler.Clear();
    }

    /// <summary>
    /// Writes a line to the underlying buffer depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="skipIfPresent">Indicates whether to skip adding the line if there already is one.</param>
    public void WriteLineIf(bool condition, bool skipIfPresent = false)
    {
        if (condition)
        {
            WriteLine(skipIfPresent);
        }
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void WriteLineIf(bool condition, string content, bool isMultiline = false)
    {
        if (condition)
        {
            WriteLine(content.AsSpan(), isMultiline);
        }
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="content">The content to write.</param>
    /// <param name="isMultiline">Whether the input content is multiline.</param>
    public void WriteLineIf(bool condition, scoped ReadOnlySpan<char> content, bool isMultiline = false)
    {
        if (condition)
        {
            Write(content, isMultiline);
            WriteLine();
        }
    }

    /// <summary>
    /// Writes content to the underlying buffer and appends a trailing new line depending on an input condition.
    /// </summary>
    /// <param name="condition">The condition to use to decide whether or not to write content.</param>
    /// <param name="handler">The interpolated string handler with content to write.</param>
    public readonly void WriteLineIf(bool condition, [InterpolatedStringHandlerArgument("", nameof(condition))] scoped ref WriteIfInterpolatedStringHandler handler)
    {
        if (condition)
        {
            Unsafe.AsRef(in this).WriteLine();
        }
    }

    /// <inheritdoc cref="DefaultInterpolatedStringHandler.ToStringAndClear"/>
    public string ToStringAndClear()
    {
        string text = _handler.Text.Trim().ToString();

        _handler.Clear();

        return text;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _handler.Clear();
    }

    /// <summary>
    /// Writes raw text to the underlying buffer, adding leading indentation if needed.
    /// </summary>
    /// <param name="content">The raw text to write.</param>
    private void WriteRawText(scoped ReadOnlySpan<char> content)
    {
        if (_handler.Text.Length == 0 || _handler.Text[^1] == DefaultNewLine)
        {
            _handler.AppendLiteral(_currentIndentation);
        }

        _handler.AppendFormatted(content);
    }

    /// <summary>
    /// A delegate representing a callback to write data into an <see cref="IndentedTextWriter"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of data to use.</typeparam>
    /// <param name="value">The input data to use to write into <paramref name="writer"/>.</param>
    /// <param name="writer">The <see cref="IndentedTextWriter"/> instance to write into.</param>
    public delegate void Callback<T>(T value, ref IndentedTextWriter writer);

    /// <summary>
    /// Represents an indented block that needs to be closed.
    /// </summary>
    /// <param name="writer">The input <see cref="IndentedTextWriter"/> instance to wrap.</param>
    public unsafe ref struct Block(ref IndentedTextWriter writer) : IDisposable
    {
        /// <summary>
        /// The <see cref="IndentedTextWriter"/> instance to write to.
        /// </summary>
        private IndentedTextWriter* _writer = (IndentedTextWriter*)Unsafe.AsPointer(ref writer);

        /// <inheritdoc/>
        public void Dispose()
        {
            ref IndentedTextWriter writer = ref *_writer;

            _writer = null;

            // We check the indentation as a way of knowing if we have reset the field before.
            // The field itself can't be 'null', but if we have assigned 'default' to it, then
            // that field will be 'null' even though it's always set from the constructor.
            if (writer._currentIndentation is not null)
            {
                writer.DecreaseIndent();
                writer.WriteLine("}");
            }
        }
    }

    /// <summary>
    /// Provides a handler used by the language compiler to append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteInterpolatedStringHandler
    {
        /// <summary>The associated <see cref="IndentedTextWriter"/> to which to append.</summary>
        private readonly IndentedTextWriter _writer;

        /// <summary>Creates a handler used to append an interpolated string into a <see cref="StringBuilder"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="writer">The associated <see cref="IndentedTextWriter"/> to which to append.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public WriteInterpolatedStringHandler(int literalLength, int formattedCount, IndentedTextWriter writer)
        {
            _writer = writer;
        }

        /// <summary>Writes the specified string to the handler.</summary>
        /// <param name="value">The string to write.</param>
        public void AppendLiteral(string value)
        {
            _writer.Write(value);
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value)
        {
            AppendFormatted<string?>(value);
        }

        /// <summary>Writes the specified character span to the handler.</summary>
        /// <param name="value">The span to write.</param>
        public void AppendFormatted(scoped ReadOnlySpan<char> value)
        {
            _writer.Write(value);
        }

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T? value)
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
        [UnconditionalSuppressMessage("Style", "IDE0038", Justification = "Not using pattern matching to avoid boxing.")]
        public void AppendFormatted<T>(T? value, string? format)
        {
            if (value is IFormattable)
            {
                _writer.Write(((IFormattable)value).ToString(format, CultureInfo.InvariantCulture));
            }
            else if (value is not null)
            {
                _writer.Write(value.ToString()!);
            }
        }
    }

    /// <summary>
    /// Provides a handler used by the language compiler to conditionally append interpolated strings into <see cref="IndentedTextWriter"/> instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [InterpolatedStringHandler]
    public readonly ref struct WriteIfInterpolatedStringHandler
    {
        /// <summary>The associated <see cref="WriteInterpolatedStringHandler"/> to use.</summary>
        private readonly WriteInterpolatedStringHandler _writer;

        /// <summary>Creates a handler used to append an interpolated string into a <see cref="StringBuilder"/>.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="writer">The associated <see cref="IndentedTextWriter"/> to which to append.</param>
        /// <param name="condition">The condition to use to decide whether or not to write content.</param>
        /// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public WriteIfInterpolatedStringHandler(int literalLength, int formattedCount, IndentedTextWriter writer, bool condition, out bool shouldAppend)
        {
            if (condition)
            {
                _writer = new WriteInterpolatedStringHandler(literalLength, formattedCount, writer);

                shouldAppend = true;
            }
            else
            {
                _writer = default;

                shouldAppend = false;
            }
        }

        /// <inheritdoc cref="WriteInterpolatedStringHandler.AppendLiteral(string)"/>
        public void AppendLiteral(string value)
        {
            _writer.AppendLiteral(value);
        }

        /// <inheritdoc cref="WriteInterpolatedStringHandler.AppendFormatted(string?)"/>
        public void AppendFormatted(string? value)
        {
            _writer.AppendFormatted(value);
        }

        /// <inheritdoc cref="WriteInterpolatedStringHandler.AppendFormatted(ReadOnlySpan{char})"/>
        public void AppendFormatted(scoped ReadOnlySpan<char> value)
        {
            _writer.AppendFormatted(value);
        }

        /// <inheritdoc cref="WriteInterpolatedStringHandler.AppendFormatted{T}(T)"/>
        public void AppendFormatted<T>(T? value)
        {
            _writer.AppendFormatted(value);
        }

        /// <inheritdoc cref="WriteInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
        public void AppendFormatted<T>(T? value, string? format)
        {
            _writer.AppendFormatted(value, format);
        }
    }
}