// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using ConsoleAppFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    private const string ProjectionAssemblyName = "WinRT.Projection";

    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="processingState">The state from the processing phase.</param>
    private static void Emit(ProjectionGeneratorArgs args, ProjectionGeneratorProcessingState processingState)
    {
        CSharpCompilation compilation;

        // Create the Roslyn compilation from the generated projection sources
        try
        {
            // Parse the source files into syntax trees
            List<SyntaxTree> syntaxTrees = [];

            ConsoleApp.Log("Generating syntax trees");

            foreach (string file in Directory.GetFiles(processingState.SourcesFolder, "*.cs"))
            {
                args.Token.ThrowIfCancellationRequested();

                ConsoleApp.Log($"Generating syntax tree for {file}");

                using Stream stream = File.OpenRead(file);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(SourceText.From(stream), path: file));
            }

            // Build the references list
            List<MetadataReference> references = [];

            ConsoleApp.Log("Building reference list");

            foreach (string refPath in processingState.ReferencesWithoutProjections)
            {
                references.Add(MetadataReference.CreateFromFile(refPath));
            }

            args.Token.ThrowIfCancellationRequested();

            ConsoleApp.Log("Starting compilation");

            // Create the compilation
            compilation = CSharpCompilation.Create(
                ProjectionAssemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: true,
                    optimizationLevel: OptimizationLevel.Release,
                    deterministic: true,
                    generalDiagnosticOption: ReportDiagnostic.Info));
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.CreateCompilationError(e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Emit the projection .dll to disk
        try
        {
            ConsoleApp.Log("Writing projection dll");

            // Configure emit options for embedded symbols
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.Embedded,
                includePrivateMembers: true);

            string projectionDllPath = Path.Combine(args.GeneratedAssemblyDirectory, ProjectionAssemblyName + ".dll");

            EmitResult result;

            // Emit the compilation to a file
            using (FileStream fileStream = new(projectionDllPath, FileMode.Create))
            {
                result = compilation.Emit(fileStream, options: emitOptions);
            }

            if (!result.Success)
            {
                throw WellKnownProjectionGeneratorExceptions.EmitDllError(result.Diagnostics);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.EmitDllError(e);
        }
    }
}