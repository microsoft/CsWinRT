// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    private static readonly string ProjectionAssemblyName = "WinRT.Projection";

    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void Emit(ProjectionGeneratorArgs args)
    {
        args.Token.ThrowIfCancellationRequested();

        string sourcesFolder = GenerateSources(args, out HashSet<string> projectionReferenceAssemblies);

        args.Token.ThrowIfCancellationRequested();

        string[] referencesWithoutProjections = [.. args.ReferenceAssemblyPaths.Where(r => !projectionReferenceAssemblies.Contains(r))];

        CSharpCompilation compilation = CreateCompilationForProjection(sourcesFolder, referencesWithoutProjections);

        args.Token.ThrowIfCancellationRequested();

        string projectionDllPath = Path.Combine(args.GeneratedAssemblyDirectory, ProjectionAssemblyName + ".dll");
        SaveDll(compilation, projectionDllPath);
    }

    private static CSharpCompilation CreateCompilationForProjection(string sourcesFolder, string[] referencePaths)
    {
        try
        {
            // Parse the source files into a syntax tree
            List<SyntaxTree> syntaxTrees = [];
            foreach (string file in Directory.GetFiles(sourcesFolder, "*.cs"))
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file));
            }

            // Build references list
            List<MetadataReference> references = [];

            foreach (string refPath in referencePaths)
            {
                references.Add(MetadataReference.CreateFromFile(refPath));
            }

            // Create the compilation
            CSharpCompilation compilation = CSharpCompilation.Create(
                ProjectionAssemblyName,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            return compilation;
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.CreateCompilationError(e);
        }
    }

    private static void SaveDll(CSharpCompilation compilation, string dllPath)
    {
        try
        {
            // Emit the compilation to a file
            using FileStream fileStream = new(dllPath, FileMode.Create);
            EmitResult result = compilation.Emit(fileStream);

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