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
            IndentedTextWriter writer = new(literalLength: 0, formattedCount: 0); // TODO: adjust the literal length

            // Emit the implementation on the annotated type
            info.TypeHierarchy.WriteSyntax(
                state: info,
                writer: ref writer,
                baseTypes: [info.FullyQualifiedCustomPropertyProviderInterfaceName],
                memberCallbacks: [
                    WriteCustomPropertyProviderType,
                    WriteCustomPropertyProviderGetCustomProperty,
                    WriteCustomPropertyProviderGetIndexedProperty,
                    WriteCustomPropertyProviderGetStringRepresentation]);

            // Emit the additional property implementation types, if needed
            WriteCustomPropertyImplementationTypes(info, ref writer);

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
        private static void WriteCustomPropertyProviderGetCustomProperty(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine($"""
                /// <inheritdoc/>
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

                using (writer.WriteBlock())
                {
                    // Emit a switch case for each available property
                    foreach (CustomPropertyInfo propertyInfo in info.CustomProperties)
                    {
                        if (propertyInfo.IsIndexer)
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
        private static void WriteCustomPropertyProviderGetIndexedProperty(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine($"""
                /// <inheritdoc/>
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
        private static void WriteCustomPropertyProviderGetStringRepresentation(CustomPropertyProviderInfo info, ref IndentedTextWriter writer)
        {
            writer.WriteLine($$"""
                /// <inheritdoc/>
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
            // If we have no custom properties, we don't need to emit any additional code
            if (info.CustomProperties.IsEmpty)
            {
                return;
            }

            // All generated types go in this well-known namespace
            writer.WriteLine();
            writer.WriteLine("namespace WindowsRuntime.Xaml.Generated");

            using (writer.WriteBlock())
            {
                // Using declarations for well-known types we can refer to directly
                writer.WriteLine("using global::System;");
                writer.WriteLine($"using global:{info.FullyQualifiedCustomPropertyProviderInterfaceName};");
                writer.WriteLine();

                // Write all custom property implementation types
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
        }

        /// <summary>
        /// Writes a single non indexed <c>ICustomProperty</c> implementation type.
        /// </summary>
        /// <param name="info"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='info']/node()"/></param>
        /// <param name="propertyInfo">The input <see cref="CustomPropertyInfo"/> instance for the property to generate the implementation type for.</param>
        /// <param name="writer"><inheritdoc cref="IndentedTextWriter.Callback{T}" path="/param[@name='writer']/node()"/></param>
        private static void WriteNonIndexedCustomPropertyImplementationType(CustomPropertyProviderInfo info, CustomPropertyInfo propertyInfo, ref IndentedTextWriter writer)
        {
            string implementationTypeName = $"{info.TypeHierarchy.Hierarchy[0].QualifiedName}_{propertyInfo.Name}";

            // Emit a type as follows:
            //
            // file sealed class <IMPLEMENTATION_TYPE_NAME> : <CUSTOM_PROPERTY_INTERFACE_TYPE>
            writer.WriteLine($"file sealed class {implementationTypeName} : {info.FullyQualifiedCustomPropertyInterfaceName}");

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
                    public Type Type => typeof({{propertyInfo.FullyQualifiedTypeName}});
                    """, isMultiline: true);

                // Emit the right dispatching code depending on whether the property is static
                if (propertyInfo.IsStatic)
                {
                    writer.WriteLine();
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public object GetValue(object target)
                        {
                            return {{info.TypeHierarchy.GetFullyQualifiedTypeName()}}.{{propertyInfo.Name}};
                        }
                        
                        /// <inheritdoc/>
                        public void SetValue(object target, object value)
                        {
                            {{info.TypeHierarchy.GetFullyQualifiedTypeName()}}.{{propertyInfo.Name}} = ({{propertyInfo.FullyQualifiedTypeName}})value;
                        }
                        """, isMultiline: true);
                }
                else
                {
                    writer.WriteLine();
                    writer.WriteLine($$"""
                        /// <inheritdoc/>
                        public object GetValue(object target)
                        {
                            return (({{info.TypeHierarchy.GetFullyQualifiedTypeName()}})target).{{propertyInfo.Name}};
                        }
                        
                        /// <inheritdoc/>
                        public void SetValue(object target, object value)
                        {
                            (({{info.TypeHierarchy.GetFullyQualifiedTypeName()}})target).{{propertyInfo.Name}} = ({{propertyInfo.FullyQualifiedTypeName}})value;
                        }
                        """, isMultiline: true);
                }

                // Emit the property accessors (indexer properties can only be instance properties)
                writer.WriteLine();
                writer.WriteLine("""                    
                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        throw new NotSupportedException();
                    }
                    
                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        throw new NotSupportedException();
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
            string implementationTypeName = $"{info.TypeHierarchy.Hierarchy[0].QualifiedName}_{propertyInfo.Name}";

            // Emit the implementation type, same as above
            writer.WriteLine($"file sealed class {implementationTypeName} : {info.FullyQualifiedCustomPropertyInterfaceName}");

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
                    public string Name => "{{propertyInfo.Name}}";

                    /// <inheritdoc/>
                    public Type Type => typeof({{propertyInfo.FullyQualifiedTypeName}});
                    """, isMultiline: true);

                // This is an indexed property, so non indexed ones will always throw
                writer.WriteLine();
                writer.WriteLine($$"""
                    /// <inheritdoc/>
                    public object GetValue(object target)
                    {
                        throw new NotSupportedException();
                    }
                        
                    /// <inheritdoc/>
                    public void SetValue(object target, object value)
                    {
                        throw new NotSupportedException();
                    }
                    """, isMultiline: true);

                // Emit the indexer property accessors (not supported)
                writer.WriteLine();
                writer.WriteLine($$"""                    
                    /// <inheritdoc/>
                    public object GetIndexedValue(object target, object index)
                    {
                        return (({{info.TypeHierarchy.GetFullyQualifiedTypeName()}})target)[({{propertyInfo.FullyQualifiedIndexerTypeName}})index];
                    }
                    
                    /// <inheritdoc/>
                    public void SetIndexedValue(object target, object value, object index)
                    {
                        (({{info.TypeHierarchy.GetFullyQualifiedTypeName()}})target)[({{propertyInfo.FullyQualifiedIndexerTypeName}})index] = ({{propertyInfo.FullyQualifiedTypeName}})value;
                    }
                    """, isMultiline: true);
            }
        }
    }
}