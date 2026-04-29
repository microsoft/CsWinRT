// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// The public key for CsWinRT assemblies, used for delay signing projection DLLs.
    /// </summary>
    private static readonly ImmutableArray<byte> CsWinRTPublicKey = [0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xB5, 0xFC, 0x90, 0xE7, 0x02, 0x7F, 0x67, 0x87, 0x1E, 0x77, 0x3A, 0x8F, 0xDE, 0x89, 0x38, 0xC8, 0x1D, 0xD4, 0x02, 0xBA, 0x65, 0xB9, 0x20, 0x1D, 0x60, 0x59, 0x3E, 0x96, 0xC4, 0x92, 0x65, 0x1E, 0x88, 0x9C, 0xC1, 0x3F, 0x14, 0x15, 0xEB, 0xB5, 0x3F, 0xAC, 0x11, 0x31, 0xAE, 0x0B, 0xD3, 0x33, 0xC5, 0xEE, 0x60, 0x21, 0x67, 0x2D, 0x97, 0x18, 0xEA, 0x31, 0xA8, 0xAE, 0xBD, 0x0D, 0xA0, 0x07, 0x2F, 0x25, 0xD8, 0x7D, 0xBA, 0x6F, 0xC9, 0x0F, 0xFD, 0x59, 0x8E, 0xD4, 0xDA, 0x35, 0xE4, 0x4C, 0x39, 0x8C, 0x45, 0x43, 0x07, 0xE8, 0xE3, 0x3B, 0x84, 0x26, 0x14, 0x3D, 0xAE, 0xC9, 0xF5, 0x96, 0x83, 0x6F, 0x97, 0xC8, 0xF7, 0x47, 0x50, 0xE5, 0x97, 0x5C, 0x64, 0xE2, 0x18, 0x9F, 0x45, 0xDE, 0xF4, 0x6B, 0x2A, 0x2B, 0x12, 0x47, 0xAD, 0xC3, 0x65, 0x2B, 0xF5, 0xC3, 0x08, 0x05, 0x5D, 0xA9];

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
            // Parse the source files into syntax trees
            List<SyntaxTree> syntaxTrees = [];

            foreach (string file in Directory.GetFiles(processingState.SourcesFolder, "*.cs"))
            {
                args.Token.ThrowIfCancellationRequested();

                using Stream stream = File.OpenRead(file);

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256), path: file));
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
                    cryptoPublicKey: CsWinRTPublicKey,
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