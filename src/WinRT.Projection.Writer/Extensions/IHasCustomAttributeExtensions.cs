// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.References;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Extension methods for <see cref="IHasCustomAttribute"/>.
/// </summary>
internal static class IHasCustomAttributeExtensions
{
    extension(IHasCustomAttribute member)
    {
        /// <summary>
        /// Returns whether the member carries a custom attribute matching the given
        /// <paramref name="ns"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="ns">The namespace of the attribute type.</param>
        /// <param name="name">The unqualified type name of the attribute.</param>
        /// <returns><see langword="true"/> if a matching custom attribute is found; otherwise <see langword="false"/>.</returns>
        public bool HasAttribute(string ns, string name)
        {
            foreach (CustomAttribute attribute in member.CustomAttributes)
            {
                if (attribute.Type?.IsTypeOf(ns, name) is true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the matching custom attribute on the member, or <see langword="null"/>
        /// if none is found.
        /// </summary>
        /// <param name="ns">The namespace of the attribute type.</param>
        /// <param name="name">The unqualified type name of the attribute.</param>
        /// <returns>The matching custom attribute, or <see langword="null"/> if none is found.</returns>
        public CustomAttribute? GetAttribute(string ns, string name)
        {
            foreach (CustomAttribute attribute in member.CustomAttributes)
            {
                if (attribute.Type?.IsTypeOf(ns, name) is true)
                {
                    return attribute;
                }
            }

            return null;
        }

        /// <summary>
        /// Convenience for <c>HasAttribute(ns, name)</c> with the namespace fixed to
        /// <c>Windows.Foundation.Metadata</c>.
        /// </summary>
        /// <param name="name">The unqualified name of the <c>Windows.Foundation.Metadata</c> attribute.</param>
        public bool HasWindowsFoundationMetadataAttribute(string name)
        {
            return member.HasAttribute(WellKnownNamespaces.WindowsFoundationMetadata, name);
        }

        /// <summary>
        /// Convenience for <c>GetAttribute(ns, name)</c> with the namespace fixed to
        /// <c>Windows.Foundation.Metadata</c>.
        /// </summary>
        /// <param name="name">The unqualified name of the <c>Windows.Foundation.Metadata</c> attribute.</param>
        public CustomAttribute? GetWindowsFoundationMetadataAttribute(string name)
        {
            return member.GetAttribute(WellKnownNamespaces.WindowsFoundationMetadata, name);
        }
    }
}