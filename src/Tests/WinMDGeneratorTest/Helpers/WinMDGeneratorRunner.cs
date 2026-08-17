// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace WinMDGeneratorTest.Helpers;

/// <summary>
/// Runs the WinMD generator (<c>cswinrtwinmdgen</c>) end-to-end and asserts its outcome.
/// </summary>
/// <remarks>
/// Each entry point compiles a small C# input (or builds a raw response file), runs the actual tool as
/// a separate process, and asserts the result. Tests should call <see cref="AssertSuccess"/> or one of
/// the <c>AssertFailure</c> overloads so each scenario stays a single call.
/// </remarks>
internal static class WinMDGeneratorRunner
{
    /// <summary>
    /// Asserts that the generator succeeds (exit code <c>0</c>) for the given component source.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use the UWP (<c>Windows.UI.Xaml</c>) projections.</param>
    public static void AssertSuccess(string source, bool useWindowsUIXamlProjections = false)
    {
        (int exitCode, string output) = Run(source, useWindowsUIXamlProjections);

        Assert.AreEqual(0, exitCode, output);
    }

    /// <summary>
    /// Asserts that the generator fails for the given component source, reporting the expected error.
    /// </summary>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="error">The expected error id (e.g. <c>"CSWINRTWINMDGEN0013"</c>) the output must contain.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use the UWP (<c>Windows.UI.Xaml</c>) projections.</param>
    public static void AssertFailure(string source, string error, bool useWindowsUIXamlProjections = false)
    {
        AssertFailureResult(Run(source, useWindowsUIXamlProjections), error);
    }

    /// <summary>
    /// Asserts that the generator fails for a caller-provided response file, reporting the expected error.
    /// </summary>
    /// <remarks>
    /// The <paramref name="responseFileFactory"/> receives a fresh temporary working directory and
    /// returns the full response file content to run with (one <c>--argument value</c> pair per line). It
    /// can use <see cref="CompileComponent"/> to produce an input assembly, or reference any other path
    /// under the directory (e.g. to test invalid inputs).
    /// </remarks>
    /// <param name="responseFileFactory">Builds the full response file content for a given temporary directory.</param>
    /// <param name="error">The expected error id (e.g. <c>"CSWINRTWINMDGEN0002"</c>) the output must contain.</param>
    public static void AssertFailure(Func<string, string> responseFileFactory, string error)
    {
        AssertFailureResult(Run(responseFileFactory), error);
    }

    /// <summary>
    /// Asserts that the generator fails when pointed at a response file path that does not exist,
    /// reporting the expected error.
    /// </summary>
    /// <param name="error">The expected error id (e.g. <c>"CSWINRTWINMDGEN0001"</c>) the output must contain.</param>
    public static void AssertFailureForMissingResponseFile(string error)
    {
        string missingResponseFilePath = Path.Combine(Path.GetTempPath(), $"WinMDGeneratorTest_{Guid.NewGuid():N}.rsp");

        AssertFailureResult(RunTool(GetGeneratorPath(), missingResponseFilePath), error);
    }

    /// <summary>
    /// Asserts that the generator saves a debug repro for a caller-provided response file, and that
    /// replaying that debug repro also succeeds.
    /// </summary>
    /// <remarks>
    /// The <paramref name="responseFileFactory"/> receives a fresh temporary working directory and the
    /// directory to point <c>--debug-repro-directory</c> at, and returns the full response file content.
    /// </remarks>
    /// <param name="responseFileFactory">Builds the full response file content for the given directories.</param>
    public static void AssertDebugReproRoundTrips(Func<string, string, string> responseFileFactory)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("WinMDGeneratorTest_").FullName;

