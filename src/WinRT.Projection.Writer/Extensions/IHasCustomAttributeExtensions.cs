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
        /// Returns the number of custom attributes on the member matching the given
        /// <paramref name="ns"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="ns">The namespace of the attribute type.</param>
        /// <param name="name">The unqualified type name of the attribute.</param>
        /// <returns>The number of matching custom attributes.</returns>
        public int CountAttributes(string ns, string name)
        {
            int count = 0;

            foreach (CustomAttribute attribute in member.CustomAttributes)
            {
                if (attribute.Type?.IsTypeOf(ns, name) is true)
                {
                    count++;
                }
            }

            return count;
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

        /// <summary>
        /// Gets whether the member carries a <c>[Windows.Foundation.Metadata.Deprecated]</c> attribute.
        /// </summary>
        public bool IsDeprecated => member.HasWindowsFoundationMetadataAttribute("DeprecatedAttribute");

        /// <summary>
        /// Gets whether the member is marked as removed: it carries a
        /// <c>[Windows.Foundation.Metadata.Deprecated]</c> attribute whose <c>DeprecationType</c>
        /// is <c>Remove</c>. A removed member is omitted from the projection, while its ABI vtable
        /// slot is preserved (stubbed to return <c>E_NOTIMPL</c>) for binary compatibility.
        /// </summary>
        /// <remarks>
        /// <c>DeprecatedAttribute(string message, DeprecationType type, ...)</c>: the second fixed
        /// argument is the <c>DeprecationType</c> enum, where <c>Deprecate</c> is 0 and <c>Remove</c> is 1.
        /// </remarks>
        public bool IsRemoved =>
            member.GetWindowsFoundationMetadataAttribute("DeprecatedAttribute") is { Signature.FixedArguments: [_, { Element: int deprecationType }, ..] }
            && deprecationType == 1;

        /// <summary>
        /// Gets whether the member is deprecated but not removed (i.e. it is projected with an
        /// <c>[Obsolete]</c> attribute rather than being omitted).
        /// </summary>
        public bool IsDeprecatedNotRemoved => member.IsDeprecated && !member.IsRemoved;

        /// <summary>
        /// Gets the message from the member's <c>[Windows.Foundation.Metadata.Deprecated]</c>
        /// attribute (the first fixed argument), or <see langword="null"/> if the member is not
        /// deprecated or the attribute carries no message.
        /// </summary>
        /// <remarks>
        /// <c>DeprecatedAttribute(string message, ...)</c>: the first fixed argument is the message.
        /// AsmResolver returns <c>Utf8String</c> for string custom-attribute args, so it is converted.
        /// </remarks>
        public string? DeprecatedMessage =>
            member.GetWindowsFoundationMetadataAttribute("DeprecatedAttribute") is { Signature.FixedArguments: [{ Element: { } message }, ..] }
                ? message.ToString()
                : null;
    }
}