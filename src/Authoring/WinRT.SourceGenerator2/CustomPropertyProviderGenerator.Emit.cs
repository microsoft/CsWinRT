// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.CodeAnalysis;
using WindowsRuntime.SourceGenerator.Models;

namespace WindowsRuntime.SourceGenerator;

/// <inheritdoc cref="CustomPropertyProviderGenerator"/>
public partial class CustomPropertyProviderGenerator
{
    /// <summary>
    /// Generation methods for <see cref="CustomPropertyProviderGenerator"/>.
    /// </summary>
    private static class Emit
    {
        /// <summary>
        /// Emits the <c>ICustomPropertyProvider</c> implementation for a given annotated type.
        /// </summary>
        /// <param name="context">The <see cref="SourceProductionContext"/> value to use.</param>
        /// <param name="info">The input <see cref="CustomPropertyProviderInfo"/> state to use.</param>
        public static void WriteCustomPropertyProviderImplementation(SourceProductionContext context, CustomPropertyProviderInfo info)
        {
            using IndentedTextWriter writer = new();

            // Emit the implementation on the annotated type
            info.TypeHierarchy.WriteSyntax(
                state: info,
                writer: writer,
                baseTypes: [info.FullyQualifiedCustomPropertyProviderInterfaceName],
                memberCallbacks: [
                    WriteCustomPropertyProviderType,
                    WriteCustomPropertyProviderGetCustomProperty,
                    WriteCustomPropertyProviderGetIndexedProperty,
                    WriteCustomPropertyProviderGetStringRepresentation]);

            // Add the source file for the annotated type
            context.AddSource($"{info.TypeHierarchy.FullyQualifiedMetadataName}.g.cs", writer.ToString());
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.Type</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderType(CustomPropertyProviderInfo info, IndentedTextWriter writer)
        {
            writer.WriteLine($"""
                /// <inheritdoc/>
                global::System.Type {info.FullyQualifiedCustomPropertyProviderInterfaceName}.Type => typeof({info.TypeHierarchy.Hierarchy[0].QualifiedName});
                """, isMultiline: true);
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.GetCustomProperty</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderGetCustomProperty(CustomPropertyProviderInfo info, IndentedTextWriter writer)
        {
            writer.WriteLine($"""
                /// <inheritdoc/>
                {info.FullyQualifiedCustomPropertyInterfaceName} {info.FullyQualifiedCustomPropertyProviderInterfaceName}.GetCustomProperty(string name)
                """, isMultiline: true);

            using (writer.WriteBlock())
            {
                // Fast-path if there are no non-indexer custom properties
                if (!info.CustomProperties.Any(static info => info.FullyQualifiedIndexerTypeName is null))
                {
                    writer.WriteLine("return null;");

                    return;
                }

                writer.WriteLine("return name switch");

                using (writer.WriteBlock())
                {
                    // Emit a switch case for each available property
                    foreach (CustomPropertyInfo propertyInfo in info.CustomProperties)
                    {
                        // Skip all indexer properties
                        if (propertyInfo.FullyQualifiedIndexerTypeName is not null)
                        {
                            continue;
                        }

                        // Return the cached property implementation for the current custom property
                        writer.WriteLine($"nameof({propertyInfo.Name}) => global::WindowsRuntime.Xaml.Generated.{info.TypeHierarchy.Hierarchy[0].QualifiedName}_{propertyInfo.Name}.Instance,");
                    }

                    // If there's no matching property, just return 'null'
                    writer.WriteLine("_ => null");
                }
            }
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.GetIndexedProperty</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderGetIndexedProperty(CustomPropertyProviderInfo info, IndentedTextWriter writer)
        {
            writer.WriteLine($"""
                /// <inheritdoc/>
                {info.FullyQualifiedCustomPropertyInterfaceName} {info.FullyQualifiedCustomPropertyProviderInterfaceName}.GetIndexedProperty(string name, global::System.Type type)
                """, isMultiline: true);

            using (writer.WriteBlock())
            {
                // Fast-path if there are no indexer custom properties
                if (!info.CustomProperties.Any(static info => info.FullyQualifiedIndexerTypeName is not null))
                {
                    writer.WriteLine("return null;");

                    return;
                }

                // Switch over the type of all available indexer properties
                foreach (CustomPropertyInfo propertyInfo in info.CustomProperties)
                {
                    // Skip all not indexer properties
                    if (propertyInfo.FullyQualifiedIndexerTypeName is null)
                    {
                        continue;
                    }

                    // If we have a match, return the cached property implementation for the current indexer
                    writer.WriteLine(skipIfPresent: true);
                    writer.WriteLine($$"""
                        if (type == typeof({{propertyInfo.FullyQualifiedIndexerTypeName}}))
                        {
                            return global::WindowsRuntime.Xaml.Generated.{{info.TypeHierarchy.Hierarchy[0].QualifiedName}}_{{propertyInfo.FullyQualifiedIndexerTypeName}}.Instance;
                        }
                        """, isMultiline: true);
                }

                // If there's no matching property, just return 'null'
                writer.WriteLine("return null;");
            }
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.GetStringRepresentation</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderGetStringRepresentation(CustomPropertyProviderInfo info, IndentedTextWriter writer)
        {
            writer.WriteLine($$"""
                /// <inheritdoc/>
                string {{info.FullyQualifiedCustomPropertyProviderInterfaceName}}.GetStringRepresentation()
                {
                    return ToString();
                }
                """, isMultiline: true);
        }
    }
}