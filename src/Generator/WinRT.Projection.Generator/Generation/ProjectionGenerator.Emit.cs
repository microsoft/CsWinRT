// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using WindowsRuntime.Generator;
using WindowsRuntime.ProjectionGenerator.Errors;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    private static readonly string ProjectionDll = "WinRT.Projection.dll";

    /// <summary>
    /// Runs the emit logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    private static void Emit(ProjectionGeneratorArgs args)
    {
        args.Token.ThrowIfCancellationRequested();

        string sourcesFolder = GenerateSources(args);

        CSharpCompilation compilation = CreateCompilation(sourcesFolder, args.ReferenceAssemblyPaths);

        args.Token.ThrowIfCancellationRequested();

        SaveDll(compilation, args.GeneratedAssemblyDirectory);
    }

    private static CSharpCompilation CreateCompilation(string sourcesFolder, string[] referencePaths)
    {
        try
        {
            // Parse the source files into a syntax tree
            List<SyntaxTree> syntaxTrees = [];
            foreach (string file in Directory.GetFiles(sourcesFolder, "*.cs"))
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(file)));
            }

            // Build references list
            List<MetadataReference> references = [];

            foreach (string refPath in referencePaths)
            {
                references.Add(MetadataReference.CreateFromFile(refPath));
            }

            // Create the compilation
            CSharpCompilation compilation = CSharpCompilation.Create(
                ProjectionDll,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

            return compilation;
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionExceptions.CreateCompilationError(e);
        }
    }

    private static void SaveDll(CSharpCompilation compilation, string outputPath)
    {
        string dllPath = Path.Combine(outputPath, ProjectionDll);

        try
        {
            // Emit the compilation to a file
            using FileStream fileStream = new(dllPath, FileMode.Create);
            EmitResult result = compilation.Emit(fileStream);

            if (!result.Success)
            {
                throw WellKnownProjectionExceptions.EmitDllError(result.Diagnostics);
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionExceptions.EmitDllError(e);
        }
    }

    private static string GetDefaultSourceCode()
    {
        return @"
using System;

namespace GeneratedLibrary
{
    public class MyGeneratedClass
    {
        public string GetMessage()
        {
            return ""Hello from dynamically generated DLL!"";
        }
        
        public string GetPersonalizedMessage(string name)
        {
            return $""Hello {name}, this message comes from a dynamically generated DLL!"";
        }
        
        public string GetCurrentTime()
        {
            return $""Current time from generated DLL: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"";
        }
    }
}";
    }
}
