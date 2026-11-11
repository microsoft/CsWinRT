// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        List<EmbeddedText> embeddedTexts = [];

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

                string sourcePath = "/_/" + Path.GetFileName(file);
                SourceText sourceText = SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256, canBeEmbedded: true);

                embeddedTexts.Add(EmbeddedText.FromSource(sourcePath, sourceText));
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                    text: sourceText,
                    path: sourcePath));
            }

            // Build the references list
            List<MetadataReference> references = [];

            foreach (string refPath in processingState.CompilationReferences)
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
                result = compilation.Emit(fileStream, options: emitOptions, embeddedTexts: embeddedTexts);
            }

            if (!result.Success)
            {
                DeleteProjectionFiles(args);

                throw WellKnownProjectionGeneratorExceptions.EmitDllError(result.Diagnostics);
            }

            // MSBuild reads this list to track the same dependency closure as the compilation
            string referencesPath = Path.Combine(args.GeneratedAssemblyDirectory, assemblyName + ".references");
            string referencesText = string.Join(Environment.NewLine, processingState.CompilationReferences.Select(Path.GetFullPath)) + Environment.NewLine;

            if (!File.Exists(referencesPath) || File.ReadAllText(referencesPath) != referencesText)
            {
                File.WriteAllText(referencesPath, referencesText);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            DeleteProjectionFiles(args);

            throw WellKnownProjectionGeneratorExceptions.EmitDllError(e);
        }
    }

    /// <summary>
    /// Removes a generated projection and its compilation-reference manifest.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void DeleteProjectionFiles(ProjectionGeneratorArgs args)
    {
        File.Delete(Path.Combine(args.GeneratedAssemblyDirectory, args.AssemblyName + ".dll"));
        File.Delete(Path.Combine(args.GeneratedAssemblyDirectory, args.AssemblyName + ".references"));
    }
}