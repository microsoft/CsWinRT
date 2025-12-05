// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionGenerator.Resolvers;

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
    /// <param name="projectionReferenceAssemblies">The projection reference assemblies which were used to generate the sources.</param>
    /// <returns>The path to the folder containing the generated sources.</returns>
    private static string GenerateSources(ProjectionGeneratorArgs args, out HashSet<string> projectionReferenceAssemblies)
    {
        args.Token.ThrowIfCancellationRequested();

        GenerateRspFile(args, out string outputFolder, out string rspFile, out projectionReferenceAssemblies);

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
    /// <param name="projectionReferenceAssemblies">The projection reference assemblies which were used to generate the rsp file.</param>
    private static void GenerateRspFile(ProjectionGeneratorArgs args, out string outputFolder, out string rspFile, out HashSet<string> projectionReferenceAssemblies)
    {
        args.Token.ThrowIfCancellationRequested();

        outputFolder = GetTempFolder();
        rspFile = Path.Combine(outputFolder, "ProjectionGenerator.rsp");
        projectionReferenceAssemblies = [];

        using StreamWriter fileStream = new(rspFile);
        PathAssemblyResolver resolver = new(args.ReferenceAssemblyPaths);
        foreach (string referenceAssemblyPath in args.ReferenceAssemblyPaths)
        {
            ModuleDefinition moduleDefinition = ModuleDefinition.FromFile(referenceAssemblyPath, resolver.ReaderParameters);

            // Check if this is a reference assembly as the ones we are regenerating are reference assemblies.
            if (!IsReferenceAssembly(moduleDefinition) || !IsWindowsRuntimeReferenceAssembly(moduleDefinition))
            {
                continue;
            }

            _ = projectionReferenceAssemblies.Add(referenceAssemblyPath);

            if (moduleDefinition.Assembly is not null)
            {
                if (moduleDefinition.Assembly.Name == "Microsoft.Windows.SDK.NET")
                {
                    // Given we know this is the Windows SDK projection and types will be only from that namespace,
                    // we just include that namespace rather than going through the types.
                    fileStream.WriteLine($"-include Windows");
                    fileStream.WriteLine($"-include WinRT.Interop");
                    continue;
                }
                else if (moduleDefinition.Assembly.Name == "Microsoft.WinUI")
                {
                    // In additional to projecting the individual types, make sure
                    // the additions get included by including the namespace.
                    fileStream.WriteLine($"-include Microsoft.UI");
                }
            }

            foreach (TypeDefinition exportedType in moduleDefinition.TopLevelTypes)
            {
                fileStream.WriteLine($"-include {exportedType.FullName}");
            }
        }

        fileStream.WriteLine($"-target {args.TargetFramework}");
        fileStream.WriteLine($"-input {args.WindowsMetadata}");
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
            FileName = args.CsWinRTExePath,
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

    /// <summary>
    /// Checks if the specified module definition represents a Windows Runtime reference assembly.
    /// </summary>
    /// <param name="moduleDefinition">The module definition to check.</param>
    /// <returns><c>true</c> if the module is a Windows Runtime reference assembly; otherwise, <c>false</c>.</returns>
    private static bool IsWindowsRuntimeReferenceAssembly(ModuleDefinition moduleDefinition)
    {
        return moduleDefinition.Assembly is not null && moduleDefinition.Assembly.HasCustomAttribute("WindowsRuntime.InteropServices"u8, "WindowsRuntimeReferenceAssemblyAttribute"u8);
    }
}