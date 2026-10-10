// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using WindowsRuntime.Generator.Errors;
using WindowsRuntime.Generator.References;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="processingState">The state from the processing phase.</param>
    private static void Emit(ProjectionGeneratorArgs args, ProjectionGeneratorProcessingState processingState)
    {
        string assemblyName = args.AssemblyName;
        CSharpCompilation compilation;

        // Create the Roslyn compilation from the generated projection sources
        try
        {
            // Parse the source files into syntax trees. The paths are normalized to a stable synthetic
            // root and the files are sorted, so the emitted assembly does not depend on the (random)
            // temporary folder the sources were generated into. Combined with 'deterministic: true', this
            // makes the output a pure function of the inputs, which is what lets several projects that
            // generate the same authoring projection safely share one assembly identity.
            List<SyntaxTree> syntaxTrees = [];

            string[] sourceFiles = Directory.GetFiles(processingState.SourcesFolder, "*.cs");

            Array.Sort(sourceFiles, StringComparer.Ordinal);

            foreach (string file in sourceFiles)
            {
                args.Token.ThrowIfCancellationRequested();

                using Stream stream = File.OpenRead(file);

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                    SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256),
                    path: "/_/" + Path.GetFileName(file)));
            }

            // Build the references list
            List<MetadataReference> references = [];

            foreach (string refPath in processingState.ReferencesWithoutProjections)
            {
                references.Add(MetadataReference.CreateFromFile(refPath));
            }

            args.Token.ThrowIfCancellationRequested();

            // Create the compilation with delay signing so the output has
            // the same public key token as the forwarder/impl assemblies.
            compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    allowUnsafe: true,
                    optimizationLevel: OptimizationLevel.Release,
                    deterministic: true,
                    cryptoPublicKey: ImmutableCollectionsMarshal.AsImmutableArray(WellKnownPublicKeys.WindowsSdkProjection),
                    delaySign: true,
                    generalDiagnosticOption: ReportDiagnostic.Info));
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.CreateCompilationError(e);
        }

        args.Token.ThrowIfCancellationRequested();

        // Emit the projection .dll to disk
        string projectionDllPath = Path.Combine(args.GeneratedAssemblyDirectory, assemblyName + ".dll");

        try
        {
            // Configure emit options for embedded symbols
            EmitOptions emitOptions = new(
                debugInformationFormat: DebugInformationFormat.Embedded,
                includePrivateMembers: true);

            EmitResult result;

            // Emit the compilation to a file
            using (FileStream fileStream = new(projectionDllPath, FileMode.Create))
            {
                result = compilation.Emit(fileStream, options: emitOptions);
            }

            if (!result.Success)
            {
                File.Delete(projectionDllPath);

                throw WellKnownProjectionGeneratorExceptions.EmitDllError(result.Diagnostics);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            if (File.Exists(projectionDllPath))
            {
                File.Delete(projectionDllPath);
            }

            throw WellKnownProjectionGeneratorExceptions.EmitDllError(e);
        }
    }
}