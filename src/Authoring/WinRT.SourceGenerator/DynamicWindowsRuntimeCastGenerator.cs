﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

#nullable enable

namespace Generator;

/// <summary>
/// A supporting generator for <see cref="RuntimeClassCastAnalyzer"/> and <see cref="RuntimeClassCastCodeFixer"/>.
/// </summary>
[Generator]
public sealed class DynamicWindowsRuntimeCastGenerator : IIncrementalGenerator
{
    /// <summary>
    /// The shared writer for all discovered type names.
    /// </summary>
    [ThreadStatic]
    private static ArrayBufferWriter<string>? typeNamesWriter;

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gather all target type names
        IncrementalValueProvider<ImmutableArray<string>> typeNames = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "WinRT.DynamicWindowsRuntimeCastAttribute",
            predicate: static (syntaxNode, _) => true,
            transform: static (context, token) =>
            {
                ArrayBufferWriter<string> typeNamesWriter = DynamicWindowsRuntimeCastGenerator.typeNamesWriter ??= [];

                typeNamesWriter.Clear();

                foreach (AttributeData attribute in context.Attributes)
                {
                    // Double check we have a target type
                    if (attribute.ConstructorArguments is not [{ Kind: TypedConstantKind.Type, IsNull: false, Value: INamedTypeSymbol typeSymbol }])
                    {
                        continue;
                    }

                    // Track the current type
                    typeNamesWriter.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }

                return typeNamesWriter.WrittenSpan.ToImmutableArray();
            })
            .SelectMany(static (value, token) => value)
            .Collect();

        // Generate the [DynamicDependency] attributes
        context.RegisterImplementationSourceOutput(typeNames, static (SourceProductionContext context, ImmutableArray<string> value) =>
        {
            if (value.IsEmpty)
            {
                return;
            }

            StringBuilder builder = new();

            builder.AppendLine("""
                // <auto-generated/>
                #pragma warning disable

                namespace WinRT
                {
                    using global::System.Runtime.CompilerServices;
                    using global::System.Diagnostics.CodeAnalysis;

                    /// <summary>
                    /// Roots projected types used in dynamic casts.
                    /// </summary>
                    internal static class DynamicWindowsRuntimeCastInitializer
                    {
                        /// <summary>
                        /// Roots all dependent RCW types.
                        /// </summary>
                        [ModuleInitializer]                    
                """);

            foreach (string typeName in value)
            {
                builder.Append("        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields, typeof(global::");
                builder.Append(typeName);
                builder.AppendLine("))]");
            }

            builder.Append("""
                        public static void InitializeDynamicWindowsRuntimeCast()
                        {
                        }
                    }
                }
                """);

            context.AddSource("DynamicWindowsRuntimeCastInitializer.g.cs", builder.ToString());
        });
    }
}

#endif
