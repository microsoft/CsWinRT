// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Ported from ComputeSharp.
// See: https://github.com/Sergio0694/ComputeSharp/blob/main/src/ComputeSharp.SourceGeneration/Helpers/IndentedTextWriter.cs.

using System;

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Extension methods for the <see cref="IndentedTextWriter"/> type.
/// </summary>
internal static class IndentedTextWriterExtensions
{
    /// <summary>
    /// Writes the following attributes into a target writer:
    /// <code>
    /// [global::System.CodeDom.Compiler.GeneratedCode("...", "...")]
    /// [global::System.Diagnostics.DebuggerNonUserCode]
    /// [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    /// </code>
    /// </summary>
    /// <param name="writer">The <see cref="IndentedTextWriter"/> instance to write into.</param>
    /// <param name="generatorName">The name of the generator.</param>
    /// <param name="useFullyQualifiedTypeNames">Whether to use fully qualified type names or not.</param>
    /// <param name="includeNonUserCodeAttributes">Whether to also include the attribute for non-user code.</param>
    public static void WriteGeneratedAttributes(
        this ref IndentedTextWriter writer,
        string generatorName,
        bool useFullyQualifiedTypeNames = true,
        bool includeNonUserCodeAttributes = true)
    {
        // We can use this class to get the assembly, as all files for generators are just included
        // via shared projects. As such, the assembly will be the same as the generator type itself.
        Version assemblyVersion = typeof(IndentedTextWriterExtensions).Assembly.GetName().Version!;

        if (useFullyQualifiedTypeNames)
        {
            writer.WriteLine($$"""[global::System.CodeDom.Compiler.GeneratedCode("{{generatorName}}", "{{assemblyVersion}}")]""");

            if (includeNonUserCodeAttributes)
            {
                writer.WriteLine($$"""[global::System.Diagnostics.DebuggerNonUserCode]""");
                writer.WriteLine($$"""[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]""");
            }
        }
        else
        {
            writer.WriteLine($$"""[GeneratedCode("{{generatorName}}", "{{assemblyVersion}}")]""");

            if (includeNonUserCodeAttributes)
            {
                writer.WriteLine($$"""[DebuggerNonUserCode]""");
                writer.WriteLine($$"""[ExcludeFromCodeCoverage]""");
            }
        }
    }
}