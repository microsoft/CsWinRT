// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="ITypeDescriptor"/>.
/// </summary>
internal static class ITypeDescriptorExtensions
{
    extension(ITypeDescriptor type)
    {
        /// <summary>
        /// Returns the namespace and name of the type as a tuple, with both fields
        /// guaranteed to be non-<see langword="null"/>: a missing namespace becomes <see cref="string.Empty"/>
        /// and a missing name becomes <see cref="string.Empty"/>.
        /// </summary>
        /// <returns>A tuple of (namespace, name) with both fields non-<see langword="null"/>.</returns>
        public (string Namespace, string Name) Names()
        {
            return (type.Namespace ?? string.Empty, type.Name ?? string.Empty);
        }

        /// <summary>
        /// Returns the type's raw metadata namespace, falling back to <see cref="string.Empty"/> when
        /// the metadata namespace is <see langword="null"/>. More efficient than the <see cref="Names"/>
        /// tuple when only the namespace is needed (avoids allocating the name <see cref="string"/>).
        /// </summary>
        public string GetRawNamespace()
        {
            return type.Namespace ?? string.Empty;
        }

        /// <summary>
        /// Returns the type's raw metadata name, falling back to <see cref="string.Empty"/> when
        /// the metadata name is <see langword="null"/>. More efficient than the <see cref="Names"/>
        /// tuple when only the name is needed (avoids allocating the namespace <see cref="string"/>).
        /// </summary>
        public string GetRawName()
        {
            return type.Name ?? string.Empty;
        }
    }
}