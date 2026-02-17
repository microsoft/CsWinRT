// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionGenerator.Errors;
using WindowsRuntime.ProjectionGenerator.Resolvers;

#pragma warning disable IDE0270

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <inheritdoc cref="ProjectionGenerator"/>
internal partial class ProjectionGenerator
{
    /// <summary>
    /// Creates a temporary folder for CsWinRT generation output.
    /// </summary>
    /// <returns>The path to the created temporary folder.</returns>
    /// <remarks>
    /// The returned folder is guaranteed to already exist when this method returns.
    /// </remarks>
    private static string GetTempFolder()
    {
        // Create a temporary folder with this structure: '%TEMP%/CsWinRT/<RANDOM_NAME>'
        string outputDir = Path.Combine(Path.GetTempPath(), "CsWinRT", Path.GetRandomFileName()).TrimEnd('\\');

        // Ensure the folder does exist (code using it might rely on it existing already)
        _ = Directory.CreateDirectory(outputDir);

        return outputDir;
    }

    /// <summary>
    /// Runs the processing logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <returns>The resulting state.</returns>
    private static ProjectionGeneratorProcessingState ProcessReferences(ProjectionGeneratorArgs args)
    {
        args.Token.ThrowIfCancellationRequested();

        GenerateRspFile(args, out string outputFolder, out string rspFile, out HashSet<string> projectionReferenceAssemblies);

        string[] referencesWithoutProjections = [.. args.ReferenceAssemblyPaths.Where(r => !projectionReferenceAssemblies.Contains(r))];

        return new ProjectionGeneratorProcessingState(outputFolder, rspFile, referencesWithoutProjections);
    }

    /// <summary>
    /// Runs the source generation logic for the generator.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="processingState">The state from the processing phase.</param>
    private static void GenerateSources(ProjectionGeneratorArgs args, ProjectionGeneratorProcessingState processingState)
    {
        ProcessStartInfo processInfo = new()
        {
            FileName = args.CsWinRTExePath,
            Arguments = "@" + processingState.RspFilePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };

        Process? cswinrtProcess;

        try
        {
            cswinrtProcess = Process.Start(processInfo);

            // Make sure we did successfully start the process ('Start' can return 'null')
            if (cswinrtProcess is null)
            {
                throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessStartError();
            }
        }
        catch (Exception e) when (!e.IsWellKnown)
        {
            throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessStartError(e);
        }

        // Validate that generation was successful
        using (cswinrtProcess)
        {
            string error = cswinrtProcess.StandardError.ReadToEnd();

            cswinrtProcess.WaitForExit();

            if (cswinrtProcess.ExitCode != 0)
            {
                throw WellKnownProjectionGeneratorExceptions.CsWinRTProcessError(
                    exitCode: cswinrtProcess.ExitCode,
                    exception: new Win32Exception(cswinrtProcess.ExitCode, error));
            }
        }
    }

    /// <summary>
    /// Generates a response file for CsWinRT based on the provided arguments and reference assemblies.
    /// </summary>
    /// <param name="args">The arguments for this invocation.</param>
    /// <param name="outputFolder">The folder where sources will be generated.</param>
    /// <param name="rspFile">The generated response file for running <c>cswinrt.exe</c>.</param>
    /// <param name="projectionReferenceAssemblies">The projection reference assemblies which were used to generate the response file.</param>
    private static void GenerateRspFile(
        ProjectionGeneratorArgs args,
        out string outputFolder,
        out string rspFile,
        out HashSet<string> projectionReferenceAssemblies)
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
                    // In addition to projecting the individual types, make sure
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