// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Extensions;

/// <summary>
/// General-purpose extension methods for <see cref="IndentedTextWriter"/> that capture
/// repeated emission micro-patterns (separator lists, well-known prefixes, etc.).
/// </summary>
internal static class IndentedTextWriterExtensions
{
    extension(IndentedTextWriter writer)
    {
        /// <summary>
        /// Writes each item in <paramref name="items"/> via <paramref name="writeItem"/>, with
        /// <paramref name="separator"/> emitted between consecutive items.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items to write.</param>
        /// <param name="separator">The separator string emitted between consecutive items (e.g. <c>", "</c>).</param>
        /// <param name="writeItem">A callback that emits a single item.</param>
        public void WriteSeparated<T>(IEnumerable<T> items, string separator, Action<IndentedTextWriter, T> writeItem)
        {
            bool first = true;
            foreach (T item in items)
            {
                if (!first) { writer.Write(separator); }
                writeItem(writer, item);
                first = false;
            }
        }

        /// <summary>
        /// Writes each item in <paramref name="items"/> via <paramref name="writeItem"/>, with
        /// <c>", "</c> emitted between consecutive items.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="items">The items to write.</param>
        /// <param name="writeItem">A callback that emits a single item.</param>
        public void WriteCommaSeparated<T>(IEnumerable<T> items, Action<IndentedTextWriter, T> writeItem)
        {
            writer.WriteSeparated(items, ", ", writeItem);
        }

        /// <summary>
        /// Writes the C# global namespace prefix (<see cref="References.ProjectionNames.GlobalPrefix"/>)
        /// followed by <paramref name="typeName"/>. Convenience wrapper for the common
        /// <c>writer.Write("global::"); writer.Write(typeName);</c> pattern.
        /// </summary>
        /// <param name="typeName">The fully-qualified type name to emit after the <c>global::</c> prefix.</param>
        public void WriteGlobal(string typeName)
        {
            writer.Write($"{References.ProjectionNames.GlobalPrefix}{typeName}");
        }

        /// <summary>
        /// Writes the fully-qualified ABI namespace prefix (<see cref="References.ProjectionNames.GlobalAbiPrefix"/>)
        /// followed by <paramref name="typeName"/>. Convenience wrapper for the common
        /// <c>writer.Write("global::ABI."); writer.Write(typeName);</c> pattern.
        /// </summary>
        /// <param name="typeName">The dot-qualified type name to emit after the <c>global::ABI.</c> prefix.</param>
        public void WriteGlobalAbi(string typeName)
        {
            writer.Write($"{References.ProjectionNames.GlobalAbiPrefix}{typeName}");
        }
    }
}