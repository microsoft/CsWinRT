// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.Generator.Resolvers;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// Creates a temporary folder for CsWinRT generation output.
    /// </summary>
    /// <returns>The path to the created temporary folder.</returns>
    private static string GetTempFolder()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), "CsWinRT", Path.GetRandomFileName()).TrimEnd('\\');
        _ = Directory.CreateDirectory(outputDir);
        return outputDir;
    }

    /// <summary>
    /// Generate the projection sources using CsWinRT for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The path to the folder containing the generated sources.</returns>
    private static string GenerateSources(ProjectionGeneratorArgs args)
    {
        args.Token.ThrowIfCancellationRequested();

        GenerateRspFile(args, out string outputFolder, out string rspFile);

        args.Token.ThrowIfCancellationRequested();

        RunCsWinRT(args, rspFile);

        return outputFolder;
    }

    /// <summary>
    /// Generates a response file for CsWinRT based on the provided arguments and reference assemblies.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="outputFolder">The folder where sources are generated in.</param>
    /// <param name="rspFile">The generated response file for running cswinrt.exe.</param>
    private static void GenerateRspFile(ProjectionGeneratorArgs args, out string outputFolder, out string rspFile)
    {
        args.Token.ThrowIfCancellationRequested();

        outputFolder = GetTempFolder();
        rspFile = Path.Combine(outputFolder, "ProjectionGenerator.rsp");

        using StreamWriter fileStream = new(rspFile);
        PathAssemblyResolver resolver = new(args.ReferenceAssemblyPaths);
        foreach (string referenceAssemblyPath in args.ReferenceAssemblyPaths)
        {
            ModuleDefinition moduleDefinition = ModuleDefinition.FromFile(referenceAssemblyPath, resolver.ReaderParameters);

            // Check if this is a reference assembly as the ones we are regenerating are reference assemblies.
            if (!IsReferenceAssembly(moduleDefinition))
            {
                continue;
            }

            foreach (TypeDefinition exportedType in moduleDefinition.TopLevelTypes)
            {
                // Check if WinRT projected type.
                if (!(exportedType.HasCustomAttribute("WindowsRuntime", "WindowsRuntimeMetadataAttribute") || exportedType.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute")))
                {
                    continue;
                }

                fileStream.WriteLine($"-include {exportedType.FullName}");
            }
        }

        // Version targetFrameworkVersion = Version.Parse(args.TargetFramework);
        // fileStream.WriteLine($"-target net{targetFrameworkVersion.Major}.{targetFrameworkVersion.MajorRevision}");
        fileStream.WriteLine($"-target net8.0");
        fileStream.WriteLine($"-input {args.TargetFramework}");
        fileStream.WriteLine($"-output \"{outputFolder}\"");
        foreach (string winmdPath in args.WinMDPaths)
        {
            fileStream.WriteLine($"-input \"{winmdPath}\"");
        }
    }

    /// <summary>
    /// Executes the CsWinRT tool with the specified response file.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="rspFile">The path to the response file containing CsWinRT arguments.</param>
    private static void RunCsWinRT(ProjectionGeneratorArgs args, string rspFile)
    {
        ProcessStartInfo processInfo = new()
        {
            FileName = args.CsWinRTPath,
            Arguments = "@" + rspFile,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };

        using Process cswinrtProcess = Process.Start(processInfo) ?? throw new Exception();
        string error = cswinrtProcess.StandardError.ReadToEnd();
        cswinrtProcess.WaitForExit();

        if (cswinrtProcess.ExitCode != 0)
        {
            throw new Win32Exception(cswinrtProcess.ExitCode, error);
        }
    }

    /// <summary>
    /// Checks if the specified module definition represents a reference assembly.
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module is a reference assembly; otherwise, <c>false</c>.</returns>
    private static bool IsReferenceAssembly(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.Assembly is not null && moduleDefinition.Assembly.HasCustomAttribute("System.Runtime.CompilerServices"u8, "ReferenceAssemblyAttribute"u8);
    }
}
