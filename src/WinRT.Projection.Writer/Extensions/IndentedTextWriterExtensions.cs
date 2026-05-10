// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter;

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
        /// Writes the C# global namespace prefix (<see cref="GlobalPrefix"/>) followed by
        /// <paramref name="typeName"/>. Convenience wrapper for the common
        /// <c>writer.Write(GlobalPrefix); writer.Write(typeName);</c> pattern.
        /// </summary>
        /// <param name="typeName">The fully-qualified type name to emit after the <c>global::</c> prefix.</param>
        public void WriteGlobal(string typeName)
        {
            writer.Write($"{GlobalPrefix}{typeName}");
        }

        /// <summary>
        /// Writes the fully-qualified ABI namespace prefix (<see cref="GlobalAbiPrefix"/>) followed
        /// by <paramref name="typeName"/>. Convenience wrapper for the common
        /// <c>writer.Write(GlobalAbiPrefix); writer.Write(typeName);</c> pattern.
        /// </summary>
        /// <param name="typeName">The dot-qualified type name to emit after the <c>global::ABI.</c> prefix.</param>
        public void WriteGlobalAbi(string typeName)
        {
            writer.Write($"{GlobalAbiPrefix}{typeName}");
        }

        /// <summary>
        /// Writes the projection-wide accessibility modifier (<c>"internal"</c> when
        /// <see cref="Settings.Internal"/> or <see cref="Settings.Embedded"/> is set; otherwise
        /// <c>"public"</c>) for an emitted top-level type.
        /// </summary>
        /// <param name="settings">The active projection settings.</param>
        public void WriteAccessibility(Settings settings)
        {
            writer.Write(settings.InternalAccessibility);
        }

        /// <summary>
        /// Writes a single C# attribute application of the form <c>[name(args)]</c> on its own
        /// line. When <paramref name="args"/> is <see langword="null"/> or empty, the parenthesis
        /// list is omitted.
        /// </summary>
        /// <param name="name">The unqualified attribute name (without the trailing <c>Attribute</c> suffix).</param>
        /// <param name="args">The argument list to render between parentheses, or <see langword="null"/> for no arguments.</param>
        public void WriteAttribute(string name, string? args = null)
        {
            if (string.IsNullOrEmpty(args))
            {
                writer.WriteLine($"[{name}]");
            }
            else
            {
                writer.WriteLine($"[{name}({args})]");
            }
        }

        /// <summary>
        /// Writes a fully-qualified reference to a well-known interface IID field
        /// (<c>global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_<paramref name="interfaceName"/></c>).
        /// </summary>
        /// <param name="interfaceName">The bare interface name (e.g. <c>"IInspectable"</c>).</param>
        public void WriteWellKnownIIDFieldRef(string interfaceName)
        {
            writer.Write($"global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_{interfaceName}");
        }
    }
}