        try
        {
            string debugReproDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, "repro")).FullName;
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            File.WriteAllText(responseFilePath, responseFileFactory(temporaryDirectory, debugReproDirectory));

            (int exitCode, string output) = RunTool(toolPath, responseFilePath);

            Assert.AreEqual(0, exitCode, output);

            string debugReproPath = Path.Combine(debugReproDirectory, "winmd-debug-repro.zip");

            Assert.IsTrue(File.Exists(debugReproPath), $"The debug repro was not saved to '{debugReproPath}'.");

            // Replaying the saved archive validates that the packed inputs and the path maps inside
            // it are consistent, and not just that saving the archive happened to not throw
            (exitCode, output) = RunTool(toolPath, debugReproPath);

            Assert.AreEqual(0, exitCode, output);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Compiles the given C# <paramref name="source"/> into <c>TestInput.dll</c> in <paramref name="directory"/>.
    /// </summary>
    /// <remarks>
    /// This is exposed for the <see cref="AssertFailure(Func{string, string}, string)"/>
    /// scenarios that need a valid input assembly before triggering a later-stage failure (e.g. an
    /// unwritable output path).
    /// </remarks>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <param name="directory">The directory to emit the assembly into.</param>
    /// <returns>The full path to the compiled assembly.</returns>
    public static string CompileComponent(string source, string directory)
    {
        string outputPath = Path.Combine(directory, "TestInput.dll");

        CSharpParseOptions parseOptions = new(LanguageVersion.Preview);

        // The target framework attribute lets the generator probe the .NET runtime version. It is added
        // in a separate syntax tree so it does not interfere with any 'using' directives in the test
        // source (an assembly attribute must precede type declarations but follow 'using' directives).
        SyntaxTree sourceTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        SyntaxTree assemblyInfoTree = CSharpSyntaxTree.ParseText(
            """[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]""",
            parseOptions);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestInput",
            syntaxTrees: [sourceTree, assemblyInfoTree],
            references: Net100.References.All,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));

        EmitResult result = compilation.Emit(outputPath);

        Assert.IsTrue(result.Success, $"Input compilation failed:\n{string.Join("\n", result.Diagnostics)}");

        return outputPath;
    }

    /// <summary>
    /// Runs the generator for the given component source and returns the custom attributes applied in
    /// the produced <c>.winmd</c>, keyed by the metadata row they are applied to.
    /// </summary>
    /// <remarks>
    /// Keys are formatted as <c>Namespace.Type</c> for types and <c>Namespace.Type.Member</c> for methods,
    /// properties, events and fields. That distinction matters: Windows Runtime metadata places some member
    /// attributes on the accessor method (e.g. <c>get_Value</c>) rather than on the property or event row.
    /// </remarks>
    /// <param name="source">The C# source defining the authored component types.</param>
    /// <returns>A lookup from metadata row to the fully qualified names of the attributes applied to it.</returns>
    public static ILookup<string, string> GetGeneratedAttributes(string source)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("WinMDGeneratorTest_").FullName;

        try
        {
            string inputAssemblyPath = CompileComponent(source, temporaryDirectory);
            string outputWinMDPath = Path.Combine(temporaryDirectory, "TestInput.winmd");
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            File.WriteAllText(responseFilePath, $"""
                --input-assembly-path {inputAssemblyPath}
                --reference-assembly-paths {inputAssemblyPath}
                --output-winmd-path {outputWinMDPath}
                --assembly-version 1.0.0.0
                --use-windows-ui-xaml-projections false
                """);

            (int exitCode, string output) = RunTool(toolPath, responseFilePath);

            Assert.AreEqual(0, exitCode, output);

            return ReadAttributes(outputWinMDPath);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Reads every custom attribute application in a <c>.winmd</c>, keyed by the metadata row it is applied to.
    /// </summary>
    /// <param name="winmdPath">The path of the <c>.winmd</c> to read.</param>
    /// <returns>A lookup from metadata row to the fully qualified names of the attributes applied to it.</returns>
    private static ILookup<string, string> ReadAttributes(string winmdPath)
    {
        using FileStream stream = File.OpenRead(winmdPath);
        using PEReader peReader = new(stream);

        // Windows Runtime projections are deliberately not applied: the tests assert the raw Windows
        // Runtime metadata the generator emits, not the CLR view of it that the reader can synthesize
        MetadataReader reader = peReader.GetMetadataReader(MetadataReaderOptions.None);

        List<KeyValuePair<string, string>> attributes = [];

        void AddAttributes(string owner, CustomAttributeHandleCollection handles)
        {
            foreach (CustomAttributeHandle handle in handles)
            {
                attributes.Add(new KeyValuePair<string, string>(owner, GetAttributeTypeName(reader, reader.GetCustomAttribute(handle))));
            }
        }

        foreach (TypeDefinitionHandle typeHandle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(typeHandle);
            string owner = Format(reader.GetString(type.Namespace), reader.GetString(type.Name));

            AddAttributes(owner, type.GetCustomAttributes());

            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);

                AddAttributes($"{owner}.{reader.GetString(method.Name)}", method.GetCustomAttributes());
            }

            foreach (PropertyDefinitionHandle propertyHandle in type.GetProperties())
            {
                PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);

                AddAttributes($"{owner}.{reader.GetString(property.Name)}", property.GetCustomAttributes());
            }

            foreach (EventDefinitionHandle eventHandle in type.GetEvents())
            {
                EventDefinition @event = reader.GetEventDefinition(eventHandle);

                AddAttributes($"{owner}.{reader.GetString(@event.Name)}", @event.GetCustomAttributes());
            }

            foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
            {
                FieldDefinition field = reader.GetFieldDefinition(fieldHandle);

                AddAttributes($"{owner}.{reader.GetString(field.Name)}", field.GetCustomAttributes());
            }
        }

        return attributes.ToLookup(static pair => pair.Key, static pair => pair.Value);
    }

    /// <summary>
    /// Resolves the fully qualified type name of the attribute a custom attribute application refers to.
    /// </summary>
    /// <param name="reader">The metadata reader for the <c>.winmd</c>.</param>
    /// <param name="attribute">The custom attribute application to resolve.</param>
    /// <returns>The fully qualified name of the attribute type.</returns>
    private static string GetAttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        // The constructor is a 'MemberReference' for attributes defined outside the '.winmd' (which is
        // every attribute the generator emits) and a 'MethodDefinition' for any defined inside it
        EntityHandle declaringType = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => default
        };

        if (declaringType.Kind == HandleKind.TypeReference)
        {
            TypeReference typeReference = reader.GetTypeReference((TypeReferenceHandle)declaringType);

            return Format(reader.GetString(typeReference.Namespace), reader.GetString(typeReference.Name));
        }

        if (declaringType.Kind == HandleKind.TypeDefinition)
        {
            TypeDefinition typeDefinition = reader.GetTypeDefinition((TypeDefinitionHandle)declaringType);

            return Format(reader.GetString(typeDefinition.Namespace), reader.GetString(typeDefinition.Name));
        }

        return string.Empty;
    }

    /// <summary>
    /// Formats a namespace and type name as a fully qualified type name.
    /// </summary>
    /// <param name="typeNamespace">The namespace of the type (possibly empty).</param>
    /// <param name="typeName">The name of the type.</param>
    /// <returns>The fully qualified type name.</returns>
    private static string Format(string typeNamespace, string typeName)
    {
        return typeNamespace.Length == 0 ? typeName : $"{typeNamespace}.{typeName}";
    }

    /// <summary>
    /// Compiles the given C# <paramref name="source"/> into an assembly and runs the WinMD generator
    /// against it with a standard response file.
    /// </summary>
    private static (int ExitCode, string Output) Run(string source, bool useWindowsUIXamlProjections)
    {
        return Run(temporaryDirectory =>
        {
            string inputAssemblyPath = CompileComponent(source, temporaryDirectory);

            return $"""
                --input-assembly-path {inputAssemblyPath}
                --reference-assembly-paths {inputAssemblyPath}
                --output-winmd-path {Path.Combine(temporaryDirectory, "TestInput.winmd")}
                --assembly-version 1.0.0.0
                --use-windows-ui-xaml-projections {useWindowsUIXamlProjections}
                """;
        });
    }

    /// <summary>
    /// Runs the WinMD generator against a caller-provided response file.
    /// </summary>
    private static (int ExitCode, string Output) Run(Func<string, string> responseFileFactory)
    {
        string toolPath = GetGeneratorPath();
        string temporaryDirectory = Directory.CreateTempSubdirectory("WinMDGeneratorTest_").FullName;

        try
        {
            string responseFileContent = responseFileFactory(temporaryDirectory);
            string responseFilePath = Path.Combine(temporaryDirectory, "args.rsp");

            File.WriteAllText(responseFilePath, responseFileContent);

            return RunTool(toolPath, responseFilePath);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    /// <summary>
    /// Asserts that a generator run failed (non-zero exit code) and its output contains the expected error.
    /// </summary>
    private static void AssertFailureResult((int ExitCode, string Output) result, string error)
    {
        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, error);
    }

    /// <summary>
    /// Runs <c>dotnet exec &lt;tool&gt; &lt;argument&gt;</c> and captures the exit code and output.
    /// </summary>
    private static (int ExitCode, string Output) RunTool(string toolPath, string argument)
    {
        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(toolPath);
        startInfo.ArgumentList.Add(argument);

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the WinMD generator process.");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        return (process.ExitCode, standardOutput + standardError);
    }

    /// <summary>
    /// Resolves the path to the built <c>cswinrtwinmdgen</c> tool from assembly metadata.
    /// </summary>
    private static string GetGeneratorPath()
    {
        string? path = typeof(WinMDGeneratorRunner).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(static attribute => attribute.Key == "WinMDGeneratorAssemblyPath")?.Value;

        Assert.IsFalse(string.IsNullOrEmpty(path), "The 'WinMDGeneratorAssemblyPath' assembly metadata was not found.");

        string fullPath = Path.GetFullPath(path!);

        Assert.IsTrue(File.Exists(fullPath), $"The WinMD generator was not found at '{fullPath}'.");

        return fullPath;
    }

    /// <summary>
    /// Deletes a directory recursively, ignoring failures (best effort cleanup).
    /// </summary>
    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Best effort cleanup; a leftover temp directory must not fail the test
        }
    }
}
