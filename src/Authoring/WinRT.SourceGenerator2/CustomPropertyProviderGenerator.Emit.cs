// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            const int ApproximateTypeDeclarationLength = 2048;

            // Approximate a close enough starting length to reduce copies
            int approximateLiteralLength = ApproximateTypeDeclarationLength + (ApproximateTypeDeclarationLength * info.CustomProperties.Length);

            IndentedTextWriter writer = new(literalLength: approximateLiteralLength, formattedCount: 0);

            ReadOnlySpan<IndentedTextWriter.Callback<CustomPropertyProviderInfo>> memberCallbacks =
            [
                WriteCustomPropertyProviderType,
                WriteCustomPropertyProviderGetCustomProperty,
                WriteCustomPropertyProviderGetIndexedProperty,
                WriteCustomPropertyProviderGetStringRepresentation,
                WriteCustomPropertyImplementationTypes
            ];

            // Emit the implementation on the annotated type
            info.TypeHierarchy.WriteSyntax(
                state: info,
                writer: ref writer,
                baseTypes: [info.FullyQualifiedCustomPropertyProviderInterfaceName],
                memberCallbacks: info.CustomProperties.IsEmpty ? memberCallbacks[..^1] : memberCallbacks);

            // Add the source file for the annotated type
            context.AddSource($"{info.TypeHierarchy.FullyQualifiedMetadataName}.g.cs", writer.ToStringAndClear());
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.Type</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderType(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine("/// <inheritdoc/>");
            writer.WriteGeneratedAttributes(nameof(CustomPropertyProviderGenerator), includeNonUserCodeAttributes: false);
            writer.WriteLine($"global::System.Type {info.FullyQualifiedCustomPropertyProviderInterfaceName}.Type => typeof({info.TypeHierarchy.Hierarchy[0].QualifiedName});");
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.GetCustomProperty</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderGetCustomProperty(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine("/// <inheritdoc/>");
            writer.WriteGeneratedAttributes(nameof(CustomPropertyProviderGenerator), includeNonUserCodeAttributes: false);
            writer.WriteLine($"""
                {info.FullyQualifiedCustomPropertyInterfaceName} {info.FullyQualifiedCustomPropertyProviderInterfaceName}.GetCustomProperty(string name)
                """, isMultiline: true);

            using (writer.WriteBlock())
            {
                // Fast-path if there are no non-indexer custom properties
                if (!info.CustomProperties.Any(static info => !info.IsIndexer))
                {
                    writer.WriteLine("return null;");

                    return;
                }

                writer.WriteLine("return name switch");
                writer.WriteLine("{");
                writer.IncreaseIndent();

                // Emit a switch case for each available property
                foreach (CustomPropertyInfo propertyInfo in info.CustomProperties)
                {
                    if (propertyInfo.IsIndexer)
                    {
                        continue;
                    }

                    // Return the cached property implementation for the current custom property
                    writer.WriteLine($"nameof({propertyInfo.Name}) => {GetCustomPropertyImplementationTypeName(propertyInfo)}.Instance,");
                }

                // If there's no matching property, just return 'null'
                writer.WriteLine("_ => null");
                writer.DecreaseIndent();
                writer.WriteLine("};");
            }
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.GetIndexedProperty</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderGetIndexedProperty(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine("/// <inheritdoc/>");
            writer.WriteGeneratedAttributes(nameof(CustomPropertyProviderGenerator), includeNonUserCodeAttributes: false);
            writer.WriteLine($"""
                {info.FullyQualifiedCustomPropertyInterfaceName} {info.FullyQualifiedCustomPropertyProviderInterfaceName}.GetIndexedProperty(string name, global::System.Type type)
                """, isMultiline: true);

            using (writer.WriteBlock())
            {
                // Fast-path if there are no indexer custom properties
                if (!info.CustomProperties.Any(static info => info.IsIndexer))
                {
                    writer.WriteLine("return null;");

                    return;
                }

                // Switch over the type of all available indexer properties
                foreach (CustomPropertyInfo propertyInfo in info.CustomProperties)
                {
                    if (!propertyInfo.IsIndexer)
                    {
                        continue;
                    }

                    string implementationTypeName = GetCustomPropertyImplementationTypeName(propertyInfo);

                    // If we have a match, return the cached property implementation for the current indexer
                    writer.WriteLine(skipIfPresent: true);
                    writer.WriteLine($$"""
                        if (type == typeof({{propertyInfo.FullyQualifiedIndexerTypeNameForTypeOf}}))
                        {
                            return {{implementationTypeName}}.Instance;
                        }
                        """, isMultiline: true);
                }

                // If there's no matching property, just return 'null'
                writer.WriteLine(skipIfPresent: true);
                writer.WriteLine("return null;");
            }
        }

        /// <summary>
        /// Writes the <c>ICustomPropertyProvider.GetStringRepresentation</c> implementation.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyProviderGetStringRepresentation(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine("/// <inheritdoc/>");
            writer.WriteGeneratedAttributes(nameof(CustomPropertyProviderGenerator), includeNonUserCodeAttributes: false);
            writer.WriteLine($$"""
                string {{info.FullyQualifiedCustomPropertyProviderInterfaceName}}.GetStringRepresentation()
                {
                    return ToString();
                }
                """, isMultiline: true);
        }

        /// <summary>
        /// Writes the <c>ICustomProperty</c> implementation types.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteCustomPropertyImplementationTypes(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            // Nest helpers in the owner so they inherit all generic parameters and constraints
            for (int i = 0; i < info.CustomProperties.Length; i++)
            {
                // Ensure members are correctly separated by one line
                if (i > 0)
                {
                    writer.WriteLine();
                }

                CustomPropertyInfo propertyInfo = info.CustomProperties[i];

                // Generate the correct implementation types for normal properties or indexer properties
                if (propertyInfo.IsIndexer)
                {
                    WriteIndexedCustomPropertyImplementationType(info, propertyInfo, ref writer);
                }
                else
                {
                    WriteNonIndexedCustomPropertyImplementationType(info, propertyInfo, ref writer);
                }
            }
        }

        /// <summary>
        /// Gets the nested implementation type name for a custom property.
        /// </summary>
        /// <param name="propertyInfo">The custom property to get the implementation type name for.</param>
        /// <returns>The name of the nested implementation type.</returns>
        private static string GetCustomPropertyImplementationTypeName(CustomPropertyInfo propertyInfo)
        {
            return propertyInfo.IsIndexer
                ? $"__CustomIndexer_{propertyInfo.FullyQualifiedIndexerTypeName.Replace("global::", "").EscapeIdentifierName()}"
                : $"__CustomProperty_{propertyInfo.Name}";
        }

        /// <summary>
        /// Writes a single non indexed <c>ICustomProperty</c> implementation type.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="propertyInfo">The input <see cref="CustomPropertyInfo"/> instance for the property to generate the implementation type for.</param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteNonIndexedCustomPropertyImplementationType(CustomPropertyProviderInfo info, CustomPropertyInfo propertyInfo, ref IndentedTextWriter writer)
        {
            string userTypeName = info.TypeHierarchy.GetTypeNameInScope();
            string implementationTypeName = GetCustomPropertyImplementationTypeName(propertyInfo);

            // Emit a type as follows:
            //
            // private sealed class <IMPLEMENTATION_TYPE_NAME> : ICustomProperty
            writer.WriteLine($"""
                /// <summary>
                /// The <see cref="global::{info.FullyQualifiedCustomPropertyInterfaceName}"/> implementation for <see cref="{userTypeName}.{propertyInfo.Name}"/>.
                /// </summary>
                """, isMultiline: true);
            writer.WriteGeneratedAttributes(nameof(CustomPropertyProviderGenerator));
            writer.WriteLine($"private sealed class {implementationTypeName} : global::{info.FullyQualifiedCustomPropertyInterfaceName}");

            using (writer.WriteBlock())
            {
                // Emit all 'ICustomProperty' members for an indexer proprty, and the singleton field
                writer.WriteLine($$"""
                    /// <summary>
                    /// Gets the singleton <see cref="{{implementationTypeName}}"/> instance for this custom property.
                    /// </summary>
                    public static readonly {{implementationTypeName}} Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => {{propertyInfo.CanRead.ToString().ToLowerInvariant()}};

                    /// <inheritdoc/>
                    public bool CanWrite => {{propertyInfo.CanWrite.ToString().ToLowerInvariant()}};

                    /// <inheritdoc/>
                    public string Name => "{{propertyInfo.Name}}";

                    /// <inheritdoc/>
                    public global::System.Type Type => typeof({{propertyInfo.FullyQualifiedTypeNameForTypeOf}});
                    """, isMultiline: true);

                writer.WriteLine();

                // Emit 'GetValue' depending on whether the property is readable and whether it's static
                if (propertyInfo.CanRead && propertyInfo.IsStatic)
                {
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public object GetValue(object target)
                        {
                            return {{userTypeName}}.{{propertyInfo.Name}};
                        }
                        """, isMultiline: true);
                }
                else if (propertyInfo.CanRead)
                {
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public object GetValue(object target)
                        {
                            return (({{userTypeName}})target).{{propertyInfo.Name}};
                        }
                        """, isMultiline: true);
                }
                else
                {
                    writer.WriteLine("""
                        /// <inheritdoc/>
                        public object GetValue(object target)
                        {
                            throw new global::System.NotSupportedException();
                        }
                        """, isMultiline: true);
                }

                writer.WriteLine();

                // Emit 'SetValue' depending on whether the property is writable and whether it's static
                if (propertyInfo.CanWrite && propertyInfo.IsStatic)
                {
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public void SetValue(object target, object value)
                        {
                            {{userTypeName}}.{{propertyInfo.Name}} = ({{propertyInfo.FullyQualifiedTypeName}})value;
                        }
                        """, isMultiline: true);
                }
                else if (propertyInfo.CanWrite)
                {
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public void SetValue(object target, object value)
                        {
                            (({{userTypeName}})target).{{propertyInfo.Name}} = ({{propertyInfo.FullyQualifiedTypeName}})value;
                        }
                        """, isMultiline: true);
                }
                else
                {
                    writer.WriteLine("""
                        /// <inheritdoc/>
                        public void SetValue(object target, object value)
                        {
                            throw new global::System.NotSupportedException();
                        }
                        """, isMultiline: true);
                }

                // Emit the property accessors (indexer properties can only be instance properties)
                writer.WriteLine();
                writer.WriteLine("""                    
                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new global::System.NotSupportedException();
                    }
                    
                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new global::System.NotSupportedException();
                    }
                    """, isMultiline: true);
            }
        }

        /// <summary>
        /// Writes a single indexed <c>ICustomProperty</c> implementation type.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="propertyInfo">The input <see cref="CustomPropertyInfo"/> instance for the property to generate the implementation type for.</param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteIndexedCustomPropertyImplementationType(CustomPropertyProviderInfo info, CustomPropertyInfo propertyInfo, ref IndentedTextWriter writer)
        {
            string userTypeName = info.TypeHierarchy.GetTypeNameInScope();
            string indexerTypeName = propertyInfo.FullyQualifiedIndexerTypeName!.Replace("global::", "");
            string implementationTypeName = GetCustomPropertyImplementationTypeName(propertyInfo);

            // Emit the implementation type, same as above
            writer.WriteLine($"""
                /// <summary>
                /// The <see cref="global::{info.FullyQualifiedCustomPropertyInterfaceName}"/> implementation for <see cref="{userTypeName}"/>'s <see cref="{indexerTypeName}"/> indexer.
                /// </summary>
                """, isMultiline: true);
            writer.WriteGeneratedAttributes(nameof(CustomPropertyProviderGenerator));
            writer.WriteLine($"private sealed class {implementationTypeName} : global::{info.FullyQualifiedCustomPropertyInterfaceName}");

            using (writer.WriteBlock())
            {
                // Emit all 'ICustomProperty' members for a normal property, and the singleton field
                writer.WriteLine($$"""
                    /// <summary>
                    /// Gets the singleton <see cref="{{implementationTypeName}}"/> instance for this custom property.
                    /// </summary>
                    public static readonly {{implementationTypeName}} Instance = new();

                    /// <inheritdoc/>
                    public bool CanRead => {{propertyInfo.CanRead.ToString().ToLowerInvariant()}};

                    /// <inheritdoc/>
                    public bool CanWrite => {{propertyInfo.CanWrite.ToString().ToLowerInvariant()}};

                    /// <inheritdoc/>
                    public string Name => "this";

                    /// <inheritdoc/>
                    public global::System.Type Type => typeof({{propertyInfo.FullyQualifiedTypeNameForTypeOf}});
                    """, isMultiline: true);

                // This is an indexed property, so non indexed ones will always throw
                writer.WriteLine();
                writer.WriteLine($$"""
                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        throw new global::System.NotSupportedException();
                    }

                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new global::System.NotSupportedException();
                    }
                    """, isMultiline: true);

                // Emit the indexer property accessors, conditionally based on CanRead/CanWrite
                writer.WriteLine();

                if (propertyInfo.CanRead)
                {
                    writer.WriteLine($$"""                    
                        /// <inheritdoc/>
                        public object GetIndexedValue(object target, object index)
                        {
                            return (({{userTypeName}})target)[({{propertyInfo.FullyQualifiedIndexerTypeName}})index];
                        }
                        """, isMultiline: true);
                }
                else
                {
                    writer.WriteLine("""
                        /// <inheritdoc/>
                        public object GetIndexedValue(object target, object index)
                        {
                            throw new global::System.NotSupportedException();
                        }
                        """, isMultiline: true);
                }

                writer.WriteLine();

                if (propertyInfo.CanWrite)
                {
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public void SetIndexedValue(object target, object value, object index)
                        {
                            (({{userTypeName}})target)[({{propertyInfo.FullyQualifiedIndexerTypeName}})index] = ({{propertyInfo.FullyQualifiedTypeName}})value;
                        }
                        """, isMultiline: true);
                }
                else
                {
                    writer.WriteLine("""
                        /// <inheritdoc/>
                        public void SetIndexedValue(object target, object value, object index)
                        {
                            throw new global::System.NotSupportedException();
                        }
                        """, isMultiline: true);
                }
            }
        }
    }
}